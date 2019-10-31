using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Clustering.Kubernetes;

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
            services.AddControllersWithViews();
            services.AddSingleton(CreateClusterClient);
            services.AddExceptional(settings =>
            {
                settings.Store.ApplicationName = $"Samples.AspNetCore";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            app.UseExceptional();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
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
