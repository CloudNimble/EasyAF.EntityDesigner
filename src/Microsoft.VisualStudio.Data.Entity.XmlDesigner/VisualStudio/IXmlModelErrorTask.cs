// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio
{
    internal interface IXmlModelErrorTask
    {
        IServiceProvider ServiceProvider { get; }
        uint ItemID { get; }
    }
}
