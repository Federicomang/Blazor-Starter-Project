using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace StarterProject.Client.Layout.MainLayout
{
    public partial class MainLayout
    {
        private bool _drawerOpen = true;

        [CascadingParameter(Name = Constants.ApplicationRenderTypeKey)]
        private RenderType ApplicationRenderType { get; set; }

        private MudTheme _currentTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
            }
        };

        private string _title = "StarterProject";
    }

}