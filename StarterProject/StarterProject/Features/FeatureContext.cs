using BlazorFeatures.Abstractions;

namespace StarterProject.Features
{
    internal class FeatureContext : IFeatureContext
    {
        public List<IBaseFeatureRequest> FeatureChain { get; set; } = [];
    }
}
