using GrainInterfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class ValueGrain : Grain, IValueGrain
    {
        private string value = "-----------NONE-----------";

        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public Task<string> GetValue()
        {
            Console.WriteLine($"GetValue Time = {DateTime.Now}!");
            return Task.FromResult(this.value);
        }

        public Task SetValue(string value)
        {
            this.value = value;
            return Task.CompletedTask;
        }
    }
}
