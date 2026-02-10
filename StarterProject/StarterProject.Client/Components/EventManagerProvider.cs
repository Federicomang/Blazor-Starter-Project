using Microsoft.AspNetCore.Components;
using StarterProject.Client.Infrastructure;

namespace StarterProject.Client.Components
{
    public class EventManagerProvider : ComponentBase
    {
        [Inject]
        private IEventManager EventManager { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if(RendererInfo.Name == Constants.RenderModes.WebAssembly)
            {
                //await EventManager.InitClient();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(firstRender && RendererInfo.Name == Constants.RenderModes.Server)
            {
                //await EventManager.InitServer();
            }
        }
    }
}
