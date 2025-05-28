using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Reflection
{
    public partial class Reflector
    {
        public static class Error
        {
            public static string DataTypeIsEmpty()
                => "[Error] Data type is empty.";
            public static string NotFoundAsset(string assetPath, string assetGuid)
                => $"[Error] Asset not found. Path: {assetPath}. GUID: {assetGuid}.";

            public static string NotAllowedToModifyAssetInPackages(string assetPath)
                => $"[Error] Not allowed to modify asset in '/Packages' folder. Path: {assetPath}.";

            public static string NeitherProvided_AssetPath_AssetGuid()
                => "[Error] Neither 'assetPath' nor 'assetGuid' provided.";

            public static string NotFoundType(string typeFullName)
                => $"[Error] Type '{typeFullName}' not found.";

            public static string TargetObjectIsNull()
                => "[Error] Target object is null.";

            public static string TypeMismatch(string expectedType, string objType)
                => $"[Error] Type mismatch between '{expectedType}' (expected) and '{objType}'.";

            public static string ComponentFieldNameIsEmpty()
                => "[Error] Component field name is empty.";
            public static string ComponentFieldTypeIsEmpty()
                => "[Error] Component field type is empty.";
            public static string ComponentPropertyNameIsEmpty()
                => $"[Error] Component property name is empty. It should be a valid property name.";
            public static string ComponentPropertyTypeIsEmpty()
                => $"[Error] Component property type is empty. It should be a valid property type.";

            public static string InvalidInstanceID(Type holderType, string fieldName)
                => $"[Error] Invalid instanceID '{fieldName}' for '{holderType.FullName}'. It should be a valid field name.";
            public static string InvalidComponentPropertyType(SerializedMember serializedProperty, PropertyInfo propertyInfo)
                => $"[Error] Invalid component property type '{serializedProperty.typeName}' for '{propertyInfo.Name}'. Expected '{propertyInfo.PropertyType.FullName}'.";
            public static string InvalidComponentFieldType(SerializedMember serializedProperty, FieldInfo propertyInfo)
                => $"[Error] Invalid component property type '{serializedProperty.typeName}' for '{propertyInfo.Name}'. Expected '{propertyInfo.FieldType.FullName}'.";

            public static string NotSupportedInRuntime(Type type)
                => $"[Error] Type '{type.FullName}' is not supported in runtime for now.";

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
