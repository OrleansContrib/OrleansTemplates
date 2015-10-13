using System.Threading.Tasks;
using Orleans;

namespace Grains
{
    public abstract partial class MyGrainBase : Grain<IMyGrainState>
    {
    }
}
