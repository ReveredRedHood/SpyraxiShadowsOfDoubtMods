using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PresetEdit;
public class JsonIntPtrIgnorer : JsonConverter<IntPtr> {
    public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options) {
        writer.WriteNullValue();
    }
}