namespace StarterProject.Client.Features
{
    internal class FeatureContext : IFeatureContext
    {
        public List<object> FeatureChain { get; set; } = [];
    }
}
