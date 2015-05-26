using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using Orleans;

namespace GrainInterfaces
{
    [SingleWriterMultipleReaders(ReadReplicaCount = 10)]
    public interface IHelloGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<string> ReadSomething();

        [ReadOnly]
        Task<string> ReadSomethingElse(string param);

        Task WriteSomething(string something);

        Task<string> WriteSomethingElse(string something);

        Task<IGrainState> GetState();
    }
}




