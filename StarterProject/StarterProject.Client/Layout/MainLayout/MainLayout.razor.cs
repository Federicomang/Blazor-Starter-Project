using MudBlazor;

namespace StarterProject.Client.Layout.MainLayout
{
    public partial class MainLayout
    {
        private bool _drawerOpen = true;

        private MudTheme _currentTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
            }
        };

        private string _title = "StarterProject";
    }

}