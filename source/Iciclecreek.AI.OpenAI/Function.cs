
using System;
using System.Collections.Generic;

namespace Iciclecreek.AI.OpenAI
{

    public class Function
    {
        public string Name { get; set; } = String.Empty;

        public List<object> Args { get; set; } = new List<object>();

        public override string ToString()
        {
            return $"{Name}({string.Join(',', Args)})";
        }
    }
}
