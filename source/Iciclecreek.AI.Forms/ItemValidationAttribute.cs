using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Iciclecreek.AI.Forms
{
    /// <summary>
    /// Composite validation attribute that applies multiple validation attributes to each item in a collection.
    /// Accepts an array of strings, each representing a validation attribute type and optional constructor arguments, e.g. "RangeAttribute(0,100)".
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ItemValidationAttribute : ValidationAttribute
    {
        private readonly ValidationAttribute[] _validators;

        public ItemValidationAttribute(params string[] validatorSpecs)
        {
            if (validatorSpecs == null || validatorSpecs.Length == 0)
                throw new ArgumentException("At least one validator spec must be specified.");
            _validators = validatorSpecs.Select(ParseValidatorSpec).ToArray();
        }

        private static ValidationAttribute ParseValidatorSpec(string spec)
        {
            // Example: "RangeAttribute(0,100)" or "RequiredAttribute()"
            var openParen = spec.IndexOf('(');
            var closeParen = spec.LastIndexOf(')');
            string typeName;
            object[] args = Array.Empty<object>();
            if (openParen > 0 && closeParen > openParen)
            {
                typeName = spec.Substring(0, openParen).Trim();
                var argsString = spec.Substring(openParen + 1, closeParen - openParen - 1);
                args = argsString.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Select(ParseArg).ToArray();
            }
            else
            {
                typeName = spec.Trim();
            }
            if (!typeName.EndsWith("Attribute"))
                typeName += "Attribute"; // Ensure it ends with Attribute   

            // Try to resolve type
            var type = Type.GetType(typeName) ??
                       AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            if (type == null || !typeof(ValidationAttribute).IsAssignableFrom(type))
                throw new ArgumentException($"Could not resolve ValidationAttribute type: {typeName}");
            return (ValidationAttribute)Activator.CreateInstance(type, args)!;
        }

        private static object ParseArg(string arg)
        {
            // Try to parse as int, double, bool, or fallback to string
            if (int.TryParse(arg, out var i)) return i;
            if (double.TryParse(arg, out var d)) return d;
            if (bool.TryParse(arg, out var b)) return b;
            if (arg.StartsWith("\"") && arg.EndsWith("\"")) return arg.Substring(1, arg.Length - 2);
            return arg;
        }

        public ValidationResult ValidateItem(object item)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(item ?? new object());
            foreach (var validator in _validators)
            {
                var result = validator.GetValidationResult(item, context);
                if (result != ValidationResult.Success)
                {
                    return result;
                }
            }
            return ValidationResult.Success;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not IEnumerable enumerable || value is string)
                throw new ValidationException("ItemValidationAttribute can only be applied to collections.");

            var results = new List<ValidationResult>();
            int index = 0;
            foreach (var item in enumerable)
            {
                foreach (var validator in _validators)
                {
                    var context = new ValidationContext(item ?? new object(), validationContext, validationContext.Items)
                    {
                        MemberName = $"{validationContext.MemberName}[{index}]"
                    };
                    var result = validator.GetValidationResult(item, context);
                    if (result != ValidationResult.Success)
                    {
                        results.Add(result);
                    }
                }
                index++;
            }
            if (results.Count > 0)
            {
                return new ValidationResult($"One or more items in {validationContext.MemberName} failed validation.", results.SelectMany(r => r.MemberNames).ToList());
            }
            return ValidationResult.Success;
        }
    }
}
