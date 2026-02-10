using static StarterProject.Client.Features.Private.EventManager;

namespace StarterProject.Client.Infrastructure
{
    public interface IEventManager
    {
        bool Subscribe<T>(string eventName, Action<T?, EventInfo> function, EventType pipeToListen = EventType.Both, string? identifier = null);

        bool Subscribe<T>(string eventName, Func<T?, EventInfo, Task> function, EventType pipeToListen = EventType.Both, string? identifier = null);

        bool Unsubscribe<T>(Action<T?, EventInfo> function);

        bool Unsubscribe<T>(Func<T?, EventInfo, Task> function);

        bool Unsubscribe(string identifier);

        bool Unsubscribe<T>(string eventName, Action<T?, EventInfo> function);

        bool Unsubscribe<T>(string eventName, Func<T?, EventInfo, Task> function);

        bool Unsubscribe(string eventName, string identifier);

        Task PublishAndWait<T>(string eventName, T? data, EventType pipeToSend = EventType.Both);

        void Publish<T>(string eventName, T? data, EventType pipeToSend = EventType.Both);
    }
}
