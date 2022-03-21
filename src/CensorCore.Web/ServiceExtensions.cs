using Microsoft.Extensions.DependencyInjection;

namespace CensorCore.Web
{
    public static class ServiceExtensions
    {
        public static IMvcBuilder AddCensorCore(this IMvcBuilder mvc) {
            return mvc.AddApplicationPart(typeof(CensoringController).Assembly);
        }
    }
}