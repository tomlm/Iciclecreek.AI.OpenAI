using System;
using System.Collections.Generic;

namespace Iciclecreek.AI.OpenAI.FormFill
{

    internal class EnumChoice
    {
        public string Name { get; set; }
        public List<string> Words { get; set; }
        public Enum Value { get; set; }
    }
}