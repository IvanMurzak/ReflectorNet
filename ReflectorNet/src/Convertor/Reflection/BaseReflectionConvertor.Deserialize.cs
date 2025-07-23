using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual object? Deserialize(Reflector reflector, SerializedMember data, ILogger? logger = null)
        {
            if (!data.TryDeserialize(out var result, logger: logger))
                return null;

            var type = result!.GetType();

            if (data.fields != null)
            {
                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                        continue;

                    if (!field.TryDeserialize(out var parsedValue, logger: logger))
                        continue;

                    var fieldInfo = type.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                        fieldInfo.SetValue(result, parsedValue);
                }
            }
            if (data.props != null)
            {
                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                        continue;

                    if (!property.TryDeserialize(out var parsedValue, logger: logger))
                        continue;

                    var propertyInfo = type.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                        propertyInfo.SetValue(result, parsedValue);
                }
            }

            return result;
        }
    }
}