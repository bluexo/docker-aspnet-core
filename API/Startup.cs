using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using OrleansDashboard;

using GrainInterfaces;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton(CreateClusterClient);
            services.AddSingleton<IGrainFactory>(provider => provider.GetService<IClusterClient>());
            services.AddServicesForSelfHostedDashboard(configurator: (DashboardOptions options) =>
            {
                options.Port = 8524;
                options.HideTrace = true;
            });
            services.AddExceptional(settings =>
            {
                settings.Store.ApplicationName = $"Samples.AspNetCore";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptional();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseOrleansDashboard();
            app.Map("/dashboard", builder => builder.UseOrleansDashboard());
            app.UseMvcWithDefaultRoute();
        }

        private IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            var log = serviceProvider.GetService<ILogger<Startup>>();

            // TODO replace with your connection string
            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "orleans-docker";
                    options.ServiceId = "AspNetSampleApp";
                })
                .UseDashboard()
                .UseMongoDBClustering(options =>
                {
                    options.ConnectionString = "mongodb://192.168.124.88:27017";
                    options.DatabaseName = "k8s-clustering";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IValueGrain).Assembly))
                .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole())
                .Build();

            client.Connect(RetryFilter).GetAwaiter().GetResult();
            log.LogInformation($"Orleans client connected!");
            return client;

            async Task<bool> RetryFilter(Exception exception)
            {
                log?.LogWarning("Exception while attempting to connect to Orleans cluster: {Exception}", exception);
                await Task.Delay(TimeSpan.FromSeconds(2));
                return true;
            }
        }
    }
}
