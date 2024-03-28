using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Iciclecreek.AI.OpenAI.FormFill.Tests
{

    [TestClass]
    public class FormFillTests
    {
        private static Lazy<IServiceProvider> _services = new Lazy<IServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(sp => new ConfigurationBuilder()
                        .AddUserSecrets<FormFillTests>()
                        .Build());
            services.AddSingleton<OpenAIClient>((sp) => new OpenAIClient(sp.GetRequiredService<IConfiguration>()["OpenAIKey"]));
            services.AddFormFiller<TestForm>();
            return services.BuildServiceProvider();
        });


        [TestMethod]
        public async Task TestForm_String()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            var results = await formFiller.EditModelAsync(model, "My name is joe", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.AreEqual("joe", model.Name);
            
            results = await formFiller.EditModelAsync(model, "Forget my name", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.Name);
        }

        [TestMethod]
        public async Task TestForm_Number()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            var results = await formFiller.EditModelAsync(model, "I will be attending with 2 friends", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNotNull(model.Attendees);
            Assert.IsTrue(model.Attendees > 0); // sometimes the model will include the speaker, sometimes not. Both are valid interpretations.


            results = await formFiller.EditModelAsync(model, "Forget attendees", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.Attendees);
        }

        [TestMethod]
        public async Task TestForm_Bool()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            Assert.IsNull(model.Cool);

            var results = await formFiller.EditModelAsync(model, "I am cool", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNotNull(model.Cool);
            Assert.IsTrue(model.Cool);


            results = await formFiller.EditModelAsync(model, "Forget I'm cool", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.Cool);
        }

        [TestMethod]
        public async Task TestForm_TimeOnly()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            Assert.IsNull(model.ArrivalTime);

            var results = await formFiller.EditModelAsync(model, "We'll get in at 1 pm", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(FormFillActions.ASSIGN, results[0].Action.Name);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.AreEqual(TimeOnly.Parse("1pm"), model.ArrivalTime);

            results = await formFiller.EditModelAsync(model, "We'll get in at 2:30 in the afternoon", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.AreEqual(TimeOnly.Parse("2:30 pm"), model.ArrivalTime);

            results = await formFiller.EditModelAsync(model, "Please clear out my arrival time", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.ArrivalTime);
        }

        [TestMethod]
        public async Task TestForm_DateOnly()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            Assert.IsNull(model.ArrivalTime);

            var results = await formFiller.EditModelAsync(model, "My birthday is may 25, 1967", default(CancellationToken));
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.AreEqual(DateOnly.Parse("5/25/1967"), model.Birthday);

            results = await formFiller.EditModelAsync(model, "Please clear out my birthday", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.Birthday);
        }

        [TestMethod]
        public async Task TestForm_Enum()
        {
            var formFiller = _services.Value.GetRequiredService<FormFillEngine<TestForm>>();
            var model = new TestForm();
            Assert.IsNull(model.ArrivalTime);

            var results = await formFiller.EditModelAsync(model, "My favorite pet is a gerbil. My favorite pet is a horse ", default(CancellationToken));
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(Pets.Horses, model.FavoritePet);
            Assert.AreEqual(SemanticActionResultType.Success, results[1].Result);

            results = await formFiller.EditModelAsync(model, "clear favorite pet", default(CancellationToken));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(SemanticActionResultType.Success, results[0].Result);
            Assert.IsNull(model.FavoritePet);
        }

    }
}