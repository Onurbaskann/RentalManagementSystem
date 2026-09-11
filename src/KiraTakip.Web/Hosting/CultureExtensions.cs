using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace KiraTakip.Web.Hosting
{
    public static class CultureExtensions
    {
        public static WebApplication UseTurkishCulture(this WebApplication app)
        {
            var cultureInfo = new CultureInfo("tr-TR");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            
            return app;
        }
    }
}
