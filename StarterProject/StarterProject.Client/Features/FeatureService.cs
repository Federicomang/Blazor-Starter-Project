using StarterProject.Client.Tools;
using System.Runtime.InteropServices;

namespace StarterProject.Client.Features
{
    public class FeatureService(IServiceProvider serviceProvider) : IFeatureService
    {
        public static bool IsClientEnvironment => RuntimeInformation.ProcessArchitecture == Architecture.Wasm;
        public static bool IsServerEnvironment => !IsClientEnvironment;

        public record EmptyResponse();

        private class ClientHandler<T> : IFeatureHandler<T> where T : class
        {
            private IBaseFeature Feature { get; init; }
            private object Request { get; init; }
            private CancellationToken CancellationToken { get; init; }

            internal ClientHandler(IBaseFeature feature, object request, CancellationToken cancellationToken = default)
            {
                Feature = feature;
                Request = request;
                CancellationToken = cancellationToken;
            }

            public async Task<FeatureResponse<T>> Handle()
            {
                var res = await Feature.HandleClient(Request, CancellationToken);
                return res.ConvertTo<T>();
            }
        }

        private class ServerHandler<T> : IFeatureHandler<T> where T : class
        {
            private IBaseFeature Feature { get; init; }
            private object Request { get; init; }
            private CancellationToken CancellationToken { get; init; }

            internal ServerHandler(IBaseFeature feature, object request, CancellationToken cancellationToken = default)
            {
                Feature = feature;
                Request = request;
                CancellationToken = cancellationToken;
            }

            public async Task<FeatureResponse<T>> Handle()
            {
                var res = await Feature.HandleServer(Request, CancellationToken);
                return res.ConvertTo<T>();
            }
        }

        private async Task<FeatureResponse<Response>> Run<Response>(Type requestType, IBaseFeatureRequest<Response> request, CancellationToken cancellationToken = default) where Response : class
        {
            var handlerType = ReflectionTools.GetGenericType(typeof(IBaseFeature<,>), requestType, typeof(Response));
            var handler = (IBaseFeature)serviceProvider.GetRequiredService(handlerType);

            if (IsClientEnvironment)
            {
                var clientHandler = new ClientHandler<Response>(handler, request, cancellationToken);
                return await clientHandler.Handle();
            }
            else
            {
                var serverHandler = new ServerHandler<Response>(handler, request, cancellationToken);
                var serverService = serviceProvider.GetService<IServerFeatureService>();
                if(serverService == null)
                {
                    return await serverHandler.Handle();
                }
                else
                {
                    return await serverService.HandleServer(serverHandler, requestType, request, cancellationToken);
                }
            }
        }

        public async Task<FeatureResponse<Response>> Run<Response>(IBaseFeatureRequest<Response> request, CancellationToken cancellationToken = default) where Response : class
        {
            return await Run(request.GetType(), request, cancellationToken);
        }

        public async Task<FeatureResponse<Response>> Run<Request, Response>(IBaseFeatureRequest<Response> request, CancellationToken cancellationToken = default) where Response : class where Request : class, IBaseFeatureRequest<Response>
        {
            return await Run(typeof(Request), request, cancellationToken);
        }
    }
}
