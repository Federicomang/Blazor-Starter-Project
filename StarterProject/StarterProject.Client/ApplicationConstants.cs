using System.Globalization;

namespace StarterProject.Client
{
    public class ApplicationConstants
    {
        public static readonly List<CultureInfo> SupportedCultures = [
            new CultureInfo("en-US"),
            new CultureInfo("it-IT")
        ];

        internal const string DefaultHttpClientName = "MyApi";
    }
}
