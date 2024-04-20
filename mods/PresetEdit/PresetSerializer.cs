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

namespace PresetEdit;

public static class PresetSerializer {
    private static readonly int START_DEPTH = 1;
    public static readonly string CUSTOM_INSTRUCTION_SUFFIX = "__CUSTOM";
    public static readonly string NOT_SUPPORTED_FLAG = "NOT_SUPPORTED";
    private static bool _convertersAdded = false;
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
        var converter = CreateJsonSoCustomComparisonConverter(preset.GetActualType());
        if (!SerializerOptions.Converters.Contains(converter)) {
            SerializerOptions.Converters.Add(converter);
        }
    }
    private static JsonSerializerOptions _serializerOptions;
    public static JsonSerializerOptions SerializerOptions {
        get {
            _serializerOptions ??= new JsonSerializerOptions {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
                IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true,
                WriteIndented = false,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Converters = {
                    new JsonStringEnumConverter(),
                    // new JsonUnityScriptableObjConverter(),
                    new JsonIl2CppEnumerableConverter(),
                    // ignored:
                    new JsonTypeIgnorer<IntPtr>(),
                    new JsonTypeIgnorer<UnityEngine.GameObject>(),
                    new JsonTypeIgnorer<UnityEngine.Component>(),
                    new JsonTypeIgnorer<UnityEngine.ScriptableObject>(),
                    new JsonTypeIgnorer<UnityEngine.BillboardAsset>(),
                    new JsonTypeIgnorer<UnityEngine.Cubemap>(),
                    new JsonTypeIgnorer<UnityEngine.CubemapArray>(),
                    new JsonTypeIgnorer<UnityEngine.CubemapFace>(),
                    new JsonTypeIgnorer<UnityEngine.CustomRenderTexture>(),
                    new JsonTypeIgnorer<UnityEngine.Gradient>(),
                    new JsonTypeIgnorer<UnityEngine.GraphicsBuffer>(),
                    new JsonTypeIgnorer<UnityEngine.LOD>(),
                    new JsonTypeIgnorer<UnityEngine.LightmapData>(),
                    new JsonTypeIgnorer<UnityEngine.LowerResBlitTexture>(),
                    new JsonTypeIgnorer<UnityEngine.Mesh>(),
                    new JsonTypeIgnorer<UnityEngine.RenderTexture>(),
                    new JsonTypeIgnorer<UnityEngine.Shader>(),
                    new JsonTypeIgnorer<UnityEngine.Sprite>(),
                    new JsonTypeIgnorer<UnityEngine.TextAsset>(),
                    new JsonTypeIgnorer<UnityEngine.Texture2D>(),
                    new JsonTypeIgnorer<UnityEngine.Texture2DArray>(),
                    new JsonTypeIgnorer<UnityEngine.Texture3D>(),
                    new JsonTypeIgnorer<UnityEngine.Texture>(),
                    new JsonTypeIgnorer<byte[]>(),
                },
            };
            return _serializerOptions;
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

    private static readonly JsonOverwriteAdapter _defaultAdapter = new JsonOverwriteAdapter();

    public static void ReadFromJson(IEnumerable<string> paths, JsonOverwriteAdapter overwriteAdapter = null) {
        foreach (var path in paths) {
            ReadFromJson(path, overwriteAdapter);
        }
    }

    public static void ReadFromJson(string path, JsonOverwriteAdapter overwriteAdapter = null) {
        AddConvertersIfNotAdded();
        if (overwriteAdapter == null) {
            overwriteAdapter = _defaultAdapter;
        }
        if (!Path.GetExtension(path).Contains("json")) {
            throw new System.Exception($"TryApplyOverwritesFromJson: File extension not supported ({path}).");
        }
        if (!File.Exists(path)) {
            throw new System.Exception($"TryApplyOverwritesFromJson: File does not exist ({path}).");
        }

        // Set up for read
        var data = File.ReadAllBytes(path);
        var reader = new Utf8JsonReader(data);

        string gameBuildId = string.Empty;
        string presetEditPluginVersion = string.Empty;

        List<string> presetPropertyNames = new();
        List<string> presetJsonValues = new();
        Dictionary<string, string> presetCustomInstructions = new();
        bool isCustomInstruction = false;
        string customInstructionPropertyName = string.Empty;
        Type presetType = typeof(SoCustomComparison);
        StringBuilder builder = new StringBuilder();

        // Read
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == START_DEPTH) {
                // We just started reading the next preset's data
            }
            else if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == START_DEPTH) {
                if (builder.Length > 0) {
                    FinishReadingValue(presetJsonValues, presetCustomInstructions, builder, ref isCustomInstruction, customInstructionPropertyName);
                }
                // We just finished reading the last preset's data
                Plugin.Log.LogDebug($"Finished reading data for preset (gameBuildId: {gameBuildId}, presetEditPluginVersion: {presetEditPluginVersion})");

                ConvertJsonDataForPreset(presetPropertyNames, presetJsonValues, presetCustomInstructions, presetType, overwriteAdapter, gameBuildId, presetEditPluginVersion);
                presetPropertyNames = new List<string>();
                presetJsonValues = new List<string>();
                presetType = typeof(SoCustomComparison);
                builder.Clear();
            }
            else if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == START_DEPTH) {
                // we just started reading a metadata property name
                var propName = reader.GetString();
                if (propName == "__gameBuildId") {
                    reader.Read();
                    gameBuildId = reader.GetString();
                }
                else if (propName == "__presetEditPluginVersion") {
                    reader.Read();
                    presetEditPluginVersion = reader.GetString();
                }
                else {
                    var presetTypeName = reader.GetString().Split("_").First();
                    presetType = ReflectionUtility.GetTypeByName(presetTypeName);
                }
            }
            else if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == (START_DEPTH + 1)) {
                if (builder.Length > 0) {
                    FinishReadingValue(presetJsonValues, presetCustomInstructions, builder, ref isCustomInstruction, customInstructionPropertyName);
                }
                // we just started reading a preset property name
                var propName = reader.GetString();
                if (propName.EndsWith(CUSTOM_INSTRUCTION_SUFFIX)) {
                    isCustomInstruction = true;
                    customInstructionPropertyName = propName.Split(CUSTOM_INSTRUCTION_SUFFIX, StringSplitOptions.RemoveEmptyEntries).First();
                    continue;
                }
                presetPropertyNames.Add(propName);
            }
            else if (reader.CurrentDepth >= (START_DEPTH + 1)) {
                // We are reading a value
                builder.Append(GetNextFromReaderAsJson(ref reader));
            }
        }
    }

    private static void FinishReadingValue(List<string> presetJsonValues, Dictionary<string, string> presetCustomInstructions, StringBuilder builder, ref bool isCustomInstruction, string customInstructionPropertyName) {
        // finished reading value
        var nextValue = builder.ToString();
        if (nextValue.EndsWith(',')) {
            nextValue = nextValue.Remove(nextValue.Length - 1, 1);
        }
        builder.Clear();
        if (!isCustomInstruction) {
            presetJsonValues.Add(nextValue);
            isCustomInstruction = false;
            return;
        }
        presetCustomInstructions[customInstructionPropertyName] = RemoveQuotesAroundStringValue(nextValue);
        isCustomInstruction = false;
    }

    private static string RemoveQuotesAroundStringValue(string original) {
        // Only remove the quotes if they are the first and last characters
        if (original[0] == '"' && original.Last() == '"') {
            return original.Substring(1, original.Length - 2);
        }
        return original;
    }

    private static void ConvertJsonDataForPreset(List<string> presetPropertyNames, List<string> presetJsonValues, Dictionary<string, string> presetCustomInstructions, Type presetType, JsonOverwriteAdapter overwriteAdapter, string gameBuildId, string presetEditPluginVersion) {
        var presetName = presetJsonValues.ElementAt(presetPropertyNames.IndexOf("presetName"));
        // Remove the quotation marks and commas
        presetName = presetName.Replace("\"", "").Replace(",", "");

        var presetInstances = Helpers.GetPresetInstances(presetType, name => { return name == presetName; }, false);
        if (presetInstances.Count() == 0) {
            throw new InvalidOperationException($"Preset {presetName} not found; ensure you have loaded or started a savegame first, and that any mods with custom presets loaded at the time of save are currently loaded.");
        }
        var zipped = presetPropertyNames.Zip(presetJsonValues);
        foreach (var presetInstance in presetInstances) {
            foreach (var (propName, json) in zipped) {
                if (json.Contains(NOT_SUPPORTED_FLAG)) {
                    continue;
                }
                if (presetCustomInstructions.TryGetValue(propName, out var customInstruction)) {
                    ConvertJsonDataForPresetInner(presetInstance, propName, json, customInstruction, overwriteAdapter, gameBuildId, presetEditPluginVersion);
                    continue;
                }
                ConvertJsonDataForPresetInner(presetInstance, propName, json, null, overwriteAdapter, gameBuildId, presetEditPluginVersion);
            }
        }
    }

    private static void ConvertJsonDataForPresetInner(object presetInstance, string propName, string json, string customInstruction, JsonOverwriteAdapter overwriteAdapter, string gameBuildId, string presetEditPluginVersion) {
        if (propName == "name") {
            // use presetName only, not name
            return;
        }
        if (propName == "presetName") {
            // handle both name and presetName at once, using the same json value
            ConvertJsonDataForPresetInner(presetInstance, "name", json, customInstruction, overwriteAdapter, gameBuildId, presetEditPluginVersion);
        }
        if (json == "null") {
            return;
        }
        var actualPresetType = presetInstance.GetActualType();
        var actualPropertyInfo = actualPresetType.GetProperty(propName);
        var currentValue = actualPropertyInfo.GetValue(presetInstance.TryCast(actualPresetType));
        var actualValueType = actualPropertyInfo.PropertyType;

        var isGenericType = actualValueType.IsGenericType;
        var isIl2Cpp = actualValueType.IsExplicitlyIl2CppType();

        Type valueManagedType = null;
        object currentValueManaged = null;
        if (isIl2Cpp) {
            valueManagedType = actualValueType.AttemptToConvertToManagedType();
            currentValueManaged = currentValue.TryCast(actualValueType);
        }
        valueManagedType ??= actualValueType;
        currentValueManaged ??= currentValue;

        var incomingValue = JsonSerializer.Deserialize(json, valueManagedType, SerializerOptions);
        var resultValue = overwriteAdapter.GetResultValue(propName, valueManagedType, currentValueManaged, incomingValue, customInstruction, gameBuildId, presetEditPluginVersion);
        if (resultValue == currentValueManaged) {
            // no change
            return;
        }
        if (isGenericType) {
            // we received a managed collection, so override the unmanaged list elements
            var resultAsList = resultValue.FormListIfEnumerable();
            Helpers.OverwriteIl2CppCollectionContents(actualPropertyInfo.GetValue(presetInstance.TryCast(actualPresetType)), resultAsList);
            return;
        }
        actualPropertyInfo.SetValue(presetInstance.TryCast(actualPresetType), resultValue);
    }

    private static void AddConvertersIfNotAdded() {
        if (_convertersAdded) {
            return;
        }
        _convertersAdded = true;
        var objs = Helpers.GetPresetInstances(typeof(SoCustomComparison), disallowDupes: false);
        var types = objs.Select(obj => obj.GetActualType()).Distinct();
        types.Select(type => CreateJsonSoCustomComparisonConverter(type)).ForEach(converter => SerializerOptions.Converters.Add(converter));
    }

    // public static void WriteToJsonAll() {
    //     var presets = Helpers.GetPresetInstances(typeof(SoCustomComparison));
    //     WriteToJson(presets);
    // }

    // private static Coroutine _writeManyCoroutine;

    // public static void WriteToJson(IEnumerable<object> instances, Func<string, bool> presetNamePredicate = null, bool singleFile = false) {
    //     var set = instances.DistinctBy(preset => Helpers.GetPresetKey(preset));
    //     if (_writeManyCoroutine == null || _writeManyCoroutine == default || _writeManyCoroutine.IsDone()) {
    //         _writeManyCoroutine = UniverseLib.RuntimeHelper.StartCoroutine(WriteToJsonCoroutine(set, singleFile));
    //     }
    // }

    // Run as a coroutine to avoid locking the game up (even though it may run slowly, we don't want to crash)
    // private static IEnumerator WriteToJsonCoroutine(IEnumerable<object> presetSet, bool singleFile) {
    //     Plugin.Log.LogInfo($"NOTE: There are {presetSet.Count()} presets. This may take a while...");
    //     foreach (var preset in presetSet) {
    //         WriteToJson(preset);
    //         yield return new WaitForEndOfFrame();
    //     }
    // }

    // public static void WriteToJson(object instance = null, Func<string, bool> presetNamePredicate = null) {
    //     AddConvertersIfNotAdded();
    //     var pathToDir = SOD.Common.Lib.SaveGame.GetSavestoreDirectoryPath(
    //         Plugin.Instance.GetActualType().Assembly,
    //         $"{Game.Instance.buildID}/"
    //     );
    //     if (instance != null) {
    //         Directory.CreateDirectory(Path.GetDirectoryName(pathToDir));
    //         using (var stream = File.Create(Path.Join(pathToDir, $"{Helpers.GetPresetKey(instance)}.json"))) {
    //             WriteJsonToFileStream(stream, instance.GetActualType(), [instance], default);
    //         }
    //         return;
    //     }
    //     var presetInstances = Helpers.GetPresetInstances(type: null, presetNamePredicate).TryCastAll<SoCustomComparison>();
    //     WriteToJson(presetInstances, presetNamePredicate);
    // }

    public static void WriteJsonToFileStream(in FileStream stream, IEnumerable<object> presetInstances, Func<string, bool> propNamePredicate = null) {
        WriteJsonToFileStream(stream, presetInstances, propNamePredicate, new JsonWriterOptions() { Indented = true });
    }

    public static void WriteJsonToFileStream(in FileStream stream, IEnumerable<object> instances, Func<string, bool> propNamePredicate = null, JsonWriterOptions writerOptions = default) {
        using (var writer = new Utf8JsonWriter(stream, writerOptions)) {
            writer.WriteStartObject();

            writer.WriteString("__gameBuildId", Game.Instance.buildID);
            writer.WriteString("__presetEditPluginVersion", MyPluginInfo.PLUGIN_VERSION);

            foreach (var instance in instances) {
                var actualType = instance.GetActualType();
                var actualInstance = instance.TryCast(actualType);

                writer.WriteStartObject(Helpers.GetPresetKey(actualInstance));

                // Properties with public setters
                var propInfos = actualType.GetProperties().Where(prop => prop.GetSetMethod() != null);
                foreach (var propInfo in propInfos) {
                    var name = propInfo.Name;
                    if (name == "name") {
                        // only write the presetName
                        continue;
                    }
                    if (name != "presetName" && propNamePredicate != null && !propNamePredicate(name)) {
                        continue;
                    }
                    var propType = propInfo.PropertyType;
                    if (propType.IsPointer || name.Contains("Ptr") || name.Contains("ptr")) {
                        continue;
                    }
                    writer.WritePropertyName(name);
                    try {
                        var value = JsonSerializer.Serialize(propInfo.GetValue(actualInstance), propType, SerializerOptions);
                        writer.WriteRawValue(value);
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
                Plugin.Log.LogInfo($"Finished writing preset to file: {Helpers.GetPresetKey(instance)}");
            }

            writer.WriteEndObject();
        }
    }
}