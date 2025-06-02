using System.Text.Json;
using System.Xml;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public class TimeSpanConverter : System.Text.Json.Serialization.JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString();
            if (long.TryParse(text, out var ticks))
                return new TimeSpan(ticks);
            return System.Xml.XmlConvert.ToTimeSpan(text);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(XmlConvert.ToString(value));
        }
    }
}