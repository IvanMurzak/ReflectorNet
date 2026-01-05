using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        public static class Error
        {
            public static string DataTypeIsEmpty()
                => "[Error] Data type is empty.";

            public static string NotFoundType(string? typeFullName)
                => $"[Error] Type '{typeFullName.ValueOrNull()}' not found.";

            public static string TargetObjectIsNull()
                => "Target object is null.";

            public static string TypeMismatch(string? expectedType, string? objType)
                => $"Type mismatch between '{expectedType.ValueOrNull()}' (expected) and '{objType.ValueOrNull()}'.";

            public static string FieldNameIsEmpty()
                => "[Error] Field name is empty.";
            public static string FieldTypeIsEmpty()
                => "[Error] Field type is empty.";
            public static string PropertyNameIsEmpty()
                => "[Error] Property name is empty. It should be a valid property name.";
            public static string PropertyTypeIsEmpty()
                => "[Error] Property type is empty. It should be a valid property type.";

            public static string InvalidInstanceID(Type holderType, string? fieldName)
                => $"[Error] Invalid instanceID '{fieldName.ValueOrNull()}' for '{holderType.GetTypeId()}'. It should be a valid field name.";
            public static string InvalidPropertyType(SerializedMember serializedProperty, PropertyInfo propertyInfo)
                => $"[Error] Invalid property type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.PropertyType.GetTypeId()}' or extended from it.";
            public static string InvalidFieldType(SerializedMember serializedProperty, FieldInfo propertyInfo)
                => $"[Error] Invalid field type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.FieldType.GetTypeId()}' or extended from it.";

            public static string NotSupportedInRuntime(Type type)
                => $"[Error] Type '{type.GetTypeId().ValueOrNull()}' is not supported in runtime for now.";

            public static string MoreThanOneMethodFound(Reflector reflector, List<MethodInfo> methods, ILogger? logger = null)
            {
                var methodDataList = methods.Select(method => new MethodData(reflector, method));
                var methodsString = methodDataList.ToJson(reflector, logger: logger);

                return @$"[Error] Found more than one method. Only single method should be targeted. Please specify the method name more precisely.
Found {methods.Count} method(s):
```json
{methodsString}
```";
            }
        }
    }
}
