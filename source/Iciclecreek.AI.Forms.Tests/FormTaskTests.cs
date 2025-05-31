using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Iciclecreek.AI.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Iciclecreek.AI.Forms.Tests
{
    [TestClass]
    public class FormTaskTests
    {
        [TestMethod]
        public void AssignValue_AssignsStringProperty()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("Name", "Alice");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual("Alice", formTask.Form.Name);
        }

        [TestMethod]
        public void AssignValue_AssignsIntProperty()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("Attendees", "5");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(5, formTask.Form.Attendees);
        }

        [TestMethod]
        public void AssignValue_AssignsDoubleProperty()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("Percent", "42.5");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(42.5, formTask.Form.Percent);
        }

        [TestMethod]
        public void AssignValue_AssignsBoolProperty()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("Cool", "true");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.IsTrue(formTask.Form.Cool);
        }

        [TestMethod]
        public void AssignValue_AssignsEnumProperty()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("FavoritePet", "Cats");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(Pets.Cats, formTask.Form.FavoritePet);
        }

        [TestMethod]
        public void AssignValue_InvalidProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("NonExistent", "value");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void AssignValue_ValidationFailure_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("PhoneNumber", "notaphone");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void AssignValue_ValidationFailure_ErrorsCollectionFilled()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AssignValue("PhoneNumber", "notaphone");
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Errors);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors[0].ErrorMessage.Contains("Phone"), "Expected error message to mention 'Phone'");
        }

        [TestMethod]
        public void AddValueToCollection_AddsStringToList()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AddValueToCollection("Categories", "Books");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.IsTrue(formTask.Form.Categories.Contains("Books"));
        }

        [TestMethod]
        public void AddValueToCollection_WorksOnIntCollection()
        {
            var form = new TestFormWithIntList();
            var formTask = new FormTask<TestFormWithIntList>(form);
            var result = formTask.AddValueToCollection("Numbers", "42");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.IsTrue(form.Numbers.Contains(42));
        }

        [TestMethod]
        public void AddValueToCollection_RejectsInvalidObjectType()
        {
            var form = new TestFormWithIntList();
            var formTask = new FormTask<TestFormWithIntList>(form);
            // Try to add a string to a List<int>
            var result = formTask.AddValueToCollection("Numbers", "notanumber");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void AddValueToCollection_HonorsValidation()
        {
            var form = new TestFormWithIntList();
            var formTask = new FormTask<TestFormWithIntList>(form);
            // Out of range (should fail validation)
            var result = formTask.AddValueToCollection("Numbers", "9999");
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Errors);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors[0].ErrorMessage.Contains("between 0 and 100"));
        }

        [TestMethod]
        public void AddValueToCollection_NonCollectionProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AddValueToCollection("Name", "Alice");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void AddValueToCollection_UnknownProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.AddValueToCollection("NotAProperty", "Value");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void RemoveValueFromCollection_RemovesStringFromList()
        {
            var formTask = new FormTask<TestForm>();
            formTask.Form.Categories.Add("Books");
            var result = formTask.RemoveValueFromCollection("Categories", "Books");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.IsFalse(formTask.Form.Categories.Contains("Books"));
        }

        [TestMethod]
        public void RemoveValueFromCollection_NonCollectionProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.RemoveValueFromCollection("Name", "Alice");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void RemoveValueFromCollection_UnknownProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.RemoveValueFromCollection("NotAProperty", "Value");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void ClearValue_ClearsListProperty()
        {
            var formTask = new FormTask<TestForm>();
            formTask.Form.Categories.Add("Books");
            formTask.Form.Categories.Add("Movies");
            var result = formTask.ClearValue("Categories");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(0, formTask.Form.Categories.Count);
        }

        [TestMethod]
        public void ClearValue_ClearsScalarProperty()
        {
            var formTask = new FormTask<TestForm>();
            formTask.Form.Name = "Alice";
            var result = formTask.ClearValue("Name");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.IsNull(formTask.Form.Name);
        }

        [TestMethod]
        public void ClearValue_UnknownProperty_ReturnsFailed()
        {
            var formTask = new FormTask<TestForm>();
            var result = formTask.ClearValue("NotAProperty");
            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod]
        public void GetValue_ReturnsPropertyValue()
        {
            var formTask = new FormTask<TestForm>();
            formTask.Form.Name = "Alice";
            var result = formTask.GetValue("Name");
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual("Alice", result.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GetValue_UnknownProperty_ThrowsException()
        {
            var formTask = new FormTask<TestForm>();
            formTask.GetValue("NotAProperty");
        }

        [TestMethod]
        public void ClearValue_ClearsAllFormProperties()
        {
            var formTask = new FormTask<TestForm>();
            // Set various properties
            formTask.Form.Name = "Alice";
            formTask.Form.Birthday = DateOnly.Parse("2000-01-01");
            formTask.Form.ArrivalTime = TimeOnly.Parse("09:00");
            formTask.Form.Percent = 50.0;
            formTask.Form.Attendees = 10;
            formTask.Form.Cool = true;
            formTask.Form.PhoneNumber = "123-456-7890";
            formTask.Form.Password = "secret";
            formTask.Form.FavoritePet = Pets.Cats;
            formTask.Form.Categories.Add("Books");

            // Clear each property
            Assert.IsTrue(formTask.ClearValue("Name").Succeeded);
            Assert.IsNull(formTask.Form.Name);
            Assert.IsTrue(formTask.ClearValue("Birthday").Succeeded);
            Assert.IsNull(formTask.Form.Birthday);
            Assert.IsTrue(formTask.ClearValue("ArrivalTime").Succeeded);
            Assert.IsNull(formTask.Form.ArrivalTime);
            Assert.IsTrue(formTask.ClearValue("Percent").Succeeded);
            Assert.IsNull(formTask.Form.Percent);
            Assert.IsTrue(formTask.ClearValue("Attendees").Succeeded);
            Assert.IsNull(formTask.Form.Attendees);
            Assert.IsTrue(formTask.ClearValue("Cool").Succeeded);
            Assert.IsNull(formTask.Form.Cool);
            Assert.IsTrue(formTask.ClearValue("PhoneNumber").Succeeded);
            Assert.IsNull(formTask.Form.PhoneNumber);
            Assert.IsTrue(formTask.ClearValue("Password").Succeeded);
            Assert.IsNull(formTask.Form.Password);
            Assert.IsTrue(formTask.ClearValue("FavoritePet").Succeeded);
            Assert.IsNull(formTask.Form.FavoritePet);
            Assert.IsTrue(formTask.ClearValue("Categories").Succeeded);
            Assert.AreEqual(0, formTask.Form.Categories.Count);
        }

        [TestMethod]
        public void AssignValue_And_GetValue_AllTypes()
        {
            var formTask = new FormTask<TestForm>();

            // String
            Assert.IsTrue(formTask.AssignValue("Name", "Alice").Succeeded);
            Assert.AreEqual("Alice", formTask.GetValue("Name").Value);

            // DateOnly
            Assert.IsTrue(formTask.AssignValue("Birthday", "2001-02-03").Succeeded);
            Assert.AreEqual(DateOnly.Parse("2001-02-03"), formTask.GetValue("Birthday").Value);

            // TimeOnly
            Assert.IsTrue(formTask.AssignValue("ArrivalTime", "10:15").Succeeded);
            Assert.AreEqual(TimeOnly.Parse("10:15"), formTask.GetValue("ArrivalTime").Value);

            // Double
            Assert.IsTrue(formTask.AssignValue("Percent", "77.7").Succeeded);
            Assert.AreEqual(77.7, formTask.GetValue("Percent").Value);

            // Int
            Assert.IsTrue(formTask.AssignValue("Attendees", "42").Succeeded);
            Assert.AreEqual(42, formTask.GetValue("Attendees").Value);

            // Bool
            Assert.IsTrue(formTask.AssignValue("Cool", "true").Succeeded);
            Assert.AreEqual(true, formTask.GetValue("Cool").Value);

            // Phone
            Assert.IsTrue(formTask.AssignValue("PhoneNumber", "555-555-5555").Succeeded);
            Assert.AreEqual("555-555-5555", formTask.GetValue("PhoneNumber").Value);

            // Password
            Assert.IsTrue(formTask.AssignValue("Password", "secret").Succeeded);
            Assert.AreEqual("secret", formTask.GetValue("Password").Value);

            // Enum
            Assert.IsTrue(formTask.AssignValue("FavoritePet", "Dogs").Succeeded);
            Assert.AreEqual(Pets.Dogs, formTask.GetValue("FavoritePet").Value);

            // List<string>
            Assert.IsTrue(formTask.AddValueToCollection("Categories", "Books").Succeeded);
            var categories = formTask.GetValue("Categories").Value as List<string>;
            Assert.IsNotNull(categories);
            Assert.IsTrue(categories.Contains("Books"));
        }

        [TestMethod]
        public void AssignValue_AssignsDurationProperty()
        {
            var formTask = new FormTask<TestForm>();
            // ISO-8601 duration for 1 day 4 hours: P1DT4H
            var isoDuration = "P1DT4H";
            var expected = new TimeSpan(1, 4, 0, 0); // 1 day, 4 hours
            var result = formTask.AssignValue("Duration", isoDuration);
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(expected, formTask.Form.Duration);
        }

        [TestMethod]
        public void AssignValue_AssignsDurationProperty_TimeSpanFormat()
        {
            var formTask = new FormTask<TestForm>();
            // TimeSpan serialization format: c (constant, e.g. "1.04:00:00" for 1 day 4 hours)
            var timeSpanString = "1.04:00:00";
            var expected = new TimeSpan(1, 4, 0, 0); // 1 day, 4 hours
            var result = formTask.AssignValue("Duration", timeSpanString);
            Assert.IsTrue(result.Succeeded, result.Description);
            Assert.AreEqual(expected, formTask.Form.Duration);
        }

        public class TestFormWithIntList
        {
            [ItemValidation("Range(0, 100)")]
            public List<int> Numbers { get; set; } = new List<int>();
        }
    }
}
