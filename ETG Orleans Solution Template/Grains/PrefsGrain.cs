using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using GrainInterfaces;
using Orleans;

namespace $safeprojectname$
{
    [State(Type = typeof(IPrefsGrainState), LazyWrite = true, Period = 5, StorageProvider = "MemoryStore")]
    public class PrefsGrain : PrefsGrainBase, IPrefsGrain
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
    }

    public interface IPrefsGrainState : IGrainState
    {
        IDictionary<string, string> Prefs { get; set; }
    }
}
