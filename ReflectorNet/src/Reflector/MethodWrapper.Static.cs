using System.Reflection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class MethodWrapper
    {
        public static MethodWrapper Create(Reflector reflector, ILogger? logger, MethodInfo methodInfo)
        {
            return methodInfo.IsStatic
                ? new MethodWrapper(reflector, logger, methodInfo)
                : new MethodWrapper(reflector, logger, methodInfo.DeclaringType!, methodInfo);
        }
        public static MethodWrapper CreateFromInstance(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo)
        {
            return methodInfo.IsStatic
                ? new MethodWrapper(reflector, logger, methodInfo)
                : new MethodWrapper(reflector, logger, targetInstance, methodInfo);
        }
    }
}