using Azure.AI.OpenAI;

namespace Iciclecreek.AI.OpenAI.Tests
{
    internal class MathFunctionRecognizer : SemanticActionRecognizer
    {
        public MathFunctionRecognizer(OpenAIClient client) : base(client)
        {
            this.Actions.Add(new SemanticActionDefinition("Add", "Add two numbers").AddArgument("number").AddArgument("number").AddExample("what is 5 + 3?","5","3"));
            this.Actions.Add(new SemanticActionDefinition("Subtract", "Subtract two numbers").AddArgument("number").AddArgument("number").AddExample("remove 10 from 20","20","10"));
            this.Actions.Add(new SemanticActionDefinition("Multiply","Multiply two numbers").AddArgument("number").AddArgument("number").AddExample("32x16","32","16"));
            this.Actions.Add(new SemanticActionDefinition("Divide","Divde two numbers").AddArgument("number").AddArgument("number").AddExample("divide 100 x 4","100","4"));
        }
    }
}
