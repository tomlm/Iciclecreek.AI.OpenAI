using Humanizer;
using Iciclecreek.AI.OpenAI.FormFill.Attributes;
using Microsoft.Extensions.AI;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Matcher;
using Microsoft.Recognizers.Text.Number;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Iciclecreek.AI.Forms
{

    [Description("A form which needs to be completed for a task")]
    public class FormTask<FormT>
        where FormT : class
    {
        private List<AITool> _tools = new List<AITool>();

        public FormTask()
        {
            this.Form = Activator.CreateInstance<FormT>();

            // add the validation funcion
            _tools.Add(AIFunctionFactory.Create(GetType().GetMethod(nameof(AssignValue))!, this));
            _tools.Add(AIFunctionFactory.Create(GetType().GetMethod(nameof(GetValue))!, this));
            _tools.Add(AIFunctionFactory.Create(GetType().GetMethod(nameof(ClearValue))!, this));
            _tools.Add(AIFunctionFactory.Create(GetType().GetMethod(nameof(AddValueToCollection))!, this));
            _tools.Add(AIFunctionFactory.Create(GetType().GetMethod(nameof(RemoveValueFromCollection))!, this));
        }

        public FormTask(FormT form)
        {
            this.Form = form;
        }

        [Description("The actual form data")]
        public FormT Form { get; set; }

        [Description("The purpose of this form.")]
        public string Purpose { get; set; } = string.Empty;

        [Description("The status of the form collection.")]
        public FormStatus Status { get; set; }

        [Description("Assign a value to a property of the Form")]
        public ToolResult AssignValue(
            [Required]
            [Description("The property member name to assign the value to.")]
            string property,
            [Required]
            [Description("The JSON serialized value to assign to the property.")]
            string value)
        {
            // get the property
            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return ToolResult.Failed($"Unknown property {property}");
            }

            // If it's an array then we process the value as "add" operation
            if (propertyInfo!.PropertyType.IsList())
            {
                var collection = (IList)propertyInfo.GetValue(Form);
                if (value is string || value.GetType().IsValueType)
                {
                    if (!collection.Contains(value))
                        collection.Add(value);
                }
                else
                {
                    foreach (var val in (IEnumerable)value)
                    {
                        if (!collection.Contains(val))
                            collection.Add(val);
                    }
                }

                // add ADDED(value) response
                return ToolResult.Success($"Added {value} to {propertyInfo.Name}");
            }

            // see if value matches property schema
            var (newValue, errorResponse) = ResolvePropertyValue(property, value?.ToString());
            if (errorResponse != null)
            {
                return ToolResult.Failed(GetErrorMessage(value, propertyInfo, errorResponse), errorResponse);
            }

            // get old value if any
            var oldValue = propertyInfo.GetValue(Form);

            // if it changed
            if (string.Compare(value?.ToString(), oldValue?.ToString(), ignoreCase: true) != 0)
            {
                // set the new value
                propertyInfo.SetValue(Form, newValue);

                // if old value was not null
                if (oldValue != null)
                {
                    // add CHANGED(value) response
                    return ToolResult.Success($"Changed {propertyInfo.Name} from {oldValue} to {newValue}");
                }
                else
                {
                    // add ASSIGNED(value) response
                    return ToolResult.Success($"Set {propertyInfo.Name} to {newValue}");
                }
            }
            return ToolResult.Failed("Not Implemented");
        }


        [Description("Add a value to a collection on the form")]
        public ToolResult AddValueToCollection(
            [Required]
            [Description("The property name of the collection to remove the value from.")]
            string property,
            [Required]
            [Description("The JSON serialized value to add to collection the property.")]
            string value)
        {
            // get the property
            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return ToolResult.Failed($"Unknown property {property}");
            }

            if (!propertyInfo!.PropertyType.IsList())
                return ToolResult.Failed($"Property {property} is not a collection type.");

            var (newValue, errorResponse) = ResolvePropertyValue(property, value?.ToString());
            if (errorResponse != null)
            {
                return ToolResult.Failed(GetErrorMessage(value, propertyInfo, errorResponse), errorResponse);
            }

            var collection = (IList)propertyInfo.GetValue(Form);
            collection.Add(newValue);

            return ToolResult.Success($"Added {value} to {propertyInfo.Name}");
        }

        [Description("Remove a value to a collection on the form")]
        public ToolResult RemoveValueFromCollection(
            [Required]
            [Description("The property name of the collection to remove the value from.")]
            string property,
            [Required]
            [Description("The JSON serialized value to remove from the collection.")]
            string value)
        {
            // get the property
            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return ToolResult.Failed($"Unknown property {property}");
            }

            if (!propertyInfo!.PropertyType.IsList())
                return ToolResult.Failed($"Property {property} is not a collection type.");

            var collection = (IList)propertyInfo.GetValue(Form);
            if (value is string || value.GetType().IsValueType)
            {
                collection.Remove(value);
            }

            return ToolResult.Success($"Removed {value} from {propertyInfo.Name}");
        }

        [Description("Clear out the value for a property on the form")]
        public ToolResult ClearValue(
            [Required]
            [Description("The property member name to clear the value for.")]
            string property)
        {
            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return ToolResult.Failed($"Unknown property {property}");
            }

            // If it's an array then we process the value as "remove" operation
            if (propertyInfo.PropertyType.IsList())
            {
                var collection = (IList)propertyInfo.GetValue(Form);
                collection.Clear();
                return ToolResult.Success($"Cleared values from {propertyInfo.Name}");
            }

            propertyInfo.SetValue(Form, null);
            return ToolResult.Success($"Cleared {propertyInfo.Name}");
        }

        [Description("Get the value of a property on the form")]
        public ToolResult GetValue(
            [Required]
            [Description("The property member name to get the value for.")]
            string property)
        {
            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                throw new Exception("nown property {property}");
            }

            var value = propertyInfo.GetValue(Form);
            var result = ToolResult.Success($"Fetched {propertyInfo.Name}");
            result.Value = value;
            return result;
        }

        /// <summary>
        /// Given a property and a value resolve the value to a valid value for the property or return errors
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual (object? value, List<ValidationResult>? errors) ResolvePropertyValue(string? property, string? value)
        {
            // Convert value to the appropriate JSON type
            // This is fixed because we ask GPT to translate.
            // var locale = dc.State.GetStringValue($"conversation.locale", "en-us");
            var locale = "en-us";

            if (!typeof(FormT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                throw new Exception($"Unknown property {property}");
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(Form)
            {
                MemberName = propertyInfo.Name,
                DisplayName = propertyInfo.Name,
            };

            Type valueType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            if (propertyInfo.PropertyType.IsList())
            {
                valueType = propertyInfo.PropertyType.GetGenericArguments().First()!;
            }

            // Figure out base time for references
            var refDate = DateTime.Now;
            //if (pschema.ExtensionData.TryGetValue(REF_DATE, out var refDateProperty))
            //{
            //    // TODO: In theory we should probably order the ref property before the current one.  
            //    // In English though it is likely the order will be right anyway so punt for now.
            //    var date = dc.State.GetStringValue($"form.{refDateProperty.Value<string>()}");
            //    if (date != null && DateTime.TryParse(date, out var dateValue))
            //    {
            //        refDate = dateValue;
            //    }
            //}
            object? realValue = null;
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.String:
                    realValue = value.Trim();
                    if (!propertyInfo.PropertyType.IsList())
                    {
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);
                    }
                    return (realValue, null);
                case TypeCode.Boolean:
                    realValue = RecognizeBool(propertyInfo, value, "en-us");
                    if (!Validator.TryValidateProperty(realValue, context, validationResults))
                        return (null, validationResults);
                    return (realValue, null);

                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        if (valueType.IsEnum)
                        {
                            if (!Enum.TryParse(valueType, value, true, out var enumValue))
                            {
                                return (null, new List<ValidationResult>() { new ValidationResult($"I didn't understand {value} as a valid {valueType.Name} value for {propertyInfo.Name}") });
                            }
                            realValue = enumValue;

                            if (!Validator.TryValidateProperty(realValue, context, validationResults))
                                return (null, validationResults);
                            return (realValue, null);
                        }
                        else
                        {
                            realValue = RecognizeNumber(propertyInfo, value, "en-us");
                            if (realValue == null)
                            {
                                return (null, new List<ValidationResult>() { new ValidationResult($"I didn't understand {value}") });
                            }

                            if (propertyInfo.PropertyType.IsList())
                            {
                                // If it's a list we need to convert the value to the correct type
                                var itemValidation = propertyInfo.GetCustomAttribute<ItemValidationAttribute>();
                                if (itemValidation != null)
                                {
                                    var validationResult = itemValidation.ValidateItem(realValue);
                                    if (validationResult != null)
                                        return (null, new List<ValidationResult>() { validationResult });
                                }
                                return (realValue, null);
                            }

                            if (!Validator.TryValidateProperty(realValue, context, validationResults))
                                return (null, validationResults);

                            return (realValue, null);
                        }
                    }
                case TypeCode.Single:
                case TypeCode.Double:
                    {
                        realValue = RecognizeNumber(propertyInfo, value, "en-us");
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);
                        return (realValue, null);
                    }

                case TypeCode.DateTime:
                    {
                        if (DateTime.TryParse(value, out refDate))
                        {
                            realValue = refDate;
                        }
                        else
                        {
                            var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                            realValue = timex.Merge((DateTimeOffset?)propertyInfo.GetValue(Form));
                            //foreach (var date in dates)
                            //{
                            //    foreach (var time in times)
                            //    {
                            //        realValue = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, TimeSpan.MinValue);
                            //    }
                            //}
                        }

                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                        {
                            return (null, validationResults);
                        }
                    }
                    return (realValue, null);



                case TypeCode.Object when valueType.Name == nameof(DateTimeOffset):
                    {
                        if (DateTimeOffset.TryParse(value, out var refDateOffset))
                        {
                            realValue = refDateOffset;
                        }
                        else
                        {
                            var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                            realValue = timex.Merge((DateTime?)propertyInfo.GetValue(Form));
                        }

                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);

                        return (realValue, null);
                    }

                case TypeCode.Object when valueType.Name == nameof(TimeOnly):
                    {
                        if (TimeOnly.TryParse(value, out var timeOnly))
                        {
                            realValue = timeOnly;
                        }
                        else
                        {
                            var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                            realValue = timex.Merge((TimeOnly?)propertyInfo.GetValue(Form));
                        }

                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);
                        return (realValue, null);
                    }

                case TypeCode.Object when valueType.Name == nameof(DateOnly):
                    {
                        if (DateOnly.TryParse(value, out var timeOnly))
                        {
                            realValue = timeOnly;
                        }
                        else
                        {
                            var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                            realValue = timex.Merge((DateOnly?)propertyInfo.GetValue(Form));
                        }

                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);
                        return (realValue, null);
                    }

                case TypeCode.Object when valueType.Name == nameof(TimeSpan):
                    {
                        if (TimeSpan.TryParse(value, out var timeSpan))
                        {
                            realValue = timeSpan;
                        }
                        else if (XmlConvert.ToTimeSpan(value) is TimeSpan xmlTimeSpan)
                        {
                            realValue = xmlTimeSpan;
                        }
                        else
                        { 
                            var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                            realValue = timex.Merge((TimeSpan?)propertyInfo.GetValue(Form));
                        }
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, validationResults);
                        return (realValue, null);
                    }

                default:
                    break;
            }
            throw new Exception("uhoho");
        }

        private static string GetErrorMessage(string value, PropertyInfo propertyInfo, List<ValidationResult> validationResults)
        {
            return $"{value} is not valid for {propertyInfo.Name} because: {string.Join(',', validationResults.Select(e => e.ErrorMessage))}";
        }

        static protected (TimexProperty, IList<DateOnly>, IList<TimeOnly>) RecognizeTimex(PropertyInfo propertyInfo, string value, string locale, DateTime refDate)
        {
            var results = DateTimeRecognizer.RecognizeDateTime(value, locale, refTime: refDate);
            if (results == null)
            {
                results = NumberRecognizer.RecognizeOrdinal(value, locale);
                if (results != null)
                {
                    results = DateTimeRecognizer.RecognizeDateTime($"on the {value}", "en-us", refTime: refDate);
                }
            }

            TimexProperty? timexProperty = null;
            List<DateOnly> dates = new List<DateOnly>();
            List<TimeOnly> times = new List<TimeOnly>();
            if (results != null)
            {
                foreach (var values in results[0].Resolution["values"] as List<Dictionary<string, string>>)
                {
                    if (timexProperty == null && values.ContainsKey("timex"))
                    {
                        timexProperty = new TimexProperty(values["timex"]);
                    }

                    if (values.ContainsKey("type"))
                    {
                        var valueType = values["type"];
                        var val = values["value"];
                        if (valueType == "datetime")
                        {
                            var dt = DateTime.Parse(val);
                            dates.Add(DateOnly.FromDateTime(dt));
                            times.Add(TimeOnly.FromDateTime(dt));
                        }
                        else if (valueType == "date")
                        {
                            dates.Add(DateOnly.Parse(val));
                        }
                        else if (valueType == "time")
                        {
                            times.Add(TimeOnly.Parse(val));
                        }
                    }
                }
            }
            return (timexProperty, dates, times.Distinct().ToList());
        }

        static protected object? RecognizeNumber(PropertyInfo propertyInfo, string value, string locale)
        {
            object? result = null;
            value = value.Trim();

            var models = NumberRecognizer.RecognizeNumber(value, locale);
            if (models.Count > 1)
            {
                return null;
            }
            var model = models.FirstOrDefault();
            if (model != null)
            {
                var subtype = model.Resolution["subtype"] as string;
                var svalue = model.Resolution["value"] as string;
                if (subtype != null && svalue != null)
                {
                    if (subtype == Microsoft.Recognizers.Text.Number.Constants.INTEGER)
                    {
                        if (int.TryParse(svalue, out var ivalue))
                        {
                            result = ivalue;
                        }
                    }
                    else
                    {
                        // DECIMAL, FRACTION or POWER
                        if (double.TryParse(svalue, out var dvalue))
                        {
                            result = dvalue;
                        }
                    }
                }
            }

            if (result != null)
            {
                var targetType = propertyInfo.PropertyType;
                if (targetType.GenericTypeArguments.Any())
                    targetType = targetType.GenericTypeArguments.First();
                return Convert.ChangeType(result, targetType);
            }
            return result;
        }

        static protected bool? RecognizeBool(PropertyInfo propertyInfo, string value, string locale)
        {
            object? result = null;
            value = value.Trim();

            var models = ChoiceRecognizer.RecognizeBoolean(value, locale);
            if (models.Count > 1)
            {
                return null;
            }
            var model = models.FirstOrDefault();
            if (model != null)
            {
                return (bool?)model.Resolution["value"];
            }
            return null;
        }

    }


    //public partial class FormFillEngine<FormT>
    //    where FormT : class
    //{
    //    private readonly IChatClient _chatClient;

    //    public FormFillEngine(IChatClient chatClient)
    //    {
    //        this._chatClient = new ChatClientBuilder(chatClient)
    //            .UseFunctionInvocation()
    //            .Build();
    //    }

    //    public void Tools()
    //    {
    //    }

    //    public IList<ValidationResult> GetFormValidationErrors()
    //    {
    //        var results = new List<ValidationResult>();
    //        var context = new ValidationContext(FormTask);

    //        if (!Validator.TryValidateObject(FormTask, context, results, validateAllProperties: true))
    //        {
    //            return results;
    //        }

    //        return results;
    //    }

    //    public bool GetConfirmation(string message)
    //    {
    //        // This method can be used to get confirmation from the user
    //        // For now, we will just return true to indicate confirmation
    //        // In a real application, you might want to implement a way to get user input
    //        return true;
    //    }

    //    public FormT SubmitForm()
    //    {
    //        // This method can be used to submit the form
    //        // For now, we will just return the model as is
    //        // In a real application, you might want to save the model to a database or perform some other action
    //        return model;
    //    }


    //}
}