// Create a class that derives from JsonConverterFactory.
// Override the CanConvert method to return true when the type to convert is one that the converter can handle. For example, if the converter is for List<T>, it might only handle List<int>, List<string>, and List<DateTime>.
// Override the CreateConverter method to return an instance of a converter class that will handle the type-to-convert that is provided at run time.
// Create the converter class that the CreateConverter method instantiates.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SOD.Common.Extensions;

namespace PresetEdit;
public sealed class JsonIl2CppIListConverter : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if (!JsonIl2CppListConverter.CanConvertSharedChecks(typeToConvert)
            || typeToConvert.GetGenericTypeDefinition() != typeof(Il2CppSystem.Collections.Generic.IList<>)) {
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

    private class InnerConverter<T> : JsonConverter<Il2CppSystem.Collections.Generic.IList<T>> {
        private readonly JsonConverter<Il2CppSystem.Collections.Generic.List<T>> _il2cppListConverter;
        private readonly JsonConverter<List<T>> _listConverter;
        public InnerConverter(JsonSerializerOptions options) {
            _il2cppListConverter = (JsonConverter<Il2CppSystem.Collections.Generic.List<T>>)options.GetConverter(typeof(Il2CppSystem.Collections.Generic.List<T>));
            _listConverter = (JsonConverter<List<T>>)options.GetConverter(typeof(List<T>));
        }

        public override Il2CppSystem.Collections.Generic.IList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return _il2cppListConverter.Read(ref reader, typeToConvert, options).TryCast<Il2CppSystem.Collections.Generic.IList<T>>();
        }

        public override void Write(Utf8JsonWriter writer, Il2CppSystem.Collections.Generic.IList<T> value, JsonSerializerOptions options) {
            _listConverter.Write(writer, value.ToList(), options);
        }
    }

}