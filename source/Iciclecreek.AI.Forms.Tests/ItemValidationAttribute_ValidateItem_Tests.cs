using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Iciclecreek.AI.Forms;
using Iciclecreek.AI.OpenAI.FormFill.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Iciclecreek.AI.Forms.Tests
{
    [TestClass]
    public class ItemValidationAttribute_ValidateItem_Tests
    {
        [TestMethod]
        public void ValidateItem_AllowsValidItem()
        {
            var attr = new ItemValidationAttribute("Range(1, 10)");
            var result = attr.ValidateItem(5);
            Assert.AreEqual(ValidationResult.Success, result);
        }

        [TestMethod]
        public void ValidateItem_RejectsInvalidItem()
        {
            var attr = new ItemValidationAttribute("Range(1, 10)");
            var result = attr.ValidateItem(20);
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.ErrorMessage.Contains("between 1 and 10"), "Expected error message to mention 'between'");
        }
    }
}
