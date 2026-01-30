using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    [Serializable]
    internal class CodeFirstModelGenerationException : Exception
    {
        public CodeFirstModelGenerationException(string message, Exception innerException)
            : base(message, innerException)
        {
            Debug.Assert(!string.IsNullOrEmpty(message), "message is null or empty.");
            Debug.Assert(innerException != null, "innerException is null.");
        }

        protected CodeFirstModelGenerationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
