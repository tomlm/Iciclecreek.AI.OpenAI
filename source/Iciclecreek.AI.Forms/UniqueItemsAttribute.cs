using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Iciclecreek.AI.Forms
{
    /// <summary>
    /// Validation attribute that ensures all elements in a collection are unique.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UniqueItemsAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not IEnumerable enumerable || value is string)
                throw new ValidationException("UniqueAttribute can only be applied to collections.");

            var set = new HashSet<object?>();
            int index = 0;
            foreach (var item in enumerable)
            {
                if (!set.Add(item))
                {
                    return new ValidationResult($"Duplicate value found in {validationContext.MemberName} at index {index}.");
                }
                index++;
            }
            return ValidationResult.Success;
        }
    }
}
