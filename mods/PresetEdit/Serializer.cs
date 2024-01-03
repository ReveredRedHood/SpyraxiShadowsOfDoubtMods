using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SOD.Common.Extensions;
using UniverseLib;

namespace PresetEdit {
    public static class Serializer {
        public static bool TryApplyOverwritesFromJson(string path) {
            var readerOptions = new JsonReaderOptions() { };
            if (!Path.GetExtension(path).Contains("json")) {
                Plugin.Log.LogWarning(
                    $"TryApplyOverwritesFromJson: File extension not supported ({path})."
                );
                return false;
            }
            if (!File.Exists(path)) {
                Plugin.Log.LogWarning($"TryApplyOverwritesFromJson: File does not exist ({path}).");
                return false;
            }
            var presetTypeName = Path.GetFileNameWithoutExtension(path);
            var presetType = ReflectionUtility.GetTypeByName(presetTypeName);
            var presetInstances = RuntimeHelper.FindObjectsOfTypeAll(presetType);
            if (presetInstances.Length == 0) {
                Plugin.Log.LogWarning(
                    "TryApplyOverwritesFromJson: No preset instances matching preset type were found in game."
                );
                return true;
            }
            var jsonUtf8Bytes = File.ReadAllBytes(path);

            ReadAndDeserializeJson(presetType, presetInstances, jsonUtf8Bytes, readerOptions);

            return true;
        }

