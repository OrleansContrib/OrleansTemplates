using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class OtherGrain : Grain<OtherGrainState>, IOtherGrain
    {
        private string value;

        public Task<string> ReadSomething()
        {
            return Task.FromResult(value);
        }

        public Task WriteSomething(string something)
        {
            value = something;
            return TaskDone.Done;
        }

        public Task<GrainState> GetState()
        {
            return Task.FromResult(State as GrainState);
        }
    }

    public class OtherGrainState : GrainState
    {
    }
}
