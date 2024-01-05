using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SOD.Common.Extensions;
using UniverseLib;
using UniverseLib.Utility;

namespace PresetEdit {
    public static partial class Serializer {
        public static void LoadAndOverwriteFromJsonFile(string path) {
            if (!Path.GetExtension(path).Contains("json")) {
                throw new System.Exception(
                    $"TryApplyOverwritesFromJson: File extension not supported ({path})."
                );
            }
            if (!File.Exists(path)) {
                throw new System.Exception(
                    $"TryApplyOverwritesFromJson: File does not exist ({path}).");
            }
            // var presetTypeName = Path.GetFileNameWithoutExtension(path).Split('_').First();
            // var presetType = ReflectionUtility.GetTypeByName(presetTypeName);
            // var presetInstances = RuntimeHelper.FindObjectsOfTypeAll(presetType);
            // if (presetInstances.Count() == 0) {
            //     throw new System.Exception(
            //         "TryApplyOverwritesFromJson: No preset instances matching preset type were found in game."
            //     );
            // }

            // Begin reading
            var data = File.ReadAllBytes(path);
            var reader = new Utf8JsonReader(data);

            OverwriteFromJsonReader(ref reader);
        }

        internal const int START_DEPTH = 1;

        public static void OverwriteFromJsonReader(ref Utf8JsonReader reader) {
            string gameBuildId;
            string presetEditPluginVersion;

            List<string> presetPropertyNames = new List<string>();
            List<string> presetJsonValues = new List<string>();
            Type presetType = typeof(SoCustomComparison);
            StringBuilder builder = new StringBuilder();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == START_DEPTH) {
                    // We just finished reading the last preset's data
                    if (builder.Length > 0) {
                        // finished reading the final value
                        var nextValue = builder.ToString();
                        presetJsonValues.Add(nextValue);
                        builder.Clear();
                    }

                    var presetName = presetJsonValues.ElementAt(presetPropertyNames.IndexOf("presetName"));
                    // Remove the quotation marks and commas
                    presetName = presetName.Replace("\"", "").Replace(",", "");

                    OverwritePresetPropertiesFromData(presetName, presetType, presetPropertyNames, presetJsonValues);
                    presetPropertyNames = new List<string>();
                    presetJsonValues = new List<string>();
                    presetType = typeof(SoCustomComparison);
                    builder.Clear();
                    continue;
                }
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == START_DEPTH) {
                    // We just started reading the next preset's data
                    continue;
                }
                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == START_DEPTH) {
                    var propertyName = reader.GetString();
                    switch (propertyName) {
                        case "__gameBuildId":
                            gameBuildId = reader.GetString();
                            break;
                        case "__presetEditPluginVersion":
                            presetEditPluginVersion = reader.GetString();
                            break;
                        default:
                            var presetTypeName = reader.GetString();
                            presetType = UniverseLib.ReflectionUtility.GetTypeByName(presetTypeName);
                            continue;
                    }
                }
                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == (START_DEPTH + 1)) {
                    if (builder.Length > 0) {
                        // finished reading value
                        var nextValue = builder.ToString();
                        if (nextValue.EndsWith(',')) {
                            nextValue = nextValue.Remove(nextValue.Length - 1, 1);
                        }
                        presetJsonValues.Add(nextValue);
                        builder.Clear();
                    }
                    var propertyName = reader.GetString();
                    presetPropertyNames.Add(propertyName);
                    continue;
                }
                if (reader.CurrentDepth >= (START_DEPTH + 1)) {
                    // We are reading a value
                    builder.Append(GetNextFromReaderAsJson(ref reader));
                }
            }
            // if (!presetType.IsAssignableTo(typeof(SoCustomComparison))) {
            //     throw new NotImplementedException("Can only overwrite SoCustomComparison objects.");
            // }
        }

        private static void OverwritePresetPropertiesFromData(string presetName, Type presetType, in List<string> presetPropertyNames, in List<string> presetJsonValues) {
            var presetInstance = GetPresetInstances(presetType, name => {
                return name == presetName;
            }).FirstOrDefault();
            if (presetInstance == default || presetInstance.IsNullOrDestroyed()) {
                throw new InvalidOperationException("Preset not found; ensure you have loaded or started a savegame first, and that any mods with custom presets loaded at the time of save are currently loaded.");
            }
            var zipped = presetPropertyNames.Zip(presetJsonValues);
            foreach (var (name, json) in zipped) {
                OverwriteValueOfPropertyOnInstance(presetInstance, name, json);
            }
        }

        private static string GetNextFromReaderAsJson(ref Utf8JsonReader reader) {
            switch (reader.TokenType) {
                case JsonTokenType.Comment:
                case JsonTokenType.None:
                    return string.Empty;
                case JsonTokenType.StartObject:
                    return "{";
                case JsonTokenType.EndObject:
                    return "},";
                case JsonTokenType.StartArray:
                    return "[";
                case JsonTokenType.EndArray:
                    return "],";
                case JsonTokenType.PropertyName:
                    return $"\"{reader.GetString()}\":";
                case JsonTokenType.String:
                    return $"\"{reader.GetString()}\",";
                case JsonTokenType.Number:
                    return $"{reader.GetDouble()},";
                case JsonTokenType.True:
                    return $"true,";
                case JsonTokenType.False:
                    return $"false,";
                default:
                    return $"null,";
            }
        }

        internal static JsonConverter CreateJsonSoCustomComparisonConverter(Type type) {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(JsonSoCustomComparisonConverter<>).MakeGenericType(new Type[] { type }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;
            return converter;
        }

        public static void RegisterCustomPreset<T>(T preset) where T : SoCustomComparison {
            var converter = CreateJsonSoCustomComparisonConverter(preset.GetType());
            if (!SerializerOptions.Converters.Contains(converter)) {
                SerializerOptions.Converters.Add(converter);
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
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Converters = {
                    new JsonStringEnumConverter(),
                    new JsonIntPtrIgnorer(),
                    new JsonIl2CppListConverter(),
                    new JsonSoCustomComparisonConverter<SoCustomComparison>(),
                },
            };

        public static void SerializePresetsWithTypesToFile(IEnumerable<Type> types = null) {
            if (types == null) {
                types = AllPresetTypes;
            }

            foreach (var type in types) {
                SerializePresetsWithTypeToFile(type);
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

        public static void SerializePresetsWithTypeToFile(
            Type type,
            System.Func<string, bool> presetNamePredicate = null
        ) {
            AllPresetTypes.Select(type => CreateJsonSoCustomComparisonConverter(type)).ForEach(converter => SerializerOptions.Converters.Add(converter));
            var writerOptions = new JsonWriterOptions() { Indented = false };
            var pathToDir = SOD.Common.Lib.SaveGame.GetSavestoreDirectoryPath(
                Plugin.Instance.GetType().Assembly,
                $"{Game.Instance.buildID}/"
            );
            var presetInstances = GetPresetInstances(type, presetNamePredicate);
            foreach (var presetInstance in presetInstances) {
                SerializePresetToFile(type, writerOptions, pathToDir, presetInstance);
            }
        }

        private static void SerializePresetToFile(Type type, JsonWriterOptions writerOptions, string pathToDir, object presetInstance) {
            Directory.CreateDirectory(Path.GetDirectoryName(pathToDir));
            using (var stream = File.Create(Path.Join(pathToDir, $"{type.FullName}_{((SoCustomComparison)presetInstance).presetName}.json"))) {
                using (var writer = new Utf8JsonWriter(stream, writerOptions)) {
                    writer.WriteStartObject();

                    writer.WriteString("__gameBuildId", Game.Instance.buildID);
                    writer.WriteString("__presetEditPluginVersion", MyPluginInfo.PLUGIN_VERSION);
                    // writer.WriteString("__type", type.FullName);

                    SerializePreset(writer, (SoCustomComparison)presetInstance, SerializerOptions, false, type);

                    writer.WriteEndObject();
                }
            }
        }

        public static IEnumerable<object> GetPresetInstances(Type type, Func<string, bool> presetNamePredicate) {
            IEnumerable<object> presetInstances = Helpers.GetAllUnityObjectsOfType(type);
            if (presetInstances.Count() == 0) {
                throw new System.NullReferenceException($"No presets found with type {type.FullName}.");
            }
            if (presetNamePredicate != null) {
                presetInstances = presetInstances.WhereUnityOrPresetNameMatches(presetNamePredicate);
            }
            if (presetInstances.Count() > 0) {
                return presetInstances;
            }
            throw new System.NullReferenceException($"No presets of type {type.FullName} found matching predicate.");
        }


        public static void SerializePreset(
            in Utf8JsonWriter writer,
            in SoCustomComparison presetInstance,
            JsonSerializerOptions options,
            bool isPartOfArray,
            System.Type type
        ) {
            var presetObj = presetInstance.TryCast(type);

            if (isPartOfArray) {
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
                    // It's a cycle
                    writer.WriteNullValue();
                }
            }

            writer.WriteEndObject();
        }

        public static object GetValueFromPropertyOnInstance(object instance, string propertyName) {
            var actualType = instance.GetActualType();
            var next = actualType
                .GetProperty(propertyName)
                .GetValue(instance.TryCast(actualType));
            return next;
        }

        public static void OverwriteValueOfPropertyOnInstance(object instance, string propertyName, string json) {
            var actualType = instance.GetActualType();
            var valueType = actualType.GetProperty(propertyName).PropertyType;

            var isIl2Cpp = valueType.IsExplicitlyIl2CppType();
            bool isPresetList = isIl2Cpp && valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Il2CppSystem.Collections.Generic.List<>);

            if (isIl2Cpp) {
                valueType = valueType.AttemptToConvertToManagedType();
            }

            var value = JsonSerializer.Deserialize(json, valueType, SerializerOptions);

            var instanceAsActualType = instance.TryCast(actualType);
            if (!isIl2Cpp) {
                actualType
                    .GetProperty(propertyName)
                    .SetValue(instanceAsActualType, value);
                return;
            }
            if (isPresetList) {
                var presetList = (Il2CppSystem.Collections.Generic.List<SoCustomComparison>)instanceAsActualType;
                presetList.ReplaceElements((List<SoCustomComparison>)value);
            }
        }
    }
}