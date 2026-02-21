using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;
using System.Text.Json;

namespace StarterProject.Client
{
    public class Constants
    {
        public static readonly JsonSerializerOptions JsonSerializeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static readonly List<CultureInfo> SupportedCultures = [
            new CultureInfo("en-US"),
            new CultureInfo("it-IT")
        ];

        public static RenderType ApplicationRenderType => RenderType.Both;

        public static IComponentRenderMode? ApplicationRenderMode
        {
            get
            {
                return ApplicationRenderType switch
                {
                    RenderType.Client => new InteractiveWebAssemblyRenderMode(),
                    RenderType.Server => new InteractiveServerRenderMode(),
                    _ => null,
                };
            }
        }

        internal const string DefaultHttpClientName = "MyApi";

        public class RenderModes
        {
            public const string Static = "Static";
            public const string Server = "Server";
            public const string WebAssembly = "WebAssembly";
        }
    }
}
