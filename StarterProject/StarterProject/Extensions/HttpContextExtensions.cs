using StarterProject.Database.Entities;
using StarterProject.Database.Entities.OpenIddict;

namespace StarterProject.Extensions
{
    public static class HttpContextExtensions
    {
        public class HttpContextItems(HttpContext context)
        {
            public const string DATA_PREFIX = "DATA#";

            private T? GetItem<T>(string key) => (T?)context.Items[DATA_PREFIX + key];
            public HttpContext GetHttpContext() => context;

            public string? AuthenticationScheme => GetItem<string>(nameof(AuthenticationScheme));

            public User? User => GetItem<User>(nameof(User));

            public OpenIddictApplication? Application => GetItem<OpenIddictApplication>(nameof(Application));
        }

        public static HttpContextItems GetItems(this HttpContext httpContext)
        {
            return new HttpContextItems(httpContext);
        }
    }
}
