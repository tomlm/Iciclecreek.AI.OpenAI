using Azure.AI.OpenAI;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using System.ComponentModel.Design;
using System.Linq;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public class FormFillRecognizer<ModelT> : SemanticActionRecognizer
    {
        private string _model;

        public FormFillRecognizer(OpenAIClient openAIClient) :
            base(openAIClient)
        {
            Actions.Add(new SemanticActionDefinition(FormFillActions.ASSIGN, "assign to a property or property array a single value.")
                .AddArgument("property")
                .AddArgument("value")
                .AddExample("My name is joe", "name", "joe"));

            Actions.Add(new SemanticActionDefinition(FormFillActions.REMOVE, "remove a value from a property.")
                .AddArgument("property")
                .AddArgument("value")
                .AddExample("Remove apple from cart", "card", "apple"));

            Actions.Add(new SemanticActionDefinition(FormFillActions.CLEAR, "clears the property values to default state")
                .AddArgument("property")
                .AddExample("Clear name", "name"));

            Actions.Add(new SemanticActionDefinition(FormFillActions.RESET, "resets all of the property values to default state to start the form over.")
                .AddExample("Start over"));
        }

        public override async Task<List<SemanticAction>> RecognizeAsync(string text, string modelOrDeploymentName = "gpt-3.5-turbo", string? instructions = null, CancellationToken cancellationToken = default)
        {
            StringBuilder sb = new StringBuilder(instructions);
            sb.AppendLine($"The properties for the form are:");

            foreach (var property in typeof(ModelT).GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (type.IsEnum || type == typeof(String))
                    sb.AppendLine($"  `{property.Name}` which is a String used for {property.GetPropertyLabel()}");
                else if (type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
                    sb.AppendLine($"  `{property.Name}` which is a date used for {property.GetPropertyLabel()}");
                else if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
                    sb.AppendLine($"  `{property.Name}` which is time used for {property.GetPropertyLabel()}");
                else if (type.IsList())
                    sb.AppendLine($"  `{property.Name}` which is {type.GetGenericArguments().First().Name}[] used for {property.GetPropertyLabel()}");
                else if (!type.IsValueType)
                    sb.AppendLine($"  `{property.Name}` which is Text used for {property.GetPropertyLabel()}");
                else
                    sb.AppendLine($"  `{property.Name}` which is {type.Name} used for {property.GetPropertyLabel()}");
            }

            return await base.RecognizeAsync(text, modelOrDeploymentName, sb.ToString(), cancellationToken);
        }

    }
}

