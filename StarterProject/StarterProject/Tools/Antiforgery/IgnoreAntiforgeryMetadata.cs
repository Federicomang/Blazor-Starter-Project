using Microsoft.AspNetCore.Antiforgery;

namespace StarterProject.Tools.Antiforgery
{
    public class IgnoreAntiforgeryMetadata : IAntiforgeryMetadata
    {
        public bool RequiresValidation => false;

        public bool IgnoreForJwtOnly { get; set; } = true;
    }
}
