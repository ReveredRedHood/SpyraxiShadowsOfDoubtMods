using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SOD.Common.Extensions;
using UniverseLib;

namespace PresetEdit;
public class JsonSoCustomComparisonConverter : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if (!typeToConvert.GetActualType().IsAssignableTo(typeof(SoCustomComparison))) {
            return false;
        }
        return true;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        return new InnerConverter();
    }

    private class InnerConverter : JsonConverter<UnityEngine.Object> {
        private const string PRESET_PROPERTY_PREFIX = "PRESET__";

        public override UnityEngine.Object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var presetName = reader.GetString()
                .Split(PRESET_PROPERTY_PREFIX, StringSplitOptions.RemoveEmptyEntries)
                .Last();
            foreach (var presetType in Serializer.AllPresetTypes) {
                UnityEngine.Object target = RuntimeHelper.FindObjectsOfTypeAll(presetType)
                    .FirstOrDefault(obj => obj.name == presetName);
                if (target != default) {
                    // TODO: override values here
                    return target;
                }
            }
            throw new InvalidOperationException("Preset not found; ensure you have loaded or started a savegame first.");
        }

        public override void Write(Utf8JsonWriter writer, UnityEngine.Object value, JsonSerializerOptions options) {
            writer.WriteStringValue($"{PRESET_PROPERTY_PREFIX}{value.name}");
        }
    }
}
