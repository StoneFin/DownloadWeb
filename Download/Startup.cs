using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Download.Startup))]
namespace Download
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
