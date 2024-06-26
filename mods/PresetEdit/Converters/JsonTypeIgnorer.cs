using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PresetEdit;
public sealed class JsonTypeIgnorer<T> : JsonConverter<T> {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStringValue(PresetSerializer.NOT_SUPPORTED_FLAG);
    }
}