        private static void ReadAndDeserializeJson(Type presetType, UnityEngine.Object[] presetInstances, byte[] jsonUtf8Bytes, JsonReaderOptions readerOptions) {
            var reader = new Utf8JsonReader(jsonUtf8Bytes, readerOptions);
            string currentPresetName = String.Empty;
            bool isNextValuePresetName = false;
            bool isArray = false;
            StringBuilder stringBuilder = new();
            List<string> propertyNames = new();
            List<string> propertyValues = new();
            int depth = 0;
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonTokenType.StartArray:
                    case JsonTokenType.StartObject: {
                        depth++;
                        isArray = reader.TokenType == JsonTokenType.StartArray && depth >= 3;
                        stringBuilder.Append(!isArray ? "{" : "[");
                        Plugin.Log.LogInfo($"mine:{depth} theirs:{reader.CurrentDepth}");
                        break;
                    }

                    case JsonTokenType.EndArray:
                    case JsonTokenType.EndObject: {
                        depth--;
                        stringBuilder.Append(!isArray ? "}" : "]");
                        if (isArray && reader.TokenType == JsonTokenType.EndArray) {
                            isArray = false;
                        }
                        switch (depth) {
                            case 2:
                                // Done reading json for this preset
                                Plugin.Log.LogInfo("hello 2");
                                OverwriteProperties(presetType, presetInstances, currentPresetName, propertyNames, propertyValues);
                                break;
                            case 3:
                                // Done reading the next json value
                                var value = stringBuilder.ToString();
                                stringBuilder.Clear();
                                propertyValues.Add(value);
                                break;
                            case >= 4:
                                stringBuilder.Append(",");
                                break;
                        }
                        break;
                    }

                    case JsonTokenType.PropertyName: {
                        var currentPropertyName = reader.GetString();
                        Plugin.Log.LogInfo($"prop name {currentPropertyName} ({depth})");
                        if (reader.ValueTextEquals(Encoding.UTF8.GetBytes("name"))) {
                            isNextValuePresetName = true;
                            break;
                        }

                        switch (depth) {
                            case 2:
                                break;
                            case 3:
                                stringBuilder.Clear();
                                propertyNames.Add(currentPropertyName);
                                break;
                            case >= 4:
                                stringBuilder.Append($"\"{currentPropertyName}\":");
                                break;
                        }

                        break;
                    }

                    case JsonTokenType.Null: {
                        Plugin.Log.LogInfo($"value=Null, ({depth})");
                        stringBuilder.Append($"null,");
                        break;
                    }

                    case JsonTokenType.String: {
                        var value = reader.GetString();
                        stringBuilder.Append($"\"{value}\",");
                        if (isNextValuePresetName) {
                            currentPresetName = value;
                            isNextValuePresetName = false;
                        }
                        break;
                    }

                    case JsonTokenType.Number: {
                        stringBuilder.Append($"{reader.GetDouble()},");
                        break;
                    }

                    case JsonTokenType.False: {
                        stringBuilder.Append($"false,");
                        break;
                    }

                    case JsonTokenType.True: {
                        stringBuilder.Append($"true,");
                        break;
                    }
                }
            }
        }

        private static void OverwriteProperties(Type presetType, UnityEngine.Object[] presetInstances, string currentPresetName, List<string> propertyNames, List<string> propertyJsonValues) {
            var instance = presetInstances.First(
                preset => preset.name == currentPresetName
            );
            foreach (var (propertyName, propertyJsonValue) in propertyNames.Zip(propertyJsonValues)) {
                Plugin.Log.LogInfo($"Type {presetType}");
                var propertyType = presetType.GetProperty(propertyName).PropertyType;
                Plugin.Log.LogInfo(propertyJsonValue);
                Plugin.Log.LogInfo($"Name: {propertyName}, Type: {presetType}/{propertyType}");
                object deserializedValue = GetCorrectDeserializedValue(propertyJsonValue, propertyType, presetType);
                SetValueOfPropertyOnInstance(instance, propertyName, deserializedValue);
            }
            propertyNames.Clear();
            propertyJsonValues.Clear();
        }

        private static object GetCorrectDeserializedValue(string propertyJsonValue, Type propertyType, Type presetType) {
            if (propertyType.IsGenericType && propertyType.FullName.StartsWith("Il2CppSystem.Collections.Generic.List")) {
                var presetNames = JsonSerializer.Deserialize<List<string>>(
                    propertyJsonValue,
                    SerializerOptions
                );
                object presets = System.Activator.CreateInstance(propertyType);

                var presetInstances = RuntimeHelper.FindObjectsOfTypeAll(presetType).Where(obj => presetNames.Contains(obj.name)).Select(presetAsObj => presetAsObj.TryCast(presetType));
                return EnumerableExtensions.ToListIl2Cpp(presetInstances);
            }
            return JsonSerializer.Deserialize(
                propertyJsonValue,
                propertyType,
                SerializerOptions
            );
        }

        private static JsonSerializerOptions SerializerOptions =>
            new JsonSerializerOptions {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
                IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true,
                WriteIndented = false,
                AllowTrailingCommas = true,
                Converters = { new JsonStringEnumConverter() }
            };

        public static void ExportSaveGamePresetData() {
            HashSet<Type> typeSet = AllPresetTypes;

            var writerOptions = new JsonWriterOptions() { Indented = false };

            foreach (var type in typeSet) {
                var path = SOD.Common.Lib.SaveGame.GetSavestoreDirectoryPath(
                    Plugin.Instance.GetType().Assembly,
                    $"{Game.Instance.buildID}/{type.Name}.json"
                );
                SavePresetsWithTypeToFile(writerOptions, type, path);
            }
        }

        private static HashSet<Type> _allPresetTypes;

        internal static HashSet<Type> AllPresetTypes {
            get {
                _allPresetTypes ??= ReflectionUtility
                .AllTypes.Values.Where(
                    type =>
                        // type.FullName.Contains("Preset")
                        type.BaseType == typeof(SoCustomComparison)
                )
                .ToHashSet();
                return _allPresetTypes;
            }
        }

        private static void SavePresetsWithTypeToFile(
            JsonWriterOptions writerOptions,
            Type type,
            string path
        ) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var presetInstances = RuntimeHelper.FindObjectsOfTypeAll(type);
            if (presetInstances.Length == 0) {
                return;
            }
            var isArray = presetInstances.Length > 1;
            using (var stream = File.Create(path)) {
                using (var writer = new Utf8JsonWriter(stream, writerOptions)) {
                    writer.WriteStartObject();
                    writer.WriteString("gameBuildId", Game.Instance.buildID);
                    if (isArray) {
                        writer.WriteStartArray(type.Name);
                    }
                    foreach (var presetInstance in presetInstances) {
                        SerializePreset(writer, presetInstance, SerializerOptions, isArray, type);
                    }
                    if (isArray) {
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                }
            }
            Plugin.Log.LogInfo($"Saved {type.Name} data to {path}");
        }

        private static void SerializePreset(
            in Utf8JsonWriter writer,
            in UnityEngine.Object presetInstance,
            JsonSerializerOptions options,
            bool isArray,
            System.Type type
        ) {
            var presetObj = presetInstance.TryCast(type);

            // stream.Write(Encoding.UTF8.GetBytes("\t\t{\n"));
            if (isArray) {
                writer.WriteStartObject();
            }
            else {
                writer.WriteStartObject(type.Name);
            }

            // Properties with public setters
            var props = type.GetProperties().Where(prop => prop.GetSetMethod() != null);
            foreach (var prop in props) {
                var name = prop.Name;
                var propType = prop.PropertyType;
                if (!GetIsSerializable(name, propType)) {
                    continue;
                }
                writer.WritePropertyName(name);
                var value = ConvertValueBeforeWrite(
                    prop.GetValue(presetObj),
                    propType,
                    out var propTypeToWrite
                );
                writer.WriteRawValue(
                    JsonSerializer.Serialize(value, propTypeToWrite, options),
                    true
                );
            }

            writer.WriteEndObject();
        }

        private static List<string> GetPresetNamesFromList(object listOfPresets, Type genericParameterType) {
            List<string> result = new();
            if (!UniverseLib.ReflectionUtility.TryGetEnumerator(listOfPresets, out var enumerator)) {
                return result;
            }
            Plugin.Log.LogInfo("Here");
            while (enumerator.MoveNext()) {
                var current = enumerator.Current;
                if (current == null) {
                    return result;
                }
                var next = GetValueFromPropertyOnInstance<string>(current, "name");
                result.Add(next);
            }
            return result;
        }

        private static object GetValueFromPropertyOnInstance(object instance, string propertyName) {
            var actualType = instance.GetActualType();
            Plugin.Log.LogInfo(actualType);
            Plugin.Log.LogInfo(instance.ToString());
            Plugin.Log.LogInfo(propertyName);
            var next = actualType
                .GetProperty(propertyName)
                .GetValue(instance.TryCast(actualType));
            return next;
        }

        private static void SetValueOfPropertyOnInstance(object instance, string propertyName, object value) {
            var actualType = instance.GetActualType();
            actualType
                .GetProperty(propertyName)
                .SetValue(instance.TryCast(actualType), value);
        }

        private static TValue GetValueFromPropertyOnInstance<TValue>(object instance, string propertyName) {
            return (TValue)GetValueFromPropertyOnInstance(instance, propertyName);
        }

        private static object ConvertValueBeforeWrite(
            object originalValue,
            Type propType,
            out Type typeToWrite
        ) {
            object result;
            var genericParameterType = GetGenericParameterTypeOrNull(propType);
            if (genericParameterType == null) {
                result = originalValue;
                typeToWrite = propType;
                return result;
            }
            typeToWrite = typeof(List<string>);
            return GetPresetNamesFromList(originalValue, genericParameterType);
        }

        private static bool GetIsSerializable(string name, Type propType) {
            Type[] serializableRefTypes = new[]
            {
                typeof(string),
                typeof(UnityEngine.Vector2),
                typeof(UnityEngine.Vector2Int),
                typeof(UnityEngine.Vector3),
                typeof(UnityEngine.Vector3Int),
                typeof(UnityEngine.Vector4),
                typeof(UnityEngine.Matrix4x4),
                typeof(UnityEngine.Quaternion),
                typeof(UnityEngine.RangeInt),
                typeof(UnityEngine.Rect),
                typeof(UnityEngine.RectInt)
            };

            bool isPreset = IsPresetType(propType);

            Type genericParameterType = GetGenericParameterTypeOrNull(propType);
            bool genericParameterIsPreset = (genericParameterType != null) && IsPresetType(genericParameterType);

            Plugin.Log.LogInfo($"{name} is {propType}");
            return (propType.IsValueType || serializableRefTypes.Contains(propType) || isPreset || genericParameterIsPreset)
                && propType != typeof(IntPtr)
                && !name.Contains("debug")
                && name != "hideFlags";
        }

        private static bool IsPresetType(Type propType) {
            return AllPresetTypes.Contains(propType);
        }

        private static Type GetGenericParameterTypeOrNull(Type propType) {
            var fullName = propType.FullName;
            if (propType.IsGenericType && fullName.StartsWith("Il2CppSystem.Collections.Generic.List")) {
                var startIdx = fullName.IndexOf("[[") + 2;
                var genericParameterTypeName = fullName.Substring(startIdx, fullName.IndexOf(',') - startIdx);
                return ReflectionUtility.GetTypeByName(genericParameterTypeName);
            }
            return null;
        }

        // Il2CppSystem.Collections.Generic.List`1[[WindowTabPreset, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]
    }
}