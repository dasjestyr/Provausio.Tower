using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Provausio.Tower.Api.Startup))]

namespace Provausio.Tower.Api
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
