using System;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        protected const int MAX_DEPTH = 10000;

        public virtual bool AllowCascadeSerialize => false;
        public virtual bool AllowCascadePopulate => false;

        public virtual bool AllowCascadeFieldsConversion => true;
        public virtual bool AllowCascadePropertiesConversion => true;

        public virtual int SerializationPriority(Type type, ILogger? logger = null)
        {
            if (type == typeof(T))
                return MAX_DEPTH + 1;

            var distance = TypeUtils.GetInheritanceDistance(baseType: typeof(T), targetType: type);

            return distance >= 0
                ? MAX_DEPTH - distance
                : 0;
        }
    }
}