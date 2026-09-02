// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal class SimpleDelegateClass
    {
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
            Justification = "This name is meaningful in this context")]
        public delegate void SimpleDelegate();
    }

}
