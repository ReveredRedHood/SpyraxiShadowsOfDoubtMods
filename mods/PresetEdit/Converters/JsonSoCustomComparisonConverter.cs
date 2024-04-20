using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniverseLib;
using UniverseLib.Utility;

namespace PresetEdit;
public class JsonSoCustomComparisonConverter<T> : JsonConverter<T> where T : SoCustomComparison {
    public const string PRESET_PROPERTY_SEPARATOR = "__PRESET__";

    public override bool CanConvert(Type typeToConvert) {
        var result = typeToConvert.IsAssignableTo(typeof(T));
        return result;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var presetName = reader
            .GetString()
            .Split(PRESET_PROPERTY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
            .Last();
        var preset = Helpers.GetPresetInstances(typeToConvert, presetName, false).FirstOrDefault();
        if (preset == default || preset.IsNullOrDestroyed()) {
            throw new InvalidOperationException("Preset not found; ensure you are in an active savegame, and that any custom presets that are being loaded have their corresponding plugin(s) installed.");
        }
        return preset.TryCast<T>();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStringValue(GetJsonString(value));
    }

    public static string GetJsonString(object value) {
        if (value == null) {
            return "null";
        }
        return Helpers.GetPresetKey(value);
    }

}
