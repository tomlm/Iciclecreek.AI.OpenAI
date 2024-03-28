using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
using System.ComponentModel;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public static class FormFillExtensions
    {
        public static IServiceCollection AddFormFiller<ModelT>(this IServiceCollection services)
            where ModelT : class
        {
            services.AddTransient<FormFillRecognizer<ModelT>>();
            services.AddTransient<FormFillEngine<ModelT>>();
            return services;
        }


        public static SemanticActionResult NoAction(this SemanticAction action, string message = null)
        {
            return new SemanticActionResult(action)
            {
                Result = SemanticActionResultType.NoAction,
                Message = message
            };
        }

        public static SemanticActionResult Success(this SemanticAction action, string message = null)
        {
            return new SemanticActionResult(action)
            {
                Result = SemanticActionResultType.Success,
                Message = message
            };
        }

        public static SemanticActionResult Failed(this SemanticAction action, string message)
        {
            return new SemanticActionResult(action)
            {
                Result = SemanticActionResultType.Failed,
                Message = message
            };
        }

        public static List<string> Tokenize(this string text)
        {
            List<string> tokens = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (char ch in text)
            {
                if (Char.IsLetter(ch) || Char.IsDigit(ch))
                    sb.Append(ch);
                else
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString().ToLower());
                        sb.Clear();
                    }
                }
            }
            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString().ToLower());
            }

            return tokens;
        }


        public static bool HasProperty(this Type type, string property)
        {
            return type.TryGetPropertyInfo(property, out var _);
        }

        public static PropertyInfo GetPropertyInfo(this Type type, string property)
        {
            return type.GetProperties().Single(p => p.Name.ToLower() == property.ToLower());
        }

        public static bool TryGetPropertyInfo(this Type type, string property, out PropertyInfo? propertyInfo)
        {
            propertyInfo = type.GetProperties().SingleOrDefault(p => p.Name.ToLower() == property.ToLower());
            return propertyInfo != null;
        }

        public static string GetPropertyLabel(this PropertyInfo propertyInfo)
        {
            var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
            return displayNameAttribute?.DisplayName ??
                displayAttribute?.GetName() ??
                displayAttribute?.GetShortName() ??
                propertyInfo.Name.Humanize();
        }

        public static string GetFormatedValueText(this PropertyInfo propertyInfo, object? value)
        {
            if (value == null)
                return string.Empty;

            var displayAttribute = propertyInfo.GetCustomAttribute<DisplayFormatAttribute>();
            if (displayAttribute != null && !String.IsNullOrEmpty(displayAttribute.DataFormatString))
            {
                return String.Format(displayAttribute.DataFormatString, value);
            }

            return value.ToString();
        }

        public static bool IsList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>);
        }

        public static DateTimeOffset Merge(this TimexProperty timex, DateTimeOffset? dt)
        {
            var datetimeOffset = (dt != null) ? dt.Value : DateTime.MinValue;
            return new DateTimeOffset(timex.Year ?? datetimeOffset.Year,
                timex.Month ?? datetimeOffset.Month,
                timex.DayOfMonth ?? datetimeOffset.Day,
                timex.Hour ?? datetimeOffset.Hour,
                timex.Minute ?? datetimeOffset.Minute,
                timex.Second ?? datetimeOffset.Second,
                datetimeOffset.Offset);
        }

        public static DateTime Merge(this TimexProperty timex, DateTime? dt)
        {
            var datetime = (dt != null) ? dt.Value : DateTime.MinValue;
            return new DateTime(timex.Year ?? datetime.Year,
                timex.Month ?? datetime.Month,
                timex.DayOfMonth ?? datetime.Day,
                timex.Hour ?? datetime.Hour,
                timex.Minute ?? datetime.Minute,
                timex.Second ?? datetime.Second);
        }

        public static DateOnly Merge(this TimexProperty timex, DateOnly? dt)
        {
            var date = (dt != null) ? dt.Value : DateOnly.MinValue;
            return new DateOnly(timex.Year ?? date.Year,
                timex.Month ?? date.Month,
                timex.DayOfMonth ?? date.Day);
        }

        public static TimeOnly Merge(this TimexProperty timex, TimeOnly? tm)
        {
            var time = (tm != null) ? tm.Value : TimeOnly.MinValue;
            return new TimeOnly(timex.Hour ?? time.Hour,
                timex.Minute ?? time.Minute,
                timex.Second ?? time.Second);
        }


        public static TimeSpan Merge(this TimexProperty timex, TimeSpan? tm)
        {
            var time = (tm != null) ? tm.Value : TimeSpan.MinValue;
            return new TimeSpan(timex.Hour ?? time.Hours,
                timex.Minute ?? time.Minutes,
                timex.Second ?? time.Seconds);
        }

    }
}
