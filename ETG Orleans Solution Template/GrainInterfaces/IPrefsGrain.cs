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
    public interface IPrefsGrain : IGrainWithStringKey
    {
        [Route("{id}")]
        [HttpPost]
        Task SetValue([FromBody] KeyValuePair<string, string> entry);

        [Route("{id}/{key}")]
        Task<string> GetValue(string key);

        [Route("{id}")]
        [HttpGet]
        Task<IDictionary<string, string>> GetAllEntries();

        [Route("{id}")]
        [HttpDelete]
        Task ClearValues();
    }
}
