using System.Collections.Generic;

namespace Iciclecreek.AI.OpenAI.FormFill
{
    public enum SemanticActionResultType
    {
        NoAction,
        Success,
        Failed
    }

    public class SemanticActionResult
    {
        public SemanticActionResult(SemanticAction action, SemanticActionResultType resultType = SemanticActionResultType.Success)
        {
            Action = action;
            Result = resultType;
        }

        public SemanticAction Action { get; set; }

        public SemanticActionResultType Result { get; set; }
    
        public string Message { get; set; }
    }
}