using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Iciclecreek.AI.OpenAI
{
    /// <summary>
    /// SemanticActionRecognizer is a recognizer which uses OpenAI model to recognize multiple semantic actions
    /// </summary>
    public class SemanticActionRecognizer
    {
        private readonly OpenAIClient _openAIClient;

        public SemanticActionRecognizer(OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        /// <summary>
        /// Functions to recognize
        /// </summary>
        public List<SemanticActionDefinition> Actions = new List<SemanticActionDefinition>();

        /// <summary>
        /// Recognize actions in text
        /// </summary>
        /// <param name="text">text</param>
        /// <param name="modelOrDeploymentName">The OpenAI model name or Azure OpenAI DeploymentName</param>
        /// <param name="instructions">Optional instructions to insert in to System instructions</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of identified functions.</returns>
        public virtual async Task<List<SemanticAction>> RecognizeAsync(string text, string modelOrDeploymentName = "gpt-3.5-turbo", string? instructions = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return new List<SemanticAction>();
            }

            var messages = new List<ChatRequestMessage>();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"I am a bot which excels at examining users text and identifying functions in it from the user text.");
            sb.AppendLine($"Today is: {DateTime.Now}.");
            if (!String.IsNullOrWhiteSpace(instructions))
                sb.AppendLine(instructions);

            sb.AppendLine("Functions have the signature of FUNCTION(`...`, `...`, ...)");
            sb.AppendLine($"The FUNCTIONLIST is:[{ String.Join(",", Actions.Select(a => a.Name))}]");
            sb.AppendLine("The function definitions are:");
            foreach (var action in Actions)
            {
                sb.AppendLine($" {action.Name}");
                sb.AppendLine($"    signature: {action.GetSignature()}");
                sb.AppendLine($"    description: {action.Description}");
                sb.AppendLine($"    examples:");
                foreach (var example in action.Examples)
                {
                    sb.AppendLine($"    - {example.Text} => {example.Output}");
                }
            }
            sb.AppendLine();
            sb.AppendLine(@"Transform the user text only (not the bot text) into functions in the FUNCTIONLIST.");
            messages.Add(new ChatRequestSystemMessage(sb.ToString()));
            messages.Add(new ChatRequestUserMessage(text));
            messages.Add(new ChatRequestAssistantMessage("The comma delimited list of functions found is "));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var response = await GetCompletionsAsync(modelOrDeploymentName, messages, 0.0f, _openAIClient, maxTokens: 1500, cancellationToken: cancellationToken);
            sw.Stop();

            Debug.WriteLine($"===== RECOGNIZE RESPONSE {sw.Elapsed}");
            Debug.WriteLine(response);

            var functions = new List<SemanticAction>();
            if (response != null)
            {
                functions = ParseFunctions(response.Trim());
            }

            return functions;
        }


        private async Task<string?> GetCompletionsAsync(string modelOrDeployment, List<ChatRequestMessage> messages, float temp, OpenAIClient openAIClient, string[]? stopWords = null, int maxTokens = 1500, float topP = 0.0f, float presencePenalty = 0.0f, float frequencyPenalty = 0.0f, int? timeout = null, CancellationToken cancellationToken = default)
        {
            if (messages.Count == 0)
            {
                return String.Empty;
            }

            for (int i = 0; i < 3; i++)
            {

                try
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeout.HasValue ? timeout.Value : 5 * 1000);

                    var options = new ChatCompletionsOptions()
                    {
                        DeploymentName = modelOrDeployment,
                        Temperature = temp,
                        MaxTokens = maxTokens,
                        NucleusSamplingFactor = (float)0.0,
                        FrequencyPenalty = frequencyPenalty,
                        PresencePenalty = presencePenalty,
                        User = Guid.NewGuid().ToString("n")
                    };
                    foreach (var message in messages)
                        options.Messages.Add(message);

                    var response = await openAIClient.GetChatCompletionsAsync(/*model, */options, cts.Token);
                    return response.Value.Choices.FirstOrDefault()?.Message.Content;

                }
                catch (Exception err)
                {
                    Debug.WriteLine(err.Message);
                }
            }
            return null;
        }

        // write method to properly tokenize and parse comma delimited list of functions like:
        // F("arg1", "Arg,2"), F(arg1), ... into a list of Command objects        
        // F('arg1', ['arg2','arg3']), F(arg1), ... into a list of Command objects

        public static List<SemanticAction> ParseFunctions(string text)
        {
            if (text.StartsWith("FUNCTIONLIST:"))
                text = text.Substring("FUNCTIONLIST:".Length);
            text = text.TrimStart('[', '-', ' ').TrimEnd(']', ' ');

            var functions = new List<SemanticAction>();
            var function = new SemanticAction();
            var arg = "";
            var inArg = false;
            char quoteChar = ' ';
            var inFunctionName = false;
            bool inArray = false;
            List<string> arrayArgs = new List<string>();

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inArg)
                {
                    if (quoteChar == ' ' && c == '`')
                    {
                        inArg = true;
                        quoteChar = c;
                        continue;
                    }
                    else if (quoteChar != ' ')
                    {
                        if (c == quoteChar)
                        {
                            quoteChar = ' ';
                            arg = arg.Trim().Trim('\'', '\"');
                            if (!String.IsNullOrWhiteSpace(arg))
                            {
                                if (inArray)
                                    arrayArgs.Add(arg.Trim());
                                else
                                    function?.Args.Add(arg.Trim());
                            }
                            arg = "";
                            continue;
                        }
                        else
                        {
                            arg += c;
                            continue;
                        }
                    }
                    else if (c == ')')
                    {
                        // Completed string
                        inArg = false;
                        inFunctionName = false;
                        arg = arg.Trim().Trim('\'', '\"');
                        if (!String.IsNullOrWhiteSpace(arg))
                        {
                            if (inArray)
                                arrayArgs.Add(arg.Trim());
                            else
                                function?.Args.Add(arg.Trim());
                        }
                        functions.Add(function!);
                        function = null;
                        arg = "";
                        continue;
                    }
                    else if (c == ',')
                    {
                        arg = arg.Trim().Trim('\'', '\"');
                        if (!String.IsNullOrWhiteSpace(arg))
                        {
                            if (inArray)
                                arrayArgs.Add(arg.Trim());
                            else
                                function?.Args.Add(arg.Trim());
                        }
                        arg = "";
                    }
                    else if (!inArray && c == '[')
                    {
                        inArray = true;
                        continue;
                    }
                    else if (inArray && c == ']')
                    {
                        arg = arg.Trim().Trim('\'', '\"');
                        if (!String.IsNullOrWhiteSpace(arg))
                        {
                            arrayArgs.Add(arg.Trim());
                        }
                        function?.Args.AddRange(arrayArgs);
                        inArray = false;
                        arg = "";
                        continue;
                    }
                    else
                    {
                        arg += c;
                        continue;
                    }
                }
                else if (!inFunctionName)
                {
                    if (Char.IsLetter(c))
                    {
                        inFunctionName = true;
                        arg += c;
                    }
                    // else skip char;
                    continue;
                }
                else if (inFunctionName)
                {
                    if (c == '(')
                    {
                        inFunctionName = false;
                        inArg = true;
                        function = new SemanticAction();
                        function.Name = arg.Trim();
                        arg = "";
                    }
                    else
                    {
                        arg += c;
                        continue;
                    }
                }
            }
            return functions;
        }


    }
}

