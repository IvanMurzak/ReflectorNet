#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Reflection.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual object? Deserialize(Reflector reflector, SerializedMember data, ILogger? logger = null)
        {
            var type = TypeUtils.GetType(data.typeName);
            if (type == null)
                return null;

            var result = data.valueJsonElement != null
                ? JsonUtils.Deserialize(data.valueJsonElement.Value, type)
                : TypeUtils.GetDefaultValue(type);

            if (data.fields != null)
            {
                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                        continue;

                    var fieldType = TypeUtils.GetType(field.typeName);
                    if (fieldType == null)
                        continue;

                    var fieldValue = field.valueJsonElement != null
                        ? JsonUtils.Deserialize(field.valueJsonElement.Value, fieldType)
                        : TypeUtils.GetDefaultValue(fieldType);

                    var fieldInfo = type.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                        fieldInfo.SetValue(result, fieldValue);
                }
            }
            if (data.props != null)
            {
                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                        continue;

                    var fieldType = TypeUtils.GetType(property.typeName);
                    if (fieldType == null)
                        continue;

                    var fieldValue = property.valueJsonElement != null
                        ? JsonUtils.Deserialize(property.valueJsonElement.Value, fieldType)
                        : TypeUtils.GetDefaultValue(fieldType);

                    var propertyInfo = type.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                        propertyInfo.SetValue(result, fieldValue);
                }
            }

            return result;
        }
    }
}