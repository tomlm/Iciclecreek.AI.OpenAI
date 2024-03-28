
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

        // string or array 
        public List<object> Args { get; set; } = new List<object>();

        public override string ToString()
        {
            var args = string.Join(',', Args.Select(arg =>
            {
                if (arg is string)
                    return $"'{arg}'";
                else if (arg.GetType().IsArray)
                    return $"[{string.Join(',', (arg as Array).OfType<object>().Select(a => $"'{a}'"))}]";
                return arg.ToString();
            }));
            return $"{Name}({args}";
        }
    }
}
