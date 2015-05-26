using System.Reflection.Emit;
using System.Threading.Tasks;
using ETG.Orleans.Swmr;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class OtherGrain : Grain<IOtherGrainState>, IOtherGrain
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

        public Task<IGrainState> GetState()
        {
            return Task.FromResult(State as IGrainState);
        }
    }

    public interface IOtherGrainState : IGrainState
    {
    }
}
