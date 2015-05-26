using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dispatcher;

namespace Host
{
    public class SelfHostAssemblyResolver : IAssembliesResolver
    {
        public ICollection<Assembly> GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var file in Directory.GetFiles(directory, "*.dll"))
            {
                assemblies.Add(Assembly.LoadFile(file));
            }
            return assemblies;
        }
    }
}
