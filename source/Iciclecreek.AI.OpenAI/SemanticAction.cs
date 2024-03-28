
using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.AI.OpenAI
{
    /// <summary>
    /// This class represents an identified semantic action with semantic args 
    /// </summary>
    /// <remarks>
    /// Example might be Add('fourteen','33')
    /// </remarks>
    public class SemanticAction
    {
        public string Name { get; set; } = String.Empty;

        public List<string> Args { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"{Name}({string.Join(',', Args.Select(a => $"'{a.Trim('\'','"')}'"))}";
        }
    }
}
