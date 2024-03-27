using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Iciclecreek.AI.OpenAI.Tests
{
    [TestClass]
    public class RecognizerUnitTests
    {
        private static Lazy<IServiceProvider> _services = new Lazy<IServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(sp => new ConfigurationBuilder()
                        .AddUserSecrets<RecognizerUnitTests>()
                        .Build());
            services.AddSingleton<OpenAIClient>((sp) => new OpenAIClient(sp.GetRequiredService<IConfiguration>()["OpenAIKey"]));
            services.AddTransient<FunctionsRecognizer>();
            services.AddTransient<MathFunctionRecognizer>();
            return services.BuildServiceProvider();
        });

        [TestMethod]
        public async Task TestGibberishIsNothing()
        {
            var recognizer = _services.Value.GetRequiredService<MathFunctionRecognizer>();
            var functions = await recognizer.RecognizeAsync("gibberish", $"These are not the droids you are seeking.");
            Assert.IsNotNull(functions);
            Assert.AreEqual(0, functions.Count);
        }

        [TestMethod]
        public async Task TestEmpty()
        {
            var recognizer = _services.Value.GetRequiredService<MathFunctionRecognizer>();
            var  functions = await recognizer.RecognizeAsync("");
            Assert.IsNotNull(functions);
            Assert.AreEqual(0, functions.Count);
        }

        [TestMethod]
        public async Task TestMath()
        {
            var recognizer = _services.Value.GetRequiredService<MathFunctionRecognizer>();
            var functions = await recognizer.RecognizeAsync("What is 5x3? What is 1+2? I want to subtract 73 from 3000...");
            Assert.IsNotNull(functions);
            Assert.AreEqual(3, functions.Count);

            Assert.AreEqual("Multiply", functions[0].Name);
            Assert.AreEqual("5", functions[0].Args[0].ToString());
            Assert.AreEqual("3", functions[0].Args[1].ToString());

            Assert.AreEqual("Add", functions[1].Name);
            Assert.AreEqual("1", functions[1].Args[0].ToString());
            Assert.AreEqual("2", functions[1].Args[1].ToString());

            Assert.AreEqual("Subtract", functions[2].Name);
            Assert.AreEqual("3000", functions[2].Args[0].ToString());
            Assert.AreEqual("73", functions[2].Args[1].ToString());
        }

        [TestMethod]
        public async Task TestMultiplySame()
        {
            var recognizer = _services.Value.GetRequiredService<MathFunctionRecognizer>();
            var functions = await recognizer.RecognizeAsync("What is 5x3? What is five by four? Multiply fifteen by one hundred and 3");
            Assert.IsNotNull(functions);
            Assert.AreEqual(3, functions.Count);

            Assert.AreEqual("Multiply", functions[0].Name);
            Assert.AreEqual("5", functions[0].Args[0].ToString());
            Assert.AreEqual("3", functions[0].Args[1].ToString());

            Assert.AreEqual("Multiply", functions[1].Name);
            Assert.AreEqual("5", functions[1].Args[0].ToString());
            Assert.AreEqual("4", functions[1].Args[1].ToString());

            Assert.AreEqual("Multiply", functions[2].Name);
            Assert.AreEqual("15", functions[2].Args[0].ToString());
            Assert.AreEqual("103", functions[2].Args[1].ToString());
        }
    }
}