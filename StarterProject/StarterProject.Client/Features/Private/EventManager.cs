using StarterProject.Client.Attributes;
using StarterProject.Client.Extensions;
using StarterProject.Client.Infrastructure;
using System.Net.Http.Json;
using System.Text.Json;
using static StarterProject.Client.Features.FeatureService;

namespace StarterProject.Client.Features.Private
{
    [FeatureServiceLifetime(ServiceLifetime.Singleton)]
    [FeatureOtherImplementation(typeof(IEventManager))]
    public class EventManager : IBaseFeature<EventManager.Request, EmptyResponse>, IEventManager, IDisposable
    {
        protected class EventData
        {
            private readonly Func<object?, EventInfo, Task> _executor;
            public string EventName { get; init; }
            public Delegate Function { get; init; }
            public Type ParameterType { get; init; }
            public EventType EventType { get; init; }
            public string? Identifier { get; init; }

            private EventData(Func<object?, EventInfo, Task> executor, string eventName, Delegate function, Type parameterType, EventType eventType, string? identifier)
            {
                _executor = executor;
                EventName = eventName;
                Function = function;
                ParameterType = parameterType;
                EventType = eventType;
                Identifier = identifier;
            }

            public Task RunFunction(object? data, EventInfo eventInfo)
            {
                return _executor(data, eventInfo);
            }

            public static EventData From<T>(string eventName, Action<T?, EventInfo> func, EventType eventType = EventType.Both, string? identifier = null)
            {
                Task Executor(object? data, EventInfo eventInfo)
                {
                    func((T?)data, eventInfo);
                    return Task.CompletedTask;
                }
                return new EventData(Executor, eventName, func, typeof(T), eventType, identifier);
            }

            public static EventData From<T>(string eventName, Func<T?, EventInfo, Task> func, EventType eventType = EventType.Both, string? identifier = null)
            {
                Task Executor(object? data, EventInfo eventInfo)
                {
                    return func((T?)data, eventInfo);
                }
                return new EventData(Executor, eventName, func, typeof(T), eventType, identifier);
            }
        }

        public enum EventType
        {
            Client,
            Server,
            Both
        }

        public class EventInfo
        {
            public required EventType EventType { get; set; }
        }

        public class Request : IBaseFeatureRequest<EmptyResponse>
        {
            public required string EventName { get; set; }
            
            public required string JsonData { get; set; }

            public required EventType PipeToSend { get; set; }
        }

        protected readonly List<EventData> Events = [];

        private readonly HttpClient? HttpClient;

        public const string ApiPath = "/api/internal/event/send";
        public const string ApiEventPath = "/api/internal/event/pipe";
        public const string ApiEventPathEnd = "/api/internal/event/pipe/{0}";

        protected EventManager() { }

        public EventManager(IHttpClientFactory httpClientFactory)
        {
            HttpClient = httpClientFactory.CreateClient(Constants.DefaultHttpClientName);
        }

        private bool Subscribe<T>(string eventName, Delegate function, EventType pipeToListen, string ? identifier)
        {
            if(IsClientEnvironment && pipeToListen != EventType.Client)
            {

            }
            lock(Events)
            {
                if(string.IsNullOrEmpty(identifier) && Events.Any(x => x.EventName == eventName && x.Function.Target == function.Target))
                {
                    return false;
                }
                EventData eventData;
                if(function is Action<T?, EventInfo> f1)
                {
                    eventData = EventData.From(eventName, f1, pipeToListen, identifier);
                }
                else if(function is Func<T?, EventInfo, Task> f2)
                {
                    eventData = EventData.From(eventName, f2, pipeToListen, identifier);
                }
                else throw new ArgumentException("Function must be either Action<T, EventInfo> or Func<T, EventInfo, Task>");
                Events.Add(eventData);
                return true;
            }
        }

        public bool Subscribe<T>(string eventName, Action<T?, EventInfo> function, EventType pipeToListen = EventType.Both, string ? identifier = null)
        {
            return Subscribe<T>(eventName, function as Delegate, pipeToListen, identifier);
        }

        public bool Subscribe<T>(string eventName, Func<T?, EventInfo, Task> function, EventType pipeToListen = EventType.Both, string ? identifier = null)
        {
            return Subscribe<T>(eventName, function as Delegate, pipeToListen, identifier);
        }

