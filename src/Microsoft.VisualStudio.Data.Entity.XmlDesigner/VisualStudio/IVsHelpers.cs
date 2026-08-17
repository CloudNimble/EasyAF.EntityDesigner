// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    interface IVsHelpers
    {
        object GetDocData(IServiceProvider site, string documentPath);
    }
}
