using System.ComponentModel.DataAnnotations;
using Iciclecreek.AI.OpenAI.FormFill.Attributes;

namespace Iciclecreek.AI.Forms.Tests
{
    [TestClass]
    public class ItemValidationAttributeTests
    {
        public class IntListForm
        {
            public static Attribute[] ItemAttributes = new Attribute[]
            {
                new RangeAttribute(1,50),
            };

            [ItemValidation(typeof(RangeAttribute), 1, 50)]
            [UniqueItems]
            public List<int> Numbers { get; set; } = new List<int>();
        }

        public class EmailListForm
        {
            [ItemValidation(typeof(EmailAddressAttribute))]
            public List<string> Emails { get; set; } = new List<string>();
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
            Assert.IsTrue(results[0].ErrorMessage.Contains("between 1 and 50"));
            Assert.IsTrue(results[0].ErrorMessage.Contains("Numbers[2]"));
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

        [TestMethod]
        public void ItemValidation_AllowsValidEmails()
        {
            var form = new EmailListForm { Emails = new List<string> { "a@example.com", "b@example.com" } };
            var context = new ValidationContext(form) { MemberName = nameof(form.Emails) };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(form.Emails, context, results);
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void ItemValidation_RejectsInvalidEmails()
        {
            var form = new EmailListForm { Emails = new List<string> { "a@example.com", "notanemail" } };
            var context = new ValidationContext(form) { MemberName = nameof(form.Emails) };
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(form.Emails, context, results);
            Assert.IsFalse(isValid);
            Assert.IsTrue(results[0].ErrorMessage.Contains("Email"));
        }
    }
}
