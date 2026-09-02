// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class ElementTextRange
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int OpenStartLine { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int OpenStartColumn { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int OpenEndLine { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int OpenEndColumn { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int CloseEndLine { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public int CloseEndColumn { get; set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("OpenStartLine=");
            buffer.Append(OpenStartLine);
            buffer.Append(",OpenStartColumn=");
            buffer.Append(OpenStartColumn);
            buffer.Append(",OpenEndLine=");
            buffer.Append(OpenEndLine);
            buffer.Append(",OpenEndColumn=");
            buffer.Append(OpenEndColumn);
            buffer.Append(",CloseEndLine=");
            buffer.Append(CloseEndLine);
            buffer.Append(",CloseEndColumn=");
            buffer.Append(CloseEndColumn);
            return buffer.ToString();
        }
    }

}
