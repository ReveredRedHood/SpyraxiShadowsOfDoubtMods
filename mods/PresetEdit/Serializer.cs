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
        public static void ApplyOverwritesFromJson(string path) {
            var readerOptions = new JsonReaderOptions() { };
            if (!Path.GetExtension(path).Contains("json")) {
                throw new System.Exception(
                    $"TryApplyOverwritesFromJson: File extension not supported ({path})."
                );
            }
            if (!File.Exists(path)) {
                throw new System.Exception(
                    $"TryApplyOverwritesFromJson: File does not exist ({path}).");
            }
            var presetTypeName = Path.GetFileNameWithoutExtension(path);
            var presetType = ReflectionUtility.GetTypeByName(presetTypeName);
            var presetInstances = RuntimeHelper.FindObjectsOfTypeAll(presetType);
            if (presetInstances.Length == 0) {
                throw new System.Exception(
                    "TryApplyOverwritesFromJson: No preset instances matching preset type were found in game."
                );
            }
        }

        private static JsonSerializerOptions SerializerOptions =>
            new JsonSerializerOptions {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
                IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true,
                WriteIndented = false,
                AllowTrailingCommas = true,
                MaxDepth = 4,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Converters = {
                    new JsonStringEnumConverter(),
                    new JsonIntPtrIgnorer(),
                    new JsonIl2CppListConverter(),
                    new JsonIl2CppIListConverter(),
                    new JsonSoCustomComparisonConverter()
                }
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
                if (propType.IsPointer || name.Contains("Ptr") || name.Contains("ptr")) {
                    continue;
                }
                writer.WritePropertyName(name);
                try {
                    var value = JsonSerializer.Serialize(prop.GetValue(presetObj), propType, options);
                    writer.WriteRawValue(value, true);
                }
                catch (JsonException exception) {
                    if (!exception.Message.Contains("cycle")) {
                        throw;
                    }
                    // Plugin.Log.LogWarning($"Possible cycle: {name} ({propType})");
                    writer.WriteNullValue();
                }
            }

            writer.WriteEndObject();
        }

        private static object GetValueFromPropertyOnInstance(object instance, string propertyName) {
            var actualType = instance.GetActualType();
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
    }
}