        private bool Unsubscribe(Delegate function, string? eventName = null)
        {
            lock(Events)
            {
                int result;
                if(string.IsNullOrEmpty(eventName))
                {
                    result = Events.RemoveAll(x => x.Function.Target == function.Target);
                }
                else
                {
                    result = Events.RemoveAll(x => x.EventName == eventName && x.Function.Target == function.Target);
                }
                return result > 0;
            }
        }

        private bool UnsubscribeById(string identifier, string? eventName = null)
        {
            lock(Events)
            {
                int result;
                if(string.IsNullOrEmpty(eventName))
                {
                    result = Events.RemoveAll(x => x.Identifier == identifier);
                }
                else
                {
                    result = Events.RemoveAll(x => x.Identifier == identifier && x.EventName == eventName);
                }
                return result > 0;
            }
        }

        public bool Unsubscribe<T>(Action<T?, EventInfo> function)
        {
            return Unsubscribe(function as Delegate);
        }

        public bool Unsubscribe<T>(Func<T?, EventInfo, Task> function)
        {
            return Unsubscribe(function as Delegate);
        }

        public bool Unsubscribe(string identifier)
        {
            return UnsubscribeById(identifier);
        }

        public bool Unsubscribe<T>(string eventName, Action<T?, EventInfo> function)
        {
            return Unsubscribe(function as Delegate, eventName);
        }

        public bool Unsubscribe<T>(string eventName, Func<T?, EventInfo, Task> function)
        {
            return Unsubscribe(function as Delegate, eventName);
        }

        public bool Unsubscribe(string eventName, string identifier)
        {
            return UnsubscribeById(identifier, eventName);
        }

        public async Task PublishAndWait<T>(string eventName, T? data, EventType pipeToSend = EventType.Both)
        {
            var request = new Request()
            {
                EventName = eventName,
                JsonData = JsonSerializer.Serialize(data),
                PipeToSend = pipeToSend
            };
            if(IsClientEnvironment)
            {
                await HandleClient(request);
            }
            else
            {
                await HandleServer(request);
            }
        }

        public void Publish<T>(string eventName, T? data, EventType pipeToSend = EventType.Both)
        {
            _ = PublishAndWait(eventName, data, pipeToSend);
        }

        private async Task<FeatureResponse<EmptyResponse>> HandleClientEvents(string eventName, string jsonData, CancellationToken cancellationToken = default)
        {
            List<EventData> eventsToInvoke;
            lock(Events)
            {
                eventsToInvoke = [.. Events.Where(x => x.EventName == eventName)];
            }
            var eventInfo = new EventInfo()
            {
                EventType = EventType.Client
            };
            foreach (var eventData in eventsToInvoke)
            {
                var parameter = JsonSerializer.Deserialize(jsonData, eventData.ParameterType);
                await eventData.RunFunction(parameter, eventInfo);
            }
            return FeatureResponse<EmptyResponse>.AsSuccess(new());
        }

        private async Task<FeatureResponse<EmptyResponse>> SendToServer(Request request, CancellationToken cancellationToken = default)
        {
            var res = await HttpClient!.PostAsJsonAsync(ApiPath, request, cancellationToken);
            return await res.AsFeatureResponse<EmptyResponse>();
        }

        public async Task<FeatureResponse<EmptyResponse>> HandleClient(Request request, CancellationToken cancellationToken = default)
        {
            List<Task<FeatureResponse<EmptyResponse>>> tasks = [];
            if(request.PipeToSend == EventType.Client || request.PipeToSend == EventType.Both)
            {
                tasks.Add(HandleClientEvents(request.EventName, request.JsonData, cancellationToken));
            }
            if(request.PipeToSend == EventType.Server || request.PipeToSend == EventType.Both)
            {
                tasks.Add(SendToServer(request, cancellationToken));
            }
            var responses = await Task.WhenAll(tasks);
            if(responses.All(x => x.Success))
            {
                return FeatureResponse<EmptyResponse>.AsSuccess(new());
            }
            else
            {
                return FeatureResponse<EmptyResponse>.AsFailure(messages: responses.SelectMany(x => x.Messages));
            }
        }

        public virtual Task<FeatureResponse<EmptyResponse>> HandleServer(Request request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }
    }
}
