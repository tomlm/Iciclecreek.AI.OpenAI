namespace Iciclecreek.OpenAI.Recognizer
{
    public class FunctionDefinition
    {
        public FunctionDefinition() { }

        public FunctionDefinition(string signature, string description, params string[] examples)
        {
            Signature = signature;
            Description = description;
            Examples = examples?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Signature for the intent (Example: Add(x, y) )
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Description of the intent (Example: Adds two numbers)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// (OPTIONAL) Examples of intent
        /// </summary>
        public List<string> Examples { get; set; } = new List<string>();
    }
}
