using BlazorFeatures.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace StarterProject.Extensions
{
    public static class FeatureCallerContextExtensions
    {
        private static T? GetValue<T>(this FeatureCallerContextBuilder context, string key)
        {
            if(context.Values.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        private static void SetValue<T>(this FeatureCallerContextBuilder context, string key, T? value)
        {
            if(value is null)
            {
                context.Values.Remove(key);
            }
            else
            {
                context.Values[key] = value;
            }
        }

        private static T? GetValue<T>(this FeatureCallerContext context, string key)
        {
            if (context.Values.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        extension(FeatureCallerContextBuilder context)
        {
            public IJSRuntime? JSRuntime
            {
                get => context.GetValue<IJSRuntime>("JSRuntime");
                set => context.SetValue("JSRuntime", value);
            }
            public string? BaseUri
            {
                get => context.GetValue<string?>("NavigationBaseUri");
                set => context.SetValue("NavigationBaseUri", value);
            }
        }

        extension(FeatureCallerContext context)
        {
            public IJSRuntime? JSRuntime => context.GetValue<IJSRuntime>("JSRuntime");
            public string? BaseUri => context.GetValue<string?>("NavigationBaseUri");
        }
    }
}
