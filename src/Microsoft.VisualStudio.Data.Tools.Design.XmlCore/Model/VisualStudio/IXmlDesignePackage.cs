// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio
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

    internal class SimpleDelegateClass
    {
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
            Justification = "This name is meaningful in this context")]
        public delegate void SimpleDelegate();
    }
}
