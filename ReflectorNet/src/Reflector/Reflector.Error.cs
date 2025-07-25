using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

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
                => "[Error] Target object is null.";

            public static string TypeMismatch(string? expectedType, string? objType)
                => $"[Error] Type mismatch between '{expectedType.ValueOrNull()}' (expected) and '{objType.ValueOrNull()}'.";

            public static string FieldNameIsEmpty()
                => "[Error] Field name is empty.";
            public static string FieldTypeIsEmpty()
                => "[Error] Field type is empty.";
            public static string PropertyNameIsEmpty()
                => "[Error] Property name is empty. It should be a valid property name.";
            public static string PropertyTypeIsEmpty()
                => "[Error] Property type is empty. It should be a valid property type.";

            public static string InvalidInstanceID(Type holderType, string? fieldName)
                => $"[Error] Invalid instanceID '{fieldName.ValueOrNull()}' for '{holderType.GetTypeName(pretty: false)}'. It should be a valid field name.";
            public static string InvalidPropertyType(SerializedMember serializedProperty, PropertyInfo propertyInfo)
                => $"[Error] Invalid property type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.PropertyType.GetTypeName(pretty: false)}' or extended from it.";
            public static string InvalidFieldType(SerializedMember serializedProperty, FieldInfo propertyInfo)
                => $"[Error] Invalid field type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.FieldType.GetTypeName(pretty: false)}' or extended from it.";

            public static string NotSupportedInRuntime(Type type)
                => $"[Error] Type '{type.GetTypeName(pretty: false).ValueOrNull()}' is not supported in runtime for now.";

            public static string MoreThanOneMethodFound(List<MethodInfo> methods)
            {
                var methodsString = JsonUtils.ToJson(methods.Select(method => new MethodDataRef(method, justRef: false)));
                return @$"[Error] Found more than one method. Only single method should be targeted. Please specify the method name more precisely.
Found {methods.Count} method(s):
```json
{methodsString}
```";
            }
        }
    }
}
