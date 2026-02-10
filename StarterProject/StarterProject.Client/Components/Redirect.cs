using Microsoft.AspNetCore.Components;

namespace StarterProject.Client.Components
{
    public class Redirect : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        [Parameter, EditorRequired] public required string To { get; set; }

        protected override void OnInitialized()
        {
            if(RendererInfo.IsInteractive)
            {
                NavigationManager.NavigateTo(To);
            }
        }
    }
}
