using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Iciclecreek.AI.OpenAI.FormFill.Attributes
{
    /// <summary>
    /// Composite validation attribute that applies multiple validation attributes to each item in a collection.
    /// Accepts an array of strings, each representing a validation attribute type and optional constructor arguments, e.g. "RangeAttribute(0,100)".
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, 
                    AllowMultiple = true)]
    public class ItemValidationAttribute : ValidationAttribute
    {
        private readonly ValidationAttribute _validator;

        public ItemValidationAttribute(Type validationType, params object?[] args)
        {
            _validator = (ValidationAttribute)Activator.CreateInstance(validationType, args);
        }

        public ValidationResult? ValidateItem(object item)
        {
            var context = new ValidationContext(item ?? new object());
            return _validator.GetValidationResult(item, context);
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
                var context = new ValidationContext(item ?? new object(), validationContext, validationContext.Items)
                {
                    MemberName = $"{validationContext.MemberName}[{index}]"
                };
                var result = _validator.GetValidationResult(item, context);
                if (result != null && result != ValidationResult.Success)
                {
                    return result;
                }
                index++;
            }

            return ValidationResult.Success;
        }
    }
}
