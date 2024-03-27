using Azure.AI.OpenAI;
using Iciclecreek.OpenAI.Recognizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iciclecreek.OpenAI.Recognizer.Tests
{
    internal class MathFunctionRecognizer : FunctionsRecognizer
    {
        public MathFunctionRecognizer(OpenAIClient client) : base(client)
        {
            this.Functions.Add(new FunctionDefinition("Add(x,y)", "Add two numbers", "what is 5 + 3? => Add(5,3)"));
            this.Functions.Add(new FunctionDefinition("Subtract(x,y)", "Subtract two numbers", "remove 10 from 20 => Subtract(20,10)"));
            this.Functions.Add(new FunctionDefinition("Multiply(fact1,fact2)", "Multiply two numbers", "32x16 => Multiply(32,16)"));
            this.Functions.Add(new FunctionDefinition("Divide(num,denom)", "Divide two numbers", "divide 100 x 4 => Divide(100,4)"));
        }
    }
}
