using StarterProject.Client.Features;

namespace StarterProject.Features
{
    internal class FeatureContext : IFeatureContext
    {
        public List<object> FeatureChain { get; set; } = [];
    }
}
