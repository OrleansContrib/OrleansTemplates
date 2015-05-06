using System;
using System.Threading.Tasks;
using ETG.Orleans;
using Orleans;

internal class GrainStateWriter
{
    private int _retryCount;
    private readonly IGrainState _grainState;
    private readonly IGrainWithStringKey _grain;

    public GrainStateWriter(IGrainState grainState, Grain grain)
    {
        _grainState = grainState;
        if (!(grain is IGrainWithStringKey))
        {
            throw new ArgumentException(string.Format("Only grains that implement {0} are supported", typeof(IGrainWithStringKey).ToString()));
        }
        _grain = (IGrainWithStringKey) grain;
    }
    public async Task WriteState(object unused)
    {
        if (StateChanged)
        {
            StateChanged = false;
            // it's important to do this here otherwise in 3s we'll try again and the call to write the state may not have come back
            try
            {
                _grainState.Etag = null; // force rewrite
                await _grainState.WriteStateAsync();
                _retryCount = 0;
                StateWriteFailed = false;
            }
            catch
            {
                _retryCount++;
                if (_retryCount < 10)
                {
                    StateChanged = true; // allow retry via the timer
                    // do not rethrow here as we've handled the exception semantically correct
                    // in addition, this method is called by the runtime so there is no "client" to throw this to for handling
                    _grain.GetPrimaryKeyString();
                }
                else
                {
                    // signal that writing of the state failed
                    StateWriteFailed = true;
                }
            }
        }
    }

    public bool StateWriteFailed { get; set; }

    public bool StateChanged { get; set; }
}

namespace TestingConsoleApp
{
    public abstract partial class MyGrainBase : Grain<IMyGrainState>
    {
        public override Task OnActivateAsync()
        {
            RegisterTimer(new GrainStateWriter(State, this).WriteState, this, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
            return base.OnActivateAsync();
        }
    }
}
