using Microsoft.AspNetCore.Antiforgery;

namespace StarterProject.Tools.Antiforgery
{
    public class AntiforgeryValidationFeature(bool isValid, Exception? error) : IAntiforgeryValidationFeature
    {
        public bool IsValid => isValid;
        public Exception? Error => error;
    }
}
