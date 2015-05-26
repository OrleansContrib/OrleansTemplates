using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using Orleans;

namespace TestingConsoleApp.swmr
{
    [SingleWriterMultipleReaders(ReadReplicaCount = 10)]
    public interface IHelloGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<string> ReadSomething();

        Task WriteSomething(string something);

        Task<IGrainState> GetState();
    }
}
