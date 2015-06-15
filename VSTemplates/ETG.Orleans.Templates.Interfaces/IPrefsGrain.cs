using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using ETG.Orleans.Attributes;
using Orleans;

namespace $safeprojectname$
{
    /// <summary>
    /// Grain interface IGrain1
    /// </summary>
    [ApiController(RoutePrefix = "api/prefs")]
    [SingleWriterMultipleReaders(ReadReplicaCount=10)]
    public interface IPrefsGrain : IGrainWithStringKey
    {
        [Route("{id}")]
        [HttpPost]
        Task SetValue([FromBody] KeyValuePair<string, string> entry);

        [ReadOnly]
        [Route("{id}/{key}")]
        Task<string> GetValue(string key);

        [ReadOnly]
        [Route("{id}")]
        [HttpGet]
        Task<IDictionary<string, string>> GetAllEntries();

        [Route("{id}")]
        [HttpDelete]
        Task ClearValues();

        Task<IGrainState> GetState();
    }
}
