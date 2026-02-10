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

        internal const string DefaultHttpClientName = "MyApi";

        public class RenderModes
        {
            public const string Static = "Static";
            public const string Server = "Server";
            public const string WebAssembly = "WebAssembly";
        }
    }
}
