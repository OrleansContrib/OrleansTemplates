using System;
using System.Threading.Tasks;
using ETG.Orleans.Attributes;

namespace $rootnamespace$
{
    [State(Type = typeof ($StateInterfaceName$), LazyWrite = true, Period = 5)]
    public class $safeitemname$ : $safeitemname$Base, I$safeitemname$
    {
        public override Task OnActivateAsync()
        {
            // TODO: write your activation code here
            return base.OnActivateAsync();
        }
    }
	
    public interface $StateInterfaceName$ : IGrainState
    {
        //TODO: Add your state properties here
    }	
}
