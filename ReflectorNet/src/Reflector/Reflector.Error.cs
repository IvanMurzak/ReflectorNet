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
            public static string NotFoundAsset(string? assetPath, string? assetGuid)
                => $"[Error] Asset not found. Path: {assetPath.ValueOrNull()}. GUID: {assetGuid.ValueOrNull()}.";

            public static string NotAllowedToModifyAssetInPackages(string? assetPath)
                => $"[Error] Not allowed to modify asset in '/Packages' folder. Path: {assetPath.ValueOrNull()}.";

            public static string NeitherProvided_AssetPath_AssetGuid()
                => "[Error] Neither 'assetPath' nor 'assetGuid' provided.";

            public static string NotFoundType(string? typeFullName)
                => $"[Error] Type '{typeFullName.ValueOrNull()}' not found.";

            public static string TargetObjectIsNull()
                => "[Error] Target object is null.";

            public static string TypeMismatch(string? expectedType, string? objType)
                => $"[Error] Type mismatch between '{expectedType.ValueOrNull()}' (expected) and '{objType.ValueOrNull()}'.";

            public static string ComponentFieldNameIsEmpty()
                => "[Error] Component field name is empty.";
            public static string ComponentFieldTypeIsEmpty()
                => "[Error] Component field type is empty.";
            public static string ComponentPropertyNameIsEmpty()
                => $"[Error] Component property name is empty. It should be a valid property name.";
            public static string ComponentPropertyTypeIsEmpty()
                => $"[Error] Component property type is empty. It should be a valid property type.";

            public static string InvalidInstanceID(Type holderType, string? fieldName)
                => $"[Error] Invalid instanceID '{fieldName.ValueOrNull()}' for '{holderType.FullName}'. It should be a valid field name.";
            public static string InvalidComponentPropertyType(SerializedMember serializedProperty, PropertyInfo propertyInfo)
                => $"[Error] Invalid component property type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.PropertyType.FullName}'.";
            public static string InvalidComponentFieldType(SerializedMember serializedProperty, FieldInfo propertyInfo)
                => $"[Error] Invalid component property type '{serializedProperty.typeName.ValueOrNull()}' for '{propertyInfo.Name}'. Expected '{propertyInfo.FieldType.FullName}'.";

            public static string NotSupportedInRuntime(Type type)
                => $"[Error] Type '{type.FullName.ValueOrNull()}' is not supported in runtime for now.";

            public static string MoreThanOneMethodFound(List<MethodInfo> methods)
            {
                var methodsString = JsonUtils.Serialize(methods.Select(method => new MethodDataRef(method, justRef: false)));
                return @$"[Error] Found more than one method. Only single method should be targeted. Please specify the method name more precisely.
Found {methods.Count} method(s):
```json
{methodsString}
```";
            }
        }
    }
}
