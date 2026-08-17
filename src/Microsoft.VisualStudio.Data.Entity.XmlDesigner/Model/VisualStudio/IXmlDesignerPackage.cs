// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal interface IXmlDesignerPackage : IServiceProvider
    {
        void InvokeOnForeground(SimpleDelegateClass.SimpleDelegate simpleDelegate);
        bool IsForegroundThread { get; }

        DocumentFrameMgr DocumentFrameMgr { get; }
        ModelManager ModelManager { get; }
        event ModelChangeEventHandler FileNameChanged;
        string GetResourceString(string resourceName);
    }

}
