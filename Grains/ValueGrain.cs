using GrainInterfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class ValueGrain : Grain, IValueGrain
    {
        private string value = "none";

        public override Task OnActivateAsync()
        {
            RegisterTimer(OnTimerElapsed, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return base.OnActivateAsync();
        }

        private Task OnTimerElapsed(object arg)
        {
            Console.WriteLine($"Current DateTime {DateTime.Now}!");
            return Task.CompletedTask;
        }

        public Task<string> GetValue()
        {
            return Task.FromResult(this.value);
        }

        public Task SetValue(string value)
        {
            this.value = value;
            return Task.CompletedTask;
        }
    }
}
