using System.Threading.Tasks;
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
