using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Configuration;
using Orleans.Statistics;
using Orleans.Hosting;

using Grains;
using System.Net;

namespace Silo
{
    public class Program
    {
        private static ISiloHost silo;
        private static readonly ManualResetEvent siloStopped = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            // TODO replace with your connection string
            silo = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "orleans-docker";
                    options.ServiceId = "AspNetSampleApp";
                })
                .Configure<ClusterMembershipOptions>(options =>
                {
                    options.DefunctSiloCleanupPeriod = TimeSpan.FromMinutes(10);
                    options.DefunctSiloExpiration = TimeSpan.FromMinutes(5);
                })
                .UseMongoDBClustering(options =>
                {
                    options.ConnectionString = "mongodb://192.168.124.88:27017";
                    options.DatabaseName = "k8s-clustering";
                })
                .UseLinuxEnvironmentStatistics()
                .UseDashboard(options =>
                {
                    options.HostSelf = false;
                    options.HideTrace = true;
                    options.Port = 8524;
                })
                .ConfigureEndpoints(new Random(1).Next(10001, 11100), new Random(1).Next(20001, 21100))
                .AddMemoryGrainStorageAsDefault()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ValueGrain).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole())
                .Build();

            Task.Run(StartSilo);

            AssemblyLoadContext.Default.Unloading += context =>
            {
                Task.Run(StopSilo);
                siloStopped.WaitOne();
            };

            siloStopped.WaitOne();
        }

        private static async Task StartSilo()
        {
            await silo.StartAsync();
            Console.WriteLine("Silo started");
        }

        private static async Task StopSilo()
        {
            await silo.StopAsync();
            Console.WriteLine("Silo stopped");
            siloStopped.Set();
        }
    }
}
