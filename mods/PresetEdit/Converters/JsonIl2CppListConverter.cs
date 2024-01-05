// Create a class that derives from JsonConverterFactory.
// Override the CanConvert method to return true when the type to convert is one that the converter can handle. For example, if the converter is for List<T>, it might only handle List<int>, List<string>, and List<DateTime>.
// Override the CreateConverter method to return an instance of a converter class that will handle the type-to-convert that is provided at run time.
// Create the converter class that the CreateConverter method instantiates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SOD.Common.Extensions;
using UnityEngine.Playables;
using UniverseLib;
using UniverseLib.Utility;

namespace PresetEdit;
public sealed class JsonIl2CppListConverter : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if (!typeToConvert.IsGenericType
            || !Serializer.AllPresetTypes.Contains(typeToConvert.GetGenericArguments()[0])
            || typeToConvert.GetGenericTypeDefinition() != typeof(Il2CppSystem.Collections.Generic.List<>)) {
            return false;
        }
        return true;
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) {
        // Construct an instance of the generic type
        Type argType = type.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(InnerConverter<>).MakeGenericType(
                new Type[] { argType }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { options },
            culture: null)!;
    }

    private class InnerConverter<T> : JsonConverter<Il2CppSystem.Collections.Generic.List<T>> {
        private readonly JsonConverter<SoCustomComparison> _soCustomComparisonConverter;
        public InnerConverter(JsonSerializerOptions options) {
            _soCustomComparisonConverter = (JsonConverter<SoCustomComparison>)options.Converters.First(converter => converter.GetType() == typeof(JsonSoCustomComparisonConverter<SoCustomComparison>));
        }

        public override Il2CppSystem.Collections.Generic.List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // We don't read from this converter
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Il2CppSystem.Collections.Generic.List<T> value, JsonSerializerOptions options) {
            var asList = value.ToList();
            if (asList == null || asList.Count == 0) {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }
            Type argType = value.GetType().GetGenericArguments()[0];
            var asSoCustomList = asList.Select(preset => preset.TryCast<SoCustomComparison>()).Where(result => !result.IsNullOrDestroyed());
            writer.WriteStartArray();
            foreach (var preset in asSoCustomList) {
                _soCustomComparisonConverter.Write(writer, preset, options);
            }
            writer.WriteEndArray();
            // try {
            //     var asStrList = asSoCustomList.Select(preset => preset.name)
            //         .Select(entry => $"{argType.FullName}{JsonSoCustomComparisonConverter.PRESET_PROPERTY_SEPARATOR}{entry}")
            //         .ToList();
            //     _listConverter.Write(writer, asStrList, options);
            // }
            // catch {
            //     Plugin.Log.LogWarning($"{argType.FullName}: Name was null, trying alternative method to get the preset name.");
            //     var asStrList = asSoCustomList.Select(preset => preset.presetName)
            //         .Select(entry => $"{argType.FullName}{JsonSoCustomComparisonConverter.PRESET_PROPERTY_SEPARATOR}{entry}")
            //         .ToList();
            //     _listConverter.Write(writer, asStrList, options);
            // }
        }
    }
}