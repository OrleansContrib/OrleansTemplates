using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ETG.Orleans.Swmr;
using Orleans;
using Orleans.Concurrency;

namespace TestingConsoleApp.swmr
{
    public interface IHelloGrainReader : IGrainWithStringKey
    {
        Task ReadSomething(string sessionId);
    }

    public interface IHelloGrainWriter : IGrainWithStringKey
    {
        Task WriteSomething(string value, string sessionId);
    }

    public interface IHelloGrainReadReplica : IGrainWithStringKey
    {
        Task ReadSomething();

        Task SetState(IGrainState state);
    }

    public class HelloGrainReadReplica : Grain, IHelloGrainReadReplica
    {
        private IHelloGrainState State;

        public override async Task OnActivateAsync()
        {
            string grainId = this.GetPrimaryKeyString().Split('_')[0];
            IHelloGrain grain = GrainFactory.GetGrain<IHelloGrain>(grainId);
            await SetState(await grain.GetState());
            await base.OnActivateAsync();
        }

        public Task ReadSomething()
        {
            return Task.FromResult(State.Value);
        }

        public Task SetState(IGrainState state)
        {
            State = state as IHelloGrainState;
            return TaskDone.Done;
        }
    }

    [StatelessWorker]
    public class HelloGrainReader : Grain, IHelloGrainReader
    {
        private ITopology _topology;

        public override async Task OnActivateAsync()
        {
            _topology = new ConsistentHashRing(8);
            await base.OnActivateAsync();
        }

        public Task ReadSomething(string sessionId)
        {
            string sessionNode = _topology.GetNode(sessionId);
            var readReplica = GrainFactory.GetGrain<IHelloGrainReadReplica>(this.GetPrimaryKeyString() + "_" + sessionNode);
            return readReplica.ReadSomething();               
        }
    }

    public class HelloGrainWriter : Grain, IHelloGrainWriter
    {
        private ITopology _topology;

        public override async Task OnActivateAsync()
        {
            _topology = new ConsistentHashRing(8);
            await base.OnActivateAsync();
        }

        public async Task WriteSomething(string value, string sessionId)
        {
            string grainId = this.GetPrimaryKeyString();
            IHelloGrain grain = GrainFactory.GetGrain<IHelloGrain>(grainId);
            await grain.WriteSomething("something");
            IGrainState state = await grain.GetState();

            string sessionNode = _topology.GetNode(sessionId);
            IEnumerable<string> otherNodes = _topology.Nodes.Where(node => node != sessionNode);
            foreach (string node in otherNodes)
            {
                GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + "_" + node).SetState(state);
            }

            await GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + "_" + sessionNode).SetState(state);
        }
    }
}
