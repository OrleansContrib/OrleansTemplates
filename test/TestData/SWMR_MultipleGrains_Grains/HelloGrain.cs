using System.Threading.Tasks;
using ETG.Orleans.Swmr;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class HelloGrain : Grain<IHelloGrainState>, IHelloGrain
    {
        public Task<string> ReadSomething()
        {
            return Task.FromResult(State.Value);
        }

        public Task WriteSomething(string something)
        {
            State.Value = something;
            return TaskDone.Done;
        }

        public Task<IGrainState> GetState()
        {
            return Task.FromResult(State as IGrainState);
        }
    }

    public interface IHelloGrainState : IGrainState
    {
        string Value { get; set; }

        string OtherValue { get; set; }
    }
}
