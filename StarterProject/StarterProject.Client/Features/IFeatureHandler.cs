namespace StarterProject.Client.Features
{
    public interface IFeatureHandler<T> where T : class
    {
        public Task<FeatureResponse<T>> Handle();
    }
}
