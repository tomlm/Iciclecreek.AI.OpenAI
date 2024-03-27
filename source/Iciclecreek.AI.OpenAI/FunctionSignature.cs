using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.AI.OpenAI
{
    public class FunctionSignature
    {
        public FunctionSignature() 
        {
        }

        public FunctionSignature(string signature, string description, params string[] examples)
        {
            Signature = signature;
            Description = description;
            Examples = examples?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Signature for the intent (Example: Add(x, y) )
        /// </summary>
        public string Signature { get; set; } = String.Empty;

        /// <summary>
        /// Description of the intent (Example: Adds two numbers)
        /// </summary>
        public string Description { get; set; } = String.Empty;

        /// <summary>
        /// (OPTIONAL) Examples of intent
        /// </summary>
        public List<string> Examples { get; set; } = new List<string>();
    }
}
