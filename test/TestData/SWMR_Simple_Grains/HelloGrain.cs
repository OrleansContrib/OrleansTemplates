using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class HelloGrain : Grain<HelloGrainState>, IHelloGrain
    {
        public Task<string> ReadSomething()
        {
            return Task.FromResult(State.Value);
        }

        public Task<string> ReadSomethingElse(string param)
        {
            return Task.FromResult(State.OtherValue);
        }

        public Task WriteSomething(string something)
        {
            State.Value = something;
            return TaskDone.Done;
        }

        public Task<string> WriteSomethingElse(string something)
        {
            State.OtherValue = something;
            return Task.FromResult("done");
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
