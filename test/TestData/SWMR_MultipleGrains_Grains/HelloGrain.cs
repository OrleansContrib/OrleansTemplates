using System.Threading.Tasks;
using ETG.Orleans.Swmr;
using GrainInterfaces;
using Orleans;
using Orleans.CodeGeneration;

namespace Grains
{
    public class HelloGrain : Grain<HelloGrainState>, IHelloGrain
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

        public Task<GrainState> GetState()
        {
            return Task.FromResult(State as GrainState);
        }
    }

    public class HelloGrainState : GrainState
    {
        public string Value { get; set; }

        public string OtherValue { get; set; }
    }
}
