using Microsoft.AspNetCore.Components;
using MudBlazor;
using StarterProject.Client.Features.Identity;

namespace StarterProject.Client.Pages.Identity
{
    public partial class Login
    {
        [SupplyParameterFromForm]
        private DoLogin.Request Data { get; set; }

        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }

        [Inject]
        private ILogger<Login> Logger { get; set; }

        private List<string> ErrorMessages { get; set; } = [];
        private string _background = "/images/sfondo.jpg";


        protected override void OnInitialized() => Data ??= new() { GrantType = "cookie", Scope = "api" };
        private string _logo = "/images/logo.png";
        private string _marginLogo = "mt-n0";
        private string year = DateTime.Now.Year.ToString();
        private string _payoff = "Qualcosa srl";


        //private readonly ILogger<Login> _logger;

        //public Login(ILogger<Login> logger)
        //{
        //    _logger = logger;
        //}

        protected override async Task OnInitializedAsync()
        {
            FillAdministratorCredentials();
        }

        private async Task HandleSubmit()
        {
            ErrorMessages = [];
            var response = await FeatureService.Run(Data);
            if (response.Success)
            {
                NavigationManager.NavigateTo(NavigationManager.Uri, RendererInfo.IsInteractive);
            }
            else
            {
                ErrorMessages = response.Messages;
            }
        }

        private string GetReturnUrl() => string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl;

        private bool _passwordVisibility;
        private InputType _passwordInput = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        void TogglePasswordVisibility()
        {
            if (_passwordVisibility)
            {
                _passwordVisibility = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _passwordVisibility = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }
        }

        void Service()
        {
            //Questa inutile funzione serve affinchè, nel front end, il button abbia la stessa dimensione di quello della visibilità password.
            //Si nota la differenza solo quando il form è già popolato dal gestore password di Google e si vede lo sfondo azzurro
            //Eliminate anche le label che si sovrapponevano a quanto compilato dal gestore password google
        }

        private void FillAdministratorCredentials()
        {
            Data.Username = "test@test.test";
            Data.Password = "Test123!";
        }

        private void FillBasicUserCredentials()
        {
            Data.Username = "";
            Data.Password = "Test123!";
        }
    }
}