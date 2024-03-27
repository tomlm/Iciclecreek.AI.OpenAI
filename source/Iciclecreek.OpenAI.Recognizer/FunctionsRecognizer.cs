using Azure.AI.OpenAI;
using System.Diagnostics;
using System.Text;

namespace Iciclecreek.OpenAI.Recognizer
{
    /// <summary>
    /// Recognizer which recognizes multiple commands as a single "Functions" intent.
    /// </summary>
    public class FunctionsRecognizer
    {
        private const string IM_START = "<|im_start|>";
        private const string IM_END = "<|im_end|>";
        private const string STOP = "<|STOP";

        private readonly OpenAIClient _openAIClient;

        public FunctionsRecognizer(OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        /// <summary>
        /// Functions to recognize
        /// </summary>
        public List<FunctionDefinition> Functions = new List<FunctionDefinition>();

        public async virtual Task<List<Function>> RecognizeAsync(string model, string text, string instructions = null, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("===== RECOGNIZE PROMPT");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<|im_start|>system\r\nI am a bot which excels at examining users text and identifying functions in it from the user text.\r\n");
            sb.AppendLine($"Today is: {DateTime.Now}");
            sb.AppendLine();
            if (!String.IsNullOrEmpty(instructions))
            {
                sb.AppendLine(instructions);
                sb.AppendLine();
            }
            sb.AppendLine("FUNCTIONLIST:");
            foreach (var intent in Functions)
            {
                sb.AppendLine($"* {intent.Signature} - {intent.Description}. Examples:");
                foreach (var example in intent.Examples)
                {
                    sb.AppendLine($"  - {example}");
                }
            }
            sb.AppendLine();
            sb.AppendLine(@"Transform the user text only (not the bot text) into a list of identified functions (from FUNCTIONLIST).");
            //sb.AppendLine("Bot said:");
            //sb.AppendLine(lastPrompt);
            sb.AppendLine("<|im_end|>");
            sb.AppendLine("<|im_start|>user");
            sb.AppendLine(text);
            sb.AppendLine("<|im_end|>");
            sb.AppendLine("<|im_start|>assistant");
            sb.AppendLine("The comma delimited list of functions found is ");

            Debug.WriteLine(sb.ToString());

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var response = await GetCompletionsAsync(model, sb.ToString(), 0.0f, _openAIClient, maxTokens: 1500, cancellationToken: cancellationToken);
            sw.Stop();

            Debug.WriteLine($"===== RECOGNIZE RESPONSE {sw.Elapsed}");
            Debug.WriteLine(response);

            var functions = new List<Function>();
            if (response != null)
            {
                functions = ParseFunctions(response.Trim());
            }

            return functions;
        }


        private async Task<string> GetCompletionsAsync(string model, string prompt, float temp, OpenAIClient openAIClient, string[] stopWords = null, int maxTokens = 1500, float topP = 0.0f, float presencePenalty = 0.0f, float frequencyPenalty = 0.0f, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 3; i++)
            {

                try
                {

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeout.HasValue ? timeout.Value.Milliseconds : 5 * 1000);

                    var options = new ChatCompletionsOptions()
                    {
                        DeploymentName = "gpt-3.5-turbo", // Use DeploymentName for "model" with non-Azure clients
                        Temperature = temp,
                        MaxTokens = maxTokens,
                        NucleusSamplingFactor = (float)0.0,
                        FrequencyPenalty = frequencyPenalty,
                        PresencePenalty = presencePenalty,
                        User = Guid.NewGuid().ToString("n")
                    };
                    if (stopWords != null)
                    {
                        foreach (var stopWord in stopWords)
                        {
                            options.StopSequences.Add(stopWord);
                        }
                    }
                    else
                    {
                        options.StopSequences.Add(STOP);
                    }
                    Debug.WriteLine($"---> STOPWORDS: [{string.Join(",", options.StopSequences)}]");

                    ChatRole chatRole = ChatRole.User;
                    var chatTexts = prompt.Split(IM_START, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var chatText in chatTexts)
                    {
                        ChatRequestMessage message = null;
                        if (chatText.StartsWith("user"))
                        {
                            int iEnd = chatText.IndexOf(IM_END);
                            message = new ChatRequestUserMessage(chatText.Substring("user".Length, iEnd - "user".Length));
                        }
                        else if (chatText.StartsWith("assistant"))
                        {
                            int iEnd = chatText.IndexOf(IM_END);
                            if (iEnd > 0)
                                message = new ChatRequestAssistantMessage(chatText.Substring("assistant".Length, iEnd - "assistant".Length));
                            else
                                message = new ChatRequestAssistantMessage(chatText.Substring("assistant".Length));
                        }
                        else if (chatText.StartsWith("system"))
                        {
                            int iEnd = chatText.IndexOf(IM_END);
                            message = new ChatRequestSystemMessage(chatText.Substring("system".Length, iEnd - "system".Length));
                        }

                        options.Messages.Add(message);
                    }

                    var response = await openAIClient.GetChatCompletionsAsync(/*model, */options, cts.Token);
                    return response.Value.Choices.FirstOrDefault()?.Message.Content;

                }
                catch (Exception err)
                {

                }
            }
            return "Failed";
        }

        // write method to properly tokenize and parse comma delimited list of functions like:
        // F("arg1", "Arg,2"), F(arg1), ... into a list of Command objects        
        // F('arg1', ['arg2','arg3']), F(arg1), ... into a list of Command objects

        public static List<Function> ParseFunctions(string text)
        {
            if (text.StartsWith("FUNCTIONLIST:"))
                text = text.Substring("FUNCTIONLIST:".Length);
            text = text.Trim().TrimStart('[').TrimEnd(']');

            var functions = new List<Function>();
            var function = new Function();
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
                    if (quoteChar == ' ' && c == '"')
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
                                    function.Args.Add(arg.Trim());
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
                                function.Args.Add(arg.Trim());
                        }
                        functions.Add(function);
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
                                function.Args.Add(arg.Trim());
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
                        function.Args.Add(arrayArgs);
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
                        function = new Function();
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

