using System.Collections.Generic;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using Orleans.Providers;

namespace $safeprojectname$
{
    [StorageProvider(ProviderName = "MemoryStore")]
    public class PrefsGrain : Grain<PrefsGrainState>, IPrefsGrain
    {
        public override Task OnActivateAsync()
        {
            if (State.Prefs == null)
            {
                State.Prefs = new Dictionary<string, string>();
            }
            return base.OnActivateAsync();
        }

        public Task SetValue(KeyValuePair<string, string> entry)
        {
            State.Prefs.Add(entry);
            return TaskDone.Done;
        }

        public Task<string> GetValue(string key)
        {
            return Task.FromResult(State.Prefs[key]);
        }

        public Task<IDictionary<string, string>> GetAllEntries()
        {
            return Task.FromResult(State.Prefs);
        }

        public Task ClearValues()
        {
            State.Prefs.Clear();
            return TaskDone.Done;
        }

        public Task<GrainState> GetState()
        {
            return Task.FromResult((GrainState)State);
        }
    }

    public class PrefsGrainState : GrainState
    {
        public IDictionary<string, string> Prefs { get; set; }
    }
}
