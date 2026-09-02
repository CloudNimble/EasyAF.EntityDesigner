// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public interface IXmlNodeNameChange : IXmlChange
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        XName OldName { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        XName NewName { get; }
    }

}
