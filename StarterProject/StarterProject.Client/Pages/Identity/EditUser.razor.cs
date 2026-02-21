using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using OpenIddict.Abstractions;
using StarterProject.Client.Features.Identity;
using EditUserRequest = StarterProject.Client.Features.Identity.EditUser.Request;

namespace StarterProject.Client.Pages.Identity
{
    public partial class EditUser
    {
        [Parameter]
        public string Id { get; set; }

        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue)]
        public List<string>? MyRoles { get; set; }

        [PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue)]
        public EditUserRequest? User { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrEmpty(Id))
            {
                NavigationManager.NavigateTo("/users");
            }
            else if(User == null)
            {
                var response = await FeatureService.Run(new GetUser.Request() { UserId = Id });
                if (response.Success)
                {
                    User = new() { UserInfo = response.Data!.User };
                }
                else
                {
                    foreach (var message in response.Messages)
                    {
                        Snackbar.Add(message, Severity.Error);
                    }
                }
            }
            if (MyRoles == null)
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                MyRoles = authState.User?.Claims.Where(x => x.Type == OpenIddictConstants.Claims.Role).Select(x => x.Value).ToList() ?? [];
            }
        }

        private async Task SaveAsync()
        {
            var response = await FeatureService.Run(User!);
            if(response.Success)
            {
                Snackbar.Add(_localizer["User edit successfully"], Severity.Success);
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
