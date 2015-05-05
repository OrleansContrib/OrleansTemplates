﻿/*
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;

using Microsoft.Owin.Hosting;
using Owin;
using System.Web.Http;
using Microsoft.Owin.Cors;
using System.Reflection;
using System.Web.Http.Dispatcher;


namespace $safeprojectname$
{
    internal class WebApiWrapper: IDisposable
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
