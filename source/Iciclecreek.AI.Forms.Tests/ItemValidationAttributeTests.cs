using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Iciclecreek.AI.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Iciclecreek.AI.Forms.Tests
{
    [TestClass]
    public class ItemValidationAttributeTests
    {
        public class IntListForm
        {
            [ItemValidation("Range(1, 50)")]
            [UniqueItems]
            public List<int> Numbers { get; set; } = new List<int>();
        }

        [TestMethod]
        public void ItemValidation_AllowsValidUniqueIntegers()
        {
            var form = new IntListForm { Numbers = new List<int> { 1, 2, 3, 50 } };
            var context = new ValidationContext(form) { MemberName = nameof(form.Numbers) };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(form.Numbers, context, results);
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void ItemValidation_RejectsOutOfRange()
        {
            var form = new IntListForm { Numbers = new List<int> { 1, 2, 100 } };
            var context = new ValidationContext(form) { MemberName = nameof(form.Numbers) };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(form.Numbers, context, results);
            Assert.IsFalse(isValid);
            Assert.IsTrue(results[0].ErrorMessage.Contains("failed validation"));
        }

        [TestMethod]
        public void ItemValidation_RejectsDuplicates()
        {
            var form = new IntListForm { Numbers = new List<int> { 1, 2, 2 } };
            var context = new ValidationContext(form) { MemberName = nameof(form.Numbers) };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(form.Numbers, context, results);
            Assert.IsFalse(isValid);
            Assert.IsTrue(results[0].ErrorMessage.Contains("Duplicate"));
        }
    }
}
