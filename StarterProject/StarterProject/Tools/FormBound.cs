using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace StarterProject.Tools
{
    public sealed class FormBound<T> where T : new()
    {
        public T Value { get; }

        private FormBound(T value)
        {
            Value = value;
        }

        public static async ValueTask<FormBound<T>?> BindAsync(HttpContext context)
        {
            if (!context.Request.HasFormContentType)
                return null;

            var form = await context.Request.ReadFormAsync();

            var model = new T();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite)
                    continue;

                var name =
                    prop.GetCustomAttribute<FromFormAttribute>()?.Name
                    ?? prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? prop.Name;

                BindProperty(model, prop, name, form);
            }

            return new FormBound<T>(model);
        }

        private static void BindProperty(
            T model,
            PropertyInfo prop,
            string name,
            IFormCollection form)
        {
            if (typeof(IFormFile).IsAssignableFrom(prop.PropertyType))
            {
                prop.SetValue(model, form.Files.GetFile(name));
                return;
            }

            if (!form.TryGetValue(name, out StringValues values))
                return;

            var raw = values.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(raw))
                return;

            var targetType =
                Nullable.GetUnderlyingType(prop.PropertyType)
                ?? prop.PropertyType;

            object? value =
                targetType.IsEnum
                    ? Enum.Parse(targetType, raw, true)
                    : TypeDescriptor.GetConverter(targetType)
                        .ConvertFromInvariantString(raw);

            prop.SetValue(model, value);
        }

        public static implicit operator T(FormBound<T> formBound)
            => formBound.Value;
    }
}
