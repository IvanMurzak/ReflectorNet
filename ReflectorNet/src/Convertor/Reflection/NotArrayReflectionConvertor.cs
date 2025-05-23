#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Reflection.Convertor
{
    public abstract class NotArrayReflectionConvertor<T> : BaseReflectionConvertor<T>
    {
        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            var distance = TypeUtils.GetInheritanceDistance(baseType: typeof(T), targetType: type);
            if (distance >= 0)
                return MAX_DEPTH - distance;

            var isArray = type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
            return isArray
                ? 0
                : base.SerializationPriority(type);
        }
    }
}