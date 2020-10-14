using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;

[assembly: OwinStartupAttribute(typeof(Samico.Startup))]
namespace Samico
{
    public partial class Startup
    {
        /// <summary>
        /// Configures services and the app's request pipeline
        /// </summary>
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration { EnableDetailedErrors = true };

            ConfigureAuth(app);
            //Enable SignalR on the app
            app.MapSignalR(hubConfiguration);

        }
    }
}
