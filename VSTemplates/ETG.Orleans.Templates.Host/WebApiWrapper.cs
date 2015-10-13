using System;
using System.Net;
using System.Diagnostics;
using Microsoft.Owin.Hosting;
using Owin;
using System.Web.Http;
using Microsoft.Owin.Cors;
using System.Web.Http.Dispatcher;
using Microsoft.Practices.Unity;
using Orleans;



namespace $safeprojectname$
{
    internal class WebApiWrapper : IDisposable
    {
        internal class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                HttpConfiguration config = new HttpConfiguration();
                config.Services.Replace(typeof(IAssembliesResolver), new SelfHostAssemblyResolver());

                config.MapHttpAttributeRoutes();
                config.Routes.MapHttpRoute("Default", "api/{controller}/{id}", new { id = RouteParameter.Optional });
                config.EnableCors();
                config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
                app.UseCors(CorsOptions.AllowAll);
                app.UseWebApi(config);

                var container = new UnityContainer();
                container.RegisterInstance(GrainClient.GrainFactory);
                config.DependencyResolver = new UnityResolver(container);
            }
        }


        private IDisposable _webApp = null;


        public string Url { get; set; }

        public bool Run()
        {
            bool ok = false;

            try
            {
                var startOptions = new StartOptions();
                startOptions.Urls.Add(string.Format("{0}://{1}:{2}", "http", Dns.GetHostName(), "81"));
                Url = startOptions.Urls[0];
                Trace.TraceInformation(String.Format("Starting OWIN at {0}", startOptions.Urls[0]), "Information");
                _webApp = WebApp.Start<Startup>(startOptions);
                ok = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return ok;
        }


        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            _webApp.Dispose();
            _webApp = null;
        }
    }
}
