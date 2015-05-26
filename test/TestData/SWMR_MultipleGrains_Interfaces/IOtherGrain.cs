using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using ETG.Orleans.Swmr;
using Orleans;

namespace GrainInterfaces
{
    [SingleWriterMultipleReaders(ReadReplicaCount = 10)]
    public interface IOtherGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<string> ReadSomething();

        Task WriteSomething(string something);

        Task<IGrainState> GetState();
    }
}




