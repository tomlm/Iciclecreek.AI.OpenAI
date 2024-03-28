using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.Choice;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System;
using Azure.AI.OpenAI;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Iciclecreek.AI.OpenAI.FormFill;
using System.Collections;
using System.Reflection.Metadata.Ecma335;
using Humanizer;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public partial class FormFillEngine<ModelT>
        where ModelT : class
    {
        private static Random _rnd = new Random();

        public FormFillEngine(FormFillRecognizer<ModelT> recognizer)
        {
            this.Recognizer = recognizer;
        }

        public FormFillRecognizer<ModelT> Recognizer { get; set; }

        public async Task<List<SemanticActionResult>> InterpretTextAsync(ModelT model, String text, CancellationToken cancellationToken)
        {
            var actions = await Recognizer.RecognizeAsync(text, cancellationToken: cancellationToken);

            List<SemanticActionResult> actionResults = new List<SemanticActionResult>();
            foreach (var action in actions)
            {
                switch (action.Name)
                {
                    case FormFillActions.ASSIGN:
                        actionResults.Add(await OnAssignPropertyAsync(model, action, cancellationToken));
                        break;
                    case FormFillActions.REMOVE:
                        actionResults.Add(await OnRemoveValueAsync(model, action, cancellationToken));
                        break;
                    case FormFillActions.CLEAR:
                        actionResults.Add(await OnClearPropertyAsync(model, action, cancellationToken));
                        break;
                    case FormFillActions.RESET:
                        actionResults.Add(await OnResetFormAsync(model, action, cancellationToken));
                        break;
                    default:
                        break;
                }
            }

            return actionResults;
        }

        /// <summary>
        /// ASSIGN(property, value) 
        /// </summary>
        /// <remarks>This processes a function to try to assign a value to a property in memory.</remarks>
        /// <param name="dc"></param>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<SemanticActionResult> OnAssignPropertyAsync(ModelT model, SemanticAction function, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var property = function.Args.Cast<String>().First();
            var value = function.Args.Skip(1).FirstOrDefault();

            // get the property
            if (!typeof(ModelT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return function.Failed($"Unknown property {property}");
            }

            // If it's an array then we process the value as "add" operation
            if (propertyInfo.PropertyType.IsList())
            {
                var collection = (IList)propertyInfo.GetValue(model);
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
                return function.Success($"Added {value} to {propertyInfo.GetPropertyLabel()}");
            }

            // see if value matches property schema
            var (newValue, errorResponse) = await ResolvePropertyValue(model, property, value?.ToString());
            if (errorResponse != null)
            {
                return function.Failed(errorResponse);
            }

            // get old value if any
            var oldValue = propertyInfo.GetValue(model);

            // if it changed
            if (string.Compare(value?.ToString(), oldValue?.ToString(), ignoreCase: true) != 0)
            {
                // set the new value
                propertyInfo.SetValue(model, newValue);

                // if old value was not null
                if (oldValue != null)
                {
                    // add CHANGED(value) response
                    return function.Success($"Changed {propertyInfo.GetPropertyLabel()} from {oldValue} to {newValue}");
                }
                else
                {
                    // add ASSIGNED(value) response
                    return function.Success($"Set {propertyInfo.GetPropertyLabel()} to {newValue}");
                }
            }

            return function.NoAction();
        }




        /// <summary>
        /// REMOVE(property) function
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<SemanticActionResult> OnRemoveValueAsync(ModelT model, SemanticAction function, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var property = function.Args.Cast<String>().First();
            var value = function.Args.Skip(1).FirstOrDefault();

            if (!typeof(ModelT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return function.Failed($"Unknown property {property}");
            }

            // If it's an array then we process the value as "remove" operation
            if (propertyInfo.PropertyType.IsList())
            {
                var collection = (IList)propertyInfo.GetValue(model);
                if (value is string || value.GetType().IsValueType)
                {
                    collection.Remove(value);
                }
                else
                {
                    foreach (var val in (IEnumerable)value)
                    {
                        collection.Remove(val);
                    }
                }

                // add REMOVE(value) response
                return function.Success($"Removed {value} from {propertyInfo.GetPropertyLabel()}");
            }

            propertyInfo.SetValue(model, default(ModelT));
            return function.Success($"Cleared {propertyInfo.GetPropertyLabel()}");
        }

        /// <summary>
        /// CLEAR(property) function
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<SemanticActionResult> OnClearPropertyAsync(ModelT model, SemanticAction function, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var property = function.Args.Cast<String>().First();

            if (!typeof(ModelT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return function.Failed($"Unknown property {property}");
            }

            // If it's an array then we process the value as "remove" operation
            if (propertyInfo.PropertyType.IsList())
            {
                var collection = (IList)propertyInfo.GetValue(model);
                collection.Clear();
                return function.Success($"Cleared values from {propertyInfo.GetPropertyLabel()}");
            }

            propertyInfo.SetValue(model, default(ModelT));
            return function.Success($"Cleared {propertyInfo.GetPropertyLabel()}");
        }

        /// <summary>
        /// RESET() - user has requested to start over with the task.
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<SemanticActionResult> OnResetFormAsync(ModelT model, SemanticAction function, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var propertyInfo in model.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType.IsList())
                {
                    var collection = (IList)propertyInfo.GetValue(model);
                    collection.Clear();
                }
                else
                {
                    propertyInfo.SetValue(model, default(ModelT));
                }
            }
            return function.Success($"The form has been reset");
        }

        /// <summary>
        /// Given a property and a value resolve the value to a valid value for the property or return errors
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual async Task<(object? value, string? errors)> ResolvePropertyValue(ModelT model, string? property, string? value)
        {
            await Task.CompletedTask;

            object? result = null;
            if (property == null)
            {
                return (result, null);
            }

            // Convert to a valid instance of the schema type
            var (newValue, errors) = NormalizeValue(model, property, value);
            if (errors != null)
            {
                return (null, errors);
            }
            return (newValue, null);
        }

        /// <summary>
        /// Convert string value to the desired schema type and validate it.
        /// </summary>
        /// <param name="value">Recognizer string value.</param>
        /// <param name="property">Property name.</param>
        /// <returns>The resulting value, the expected type and any validation errors.</returns>
        /// <remarks>
        /// If there are validation errors then we would convert but the result did not match json schema validation.
        /// If the value is null and there is no type, then the property did not exist.
        /// </remarks>
        protected (object?, string?) NormalizeValue(ModelT model, string? property, string? value)
        {
            // Convert value to the appropriate JSON type
            // This is fixed because we ask GPT to translate.
            // var locale = dc.State.GetStringValue($"conversation.locale", "en-us");
            var locale = "en-us";

            if (!typeof(ModelT).TryGetPropertyInfo(property, out var propertyInfo))
            {
                return (null, $"I didn't understand {property}");
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model)
            {
                MemberName = propertyInfo.Name,
                DisplayName = propertyInfo.GetPropertyLabel(),
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
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                    }
                    return (realValue, null);
                case TypeCode.Boolean:
                    realValue = RecognizeBool(propertyInfo, value, "en-us");
                    if (!Validator.TryValidateProperty(realValue, context, validationResults))
                        return (null, GetErrorMessage(value, propertyInfo, validationResults));
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
                            var enumChoices = Enum.GetNames(valueType)
                                .Select(name =>
                                {
                                    Enum val = (Enum)Enum.Parse(valueType, name);
                                    var normalizedName = val.Humanize(LetterCasing.LowerCase);
                                    return new EnumChoice()
                                    {
                                        Name = normalizedName,
                                        Words = normalizedName.Tokenize(),
                                        Value = val
                                    };
                                }).ToList();

                            var normalizedValue = value.Humanize(LetterCasing.LowerCase).Singularize();
                            var incomingWords = normalizedValue.Tokenize().Select(t => t.ToLower()).ToList();
                            var wordCount = -1;
                            EnumChoice topChoice = null;

                            // partial matching
                            foreach (var choice in enumChoices)
                            {
                                if (choice.Name == normalizedValue ||
                                    choice.Name.Singularize() == normalizedValue.Singularize())

                                {
                                    topChoice = choice;
                                    break;
                                }
                                else
                                {
                                    // Count word matches for partial matches
                                    var count = 0;
                                    foreach (var word in choice.Words)
                                    {
                                        foreach (var incomingWord in incomingWords)
                                        {
                                            if (incomingWord == word ||
                                                incomingWord.Singularize() == word.Singularize())
                                                ++count;
                                        }
                                    }

                                    if (count > 0 && count > wordCount)
                                    {
                                        wordCount = count;
                                        topChoice = choice;
                                    }
                                }
                            }
                            if (topChoice != null)
                            {
                                realValue = topChoice.Value;
                            }

                            if (!Validator.TryValidateProperty(realValue, context, validationResults))
                                return (null, GetErrorMessage(value, propertyInfo, validationResults));
                            return (realValue, null);
                        }
                        else
                        {
                            realValue = RecognizeNumber(propertyInfo, value, "en-us");
                            if (realValue == null)
                            {
                                return (null, $"I didn't understand {value}");
                            }

                            if (!Validator.TryValidateProperty(realValue, context, validationResults))
                                return (null, GetErrorMessage(value, propertyInfo, validationResults));

                            return (realValue, null);
                        }
                    }
                case TypeCode.Single:
                case TypeCode.Double:
                    {
                        realValue = RecognizeNumber(propertyInfo, value, "en-us");
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                        return (realValue, null);
                    }

                case TypeCode.DateTime:
                    {
                        var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                        realValue = timex.Merge((DateTimeOffset?)propertyInfo.GetValue(model));
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                        {
                            foreach (var date in dates)
                            {
                                foreach (var time in times)
                                {
                                    realValue = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, TimeSpan.MinValue);
                                }
                            }
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                        }
                    }
                    return (realValue, null);



                case TypeCode.Object when valueType.Name == nameof(DateTimeOffset):
                    {
                        var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                        realValue = timex.Merge((DateTime?)propertyInfo.GetValue(model));
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                        return (realValue, null);
                    }

                case TypeCode.Object when valueType.Name == nameof(TimeOnly):
                    {
                        var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                        realValue = timex.Merge((TimeOnly?)propertyInfo.GetValue(model));
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                        return (realValue, null);
                    }

                case TypeCode.Object when valueType.Name == nameof(DateOnly):
                    {
                        var (timex, dates, times) = RecognizeTimex(propertyInfo, value, locale, refDate);
                        realValue = timex.Merge((DateOnly?)propertyInfo.GetValue(model));
                        if (!Validator.TryValidateProperty(realValue, context, validationResults))
                            return (null, GetErrorMessage(value, propertyInfo, validationResults));
                        return (realValue, null);
                    }
                default:
                    break;
            }
            return (null, "uhoho");
        }

        private static string GetErrorMessage(string value, PropertyInfo propertyInfo, List<ValidationResult> validationResults)
        {
            return $"{value} is not valid for {propertyInfo.GetPropertyLabel()} because: {string.Join(',', validationResults.Select(e => e.ErrorMessage))}";
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
}