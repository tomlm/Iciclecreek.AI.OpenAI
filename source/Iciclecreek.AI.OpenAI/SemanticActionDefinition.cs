using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.AI.OpenAI
{
    public class SemanticActionDefinition
    {
        public SemanticActionDefinition()
        {
        }

        public SemanticActionDefinition(string name, string? description = default)
        {
            Name = name;
            Description = description ?? String.Empty;
        }

        public SemanticActionDefinition AddArgument(string name)
        {
            this.Args.Add(name);
            return this;
        }

        public SemanticActionDefinition AddExample(string textInput, params string[] arguments)
        {
            this.Examples.Add(new SemanticExample()
            {
                Text = textInput,
                Output = $"{Name}({string.Join(',', arguments.Select(a => $"`{a}`"))})"
            });
            return this;
        }

        /// <summary>
        /// Signature for the intent (Example: Add(x, y) )
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// 
        /// </summary>
        public List<string> Args { get; set; } = new List<string>();

        /// <summary>
        /// Description of the intent (Example: Adds two numbers)
        /// </summary>
        public string Description { get; set; } = String.Empty;

        /// <summary>
        /// (OPTIONAL) Examples of intent
        /// </summary>
        public List<SemanticExample> Examples { get; set; } = new List<SemanticExample>();

        public string GetSignature()
        {
            return $"{Name}({string.Join(',', Args)})";
        }
    }

    public class SemanticExample
    {
        public string Text { get; set; }

        public string Output { get; set; }
    }
}
