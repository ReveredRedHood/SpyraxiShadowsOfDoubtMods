// Create a class that derives from JsonConverterFactory.
// Override the CanConvert method to return true when the type to convert is one that the converter can handle. For example, if the converter is for List<T>, it might only handle List<int>, List<string>, and List<DateTime>.
// Override the CreateConverter method to return an instance of a converter class that will handle the type-to-convert that is provided at run time.
// Create the converter class that the CreateConverter method instantiates.

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniverseLib;

namespace PresetEdit;
public sealed class JsonIl2CppEnumerableConverter : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if (!typeToConvert.IsGenericType
            || !typeToConvert.GetGenericTypeDefinition().FullName.Contains("Il2CppSystem.Collections.Generic")) {
            return false;
        }
        return true;
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) {
        // Construct an instance of the generic type
        var argType = type.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(InnerConverter<,>).MakeGenericType(
                new Type[] { type, argType }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;
    }

    private class InnerConverter<TEnumerable, TParam> : JsonConverter<TEnumerable> {
        private const int LIST_COUNT_LIMIT = 100;
        private readonly JsonConverter<TParam> _elementConverter;
        public InnerConverter() {
            _elementConverter = (JsonConverter<TParam>)PresetSerializer.SerializerOptions.GetConverter(typeof(TParam));
        }

        public override TEnumerable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // We don't read from these converters
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TEnumerable value, JsonSerializerOptions options) {
            var asList = value.FormListIfEnumerable();
            if (asList.Count > LIST_COUNT_LIMIT) {
                Plugin.Log.LogWarning($"Skipping writing enumerable value; the length {asList.Count} is too long (must be <= {LIST_COUNT_LIMIT}).");
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }
            if (asList == null || asList.Count == 0) {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }
            writer.WriteStartArray();
            foreach (var element in asList) {
                _elementConverter.Write(writer, element.TryCast<TParam>(), options);
            }
            writer.WriteEndArray();
        }
    }
}