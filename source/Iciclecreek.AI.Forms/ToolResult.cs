using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Iciclecreek.AI.Forms
{
    public class ToolResult
    {
        public ToolResult(bool success, string summary, [CallerMemberName] string action = null)
        {
            Action = action;
            Succeeded = success;
            Description = summary;
        }

        [Description("The action name")]
        public string Action { get; set; }

        [Description("Description of the action taken")]
        public string Description { get; set; }

        [Description("Whether the action was valid")]
        public bool Succeeded { get; set; }

        [Description("the value")]
        public object? Value { get; set; }

        [Description("Errors with the data")]
        public List<ValidationResult> Errors { get; set; }

        public static ToolResult Success(string message, [CallerMemberName] string action = null) => new ToolResult(true, message, action);

        public static ToolResult Failed(string message, [CallerMemberName] string action = null) => new ToolResult(false, message, action);

        public static ToolResult Failed(string message, IEnumerable<ValidationResult> errors, [CallerMemberName] string action = null)
            => new ToolResult(false, message, action)
            {
                Errors = errors.ToList()
            };
    }
}