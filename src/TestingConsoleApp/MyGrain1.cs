using System;
using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using GrainsTemplate;
using Orleans;

namespace TestingConsoleApp
{
    [State(Type = typeof(IMyGrain1State), LazyWrite = true, Period = 5)]
    public class MyGrain1 : MyGrain1Base, IMyGrain1
    {
        public override Task OnActivateAsync()
        {
            // TODO: write your activation code here
            return base.OnActivateAsync();
        }
    }

    public interface IMyGrain1State : IGrainState
    {
        //TODO: Add your state properties here
    }
}
