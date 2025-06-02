using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Iciclecreek.AI.Forms
{
    /// <summary>
    /// Agent that uses a FormTask<T> and a chat client to process input using the form's tools.
    /// </summary>
    public class FormAgent<T> where T : class
    {
        private readonly IChatClient _chatClient;
        private readonly Form<T> _form;

        public FormAgent(IChatClient chatClient, Form<T> form)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public FormAgent(IChatClient chatClient) : this(chatClient, new Form<T>())
        {
        }

        /// <summary>
        /// Process input using the chat client and the form's tools.
        /// </summary>
        /// <param name="input">User input</param>
        /// <returns>ToolResult from the invoked tool</returns>
        public async Task<string> ProcessInputAsync(string input)
        {
            var schema = ""; // jsonObject.ToJsonString();// StructuredSchemaGenerator.FromType<FormChanges<Address>>();
            JsonElement jsonSchema;
            using (var doc = JsonDocument.Parse(schema))
            {
                jsonSchema = doc.RootElement.Clone();
            }

            // Use the public GetTools() method to get the tools
            var tools = _form.GetTools();

            var options = new ChatOptions()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema(jsonSchema),
                AllowMultipleToolCalls = true,
                Tools = _form.GetTools().ToList()
            };

            var chatCompletion = await _chatClient.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System,
                    $"""
                    The current date and time is: {DateTime.Now.ToString()}
                    You are an agent which is collecting form data for {_form.Purpose}.
                    Interpret the users text to update the form data by calling the appropriate tools.
                    Based on the tool results, you will either:
                    * If there are clarifications needed, ask the user for more information.
                    * If there is missing information, ask the user for it.
                    * If the form is ready, ask the user for confirmation to finialize and submit for form for it's purpose.
                    """),
                    new ChatMessage(ChatRole.User, "1 boogala ave. It's in Omaha, ooksa. I purchased the house in 2015, I think it was may 7th. We have lived there for 3 months"),
                ], options);
            
            return "";
        }

        /// <summary>
        /// Expose the underlying FormTask instance.
        /// </summary>
        public Form<T> Form => _form;
    }
}
