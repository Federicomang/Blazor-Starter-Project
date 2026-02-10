using Microsoft.AspNetCore.Components;
using MudBlazor;
using StarterProject.Client.Features.Identity;

namespace StarterProject.Client.Pages.Identity
{
    public partial class ChangePasswordPage
    {
        [SupplyParameterFromForm]
        private ChangePassword.Request Data { get; set; }

        private List<string> ErrorMessages { get; set; } = [];

        protected override void OnInitialized() => Data ??= new();
        
        private async Task HandleSubmit()
        {
            ErrorMessages = [];

            //Controllo se le password coincidono

            var response = await FeatureService.Run(Data);
            if (response.Success)
            {
                Snackbar.Add("Password cambiata con successo", Severity.Success);
            }
            else
            {
                ErrorMessages = response.Messages;
            }
        }
    }
}