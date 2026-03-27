using Microsoft.AspNetCore.Authorization;

namespace StarterProject.Features
{
    public interface IBaseFeatureAuthorization
    {
        public void BuildPolicy(AuthorizationPolicyBuilder policy);
    }
}
