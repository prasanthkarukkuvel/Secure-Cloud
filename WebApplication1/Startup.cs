using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SecureCloud.Startup))]
namespace SecureCloud
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
