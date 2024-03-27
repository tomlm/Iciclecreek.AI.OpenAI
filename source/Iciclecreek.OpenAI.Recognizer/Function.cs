
namespace Iciclecreek.OpenAI.Recognizer
{

    public class Function
    {
        public string Name { get; set; }

        public List<object> Args { get; set; } = new List<object>();

        public override string ToString()
        {
            return $"{Name}({string.Join(',', Args)})";
        }
    }
}
