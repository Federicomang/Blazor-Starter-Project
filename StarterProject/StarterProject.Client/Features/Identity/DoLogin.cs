using FluentValidation;
using StarterProject.Client.Extensions;
using StarterProject.Client.Tools;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarterProject.Client.Features.Identity
{
    public class DoLogin : IBaseFeature<DoLogin.Request, DoLogin.Response>
    {
        public class Request : IBaseFeatureRequest<Response>
        {
            [JsonPropertyName("grant_type")]
            [Required]
            public virtual string GrantType { get; set; }

            [JsonPropertyName("username")]
            public virtual string? Username { get; set; }

            [JsonPropertyName("password")]
            public virtual string? Password { get; set; }

            [JsonPropertyName("client_id")]
            public virtual string? ClientId { get; set; } //Only for client_credentials

            [JsonPropertyName("client_secret")]
            public virtual string? ClientSecret { get; set; } //Only for client_credentials

            [JsonPropertyName("scope")]
            [Required]
            public virtual string Scope { get; set; }

            [JsonPropertyName("is_persistent")]
            [DefaultValue(false)]
            public virtual bool IsPersistent { get; set; } //Only for cookie
        }

        public class Response
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public long ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }
        }

        public class ErrorResponse
        {
            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        private readonly HttpClient? HttpClient;

        public const string ApiPath = "/api/identity/login";

        protected DoLogin() { }

        public DoLogin(HttpClient client)
        {
            HttpClient = client;
        }

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.Username).NotEmpty().WithMessage("Il campo e-mail non può essere vuoto")
                    .EmailAddress().WithMessage("L'indirizzo e-mail deve avere un formato corretto");
            }
        }

        public async Task<FeatureResponse<Response>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(HttpTools.ToUrlEncodedString(request), Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await HttpClient!.PostAsync(ApiPath, content, cancellationToken);
            return await response.AsFeatureResponse(content =>
            {
                FeatureResponse<Response> result;
                if (response.IsSuccessStatusCode)
                {
                    var responseData = string.IsNullOrEmpty(content)
                        ? new Response()
                        : JsonSerializer.Deserialize<Response>(content);
                    result = FeatureResponse<Response>.Create(true, responseData!);
                }
                else if (!string.IsNullOrEmpty(content))
                {
                    var errorRes = JsonSerializer.Deserialize<ErrorResponse>(content);
                    result = FeatureResponse<Response>.Create(false, null, errorRes?.Error == null ? [] : [errorRes.Error]);
                }
                else
                {
                    result = FeatureResponse<Response>.Create(false, null);
                }
                return Task.FromResult(result);
            });
        }

        public virtual Task<FeatureResponse<Response>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
