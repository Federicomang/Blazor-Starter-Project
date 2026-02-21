using MudBlazor;
using StarterProject.Client.Components;
using StarterProject.Client.Features;
using StarterProject.Client.Features.Identity;
using StarterProject.Client.Features.Identity.Models;

namespace StarterProject.Client.Pages.Identity
{
    public partial class Users
    {
        private MudTable<UserInfo>? table;
        private string searchString = string.Empty;

        private List<UserInfo> UsersList { get; set; } = [];

        private int TotalItems { get; set; }

        private void ProcessResponse(FeatureResponse<GetUsers.Response> response)
        {
            if (response.Success)
            {
                UsersList = response.Data?.Users.Items ?? [];
                TotalItems = response.Data?.Users.TotalCount ?? 0;
            }
            else
            {
                foreach (var message in response.Messages)
                {
                    Snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task<TableData<UserInfo>> ServerReload(TableState state, CancellationToken token)
        {
            var request = new GetUsers.Request()
            {
                SearchString = searchString
            };
            request.FromTableState(state);
            var response = await FeatureService.Run(request, token);
            ProcessResponse(response);
            return new TableData<UserInfo>()
            {
                Items = UsersList,
                TotalItems = TotalItems
            };
        }

        private void Search(string value)
        {
            searchString = value;
            _ = table?.ReloadServerData();
        }

        private void EditUser(UserInfo user)
        {
            NavigationManager.NavigateTo($"/users/edit/{user.Id}");
        }

        private async Task DeleteUser(UserInfo user)
        {
            var parameters = new DialogParameters
            {
                { nameof(GenericModal.Text), localizer["ConfirmDeleteUser"].ToString() }
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true, BackdropClick = false };
            var dialog = await DialogService.ShowAsync<GenericModal>(_globalLocalizer["Warning"], parameters, options);
            var dialogResult = await dialog.Result;

            if (dialogResult != null && !dialogResult.Canceled)
            {
                var res = await FeatureService.Run(new DeleteUser.Request() { UserId = user.Id });
                if(res.Success)
                {
                    Snackbar.Add(localizer["DeleteUserSuccess"], Severity.Success);
                    _ = table?.ReloadServerData();
                }
                else
                {
                    foreach(var message in res.Messages)
                    {
                        Snackbar.Add(message, Severity.Error);
                    }
                }
            }
        }

        private void CreateUser()
        {
            NavigationManager.NavigateTo("/users/create");
        }
    }
}
