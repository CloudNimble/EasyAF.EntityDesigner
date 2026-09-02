// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    internal interface IMouseHookClient
    {
        // return true if the click is handled, false
        // to pass it on
        bool OnClickHooked();
    }

}
