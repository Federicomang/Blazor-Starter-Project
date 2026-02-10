using FluentValidation;
using StarterProject.Client.Extensions;
using System.Net.Http.Json;

namespace StarterProject.Client.Features.Identity
{
    public class ChangePassword : IBaseFeature<ChangePassword.Request, ChangePassword.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            public string CurrentPassword { get; set; }

            public string NewPassword { get; set; }

            public string ConfirmPassword { get; set; }
        }

        public record Response();

        private readonly HttpClient? HttpClient;

        protected const string ApiPath = "/api/identity/changePassword";

        protected ChangePassword() { }

        public ChangePassword(HttpClient client)
        {
            HttpClient = client;
        }

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

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var response = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await response.AsFeatureResponse<Response>();
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
