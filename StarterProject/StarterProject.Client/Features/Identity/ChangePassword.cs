using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Extensions;
using BlazorFeatures.Base;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using static BlazorFeatures.Base.FeatureService;

namespace StarterProject.Client.Features.Identity
{
    public class ChangePassword(IServiceProvider serviceProvider) : IBaseFeature<ChangePassword.Request, EmptyResponse>
    {
        public class Request : IBaseFeatureRequest<EmptyResponse>
        {
            public string CurrentPassword { get; set; }

            public string NewPassword { get; set; }

            public string ConfirmPassword { get; set; }
        }

        private readonly HttpClient HttpClient = serviceProvider.GetRequiredService<HttpClient>();
        private readonly JsonSerializerOptions? JsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>()?.Value;

        protected const string ApiPath = "/api/identity/changePassword";

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La password attuale non può essere vuota");
                RuleFor(x => x.NewPassword).NotEmpty().WithMessage("La nuova password non può essere vuota");

                RuleFor(x => x.NewPassword)
                   .NotEmpty()
                   .WithMessage("La nuova password non può essere vuota")
                   .Equal(x => x.ConfirmPassword)
                   .WithMessage("Le password non coincidono");
            }
        }

        public async Task<FeatureResponse<EmptyResponse>> HandleClient(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<EmptyResponse>(JsonOptions);
        }

        public virtual Task<FeatureResponse<EmptyResponse>> HandleServer(Request request, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
