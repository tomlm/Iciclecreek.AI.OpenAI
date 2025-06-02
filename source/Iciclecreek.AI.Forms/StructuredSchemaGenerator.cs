using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public class StructuredSchemaGenerator
    {
        public static JsonObject FromType<Type>()
        {
            return FromType(typeof(Type));
        }

        public static JsonObject FromType(Type type)
        {
            var schema = GetSchema(type);
            return schema;
        }

        private static JsonObject GetSchema(Type type)
        {
            var schema = new JsonObject();

            bool isNullable = type.Name == "Nullable`1";
            if (isNullable)
                type = type.GetGenericArguments().First();

            if (type == typeof(string))
            {
                schema["type"] = "string";
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                schema["type"] = "integer";
            }
            else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                schema["type"] = "number";
            }
            else if (type == typeof(bool))
            {
                schema["type"] = "boolean";
            }
            else if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                schema["type"] = "array";
                var itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                schema["items"] = GetSchema(itemType);
            }
            else if (type.IsEnum)
            {
                schema["type"] = "string";
                schema["enum"] = new JsonArray(Enum.GetNames(type).Select(n => JsonValue.Create(n)).ToArray());
            }
            else if (type == typeof(DateOnly))
            {
                schema["type"] = "string";
                schema["format"] = "date";
            }
            else if (type == typeof(TimeOnly))
            {
                schema["type"] = "string";
                schema["format"] = "time";
            }
            else if (type == typeof(DateTime))
            {
                schema["type"] = "string";
                schema["format"] = "date-time";
            }
            else if (type == typeof(TimeSpan))
            {
                schema["type"] = "string";
                schema["format"] = "duration";
                schema["description"] = "timespan as 100 nanosecond ticks or iso-8601 timex";
            }
            else
            {
                schema["type"] = "object";
                schema["additionalProperties"] = false;
                var properties = new JsonObject();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var required = new JsonArray(props.Select(s => JsonValue.Create(s.Name)).ToArray());
                schema["required"] = required;
                foreach (var prop in props)
                {
                    properties[prop.Name] = GetSchema(prop.PropertyType);
                }
                schema["properties"] = properties;
            }

            //if (isNullable)
            //  schema["type"] = new JsonArray { schema["type"], "null" };
            return schema;
        }
    }
}