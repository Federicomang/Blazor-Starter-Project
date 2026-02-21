using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using OpenIddict.Abstractions;
using CreateUserRequest = StarterProject.Client.Features.Identity.CreateUser.Request;

namespace StarterProject.Client.Pages.Identity
{
    public partial class CreateUser
    {
        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue)]
        public List<string>? MyRoles { get; set; }

        public CreateUserRequest User { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            if (MyRoles == null)
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                MyRoles = authState.User?.Claims.Where(x => x.Type == OpenIddictConstants.Claims.Role).Select(x => x.Value).ToList() ?? [];
            }
        }

        private async Task SaveAsync()
        {
            var response = await FeatureService.Run(User);
            if(response.Success)
            {
                Snackbar.Add(_localizer["User created successfully"], Severity.Success);
                NavigationManager.NavigateTo($"/users/edit/{response.Data!.Id}");
            }
            else
            {
                foreach(var message in response.Messages)
                {
                    Snackbar.Add(message, Severity.Error);
                }
            }
        }
    }
}
