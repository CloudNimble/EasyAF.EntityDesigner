// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;
using System;
using ModelChangeEventArgs = Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package.ModelChangeEventArgs;

namespace Microsoft.Data.Entity.Tests.Shared.EFDesigner
{
    internal class MockPackage : IEdmPackage, IDisposable
    {
        private readonly EntityDesignModelManager _manager;

        internal MockPackage(EntityDesignModelManager manager)
        {
            _manager = manager;
            PackageManager.Package = this;
        }

        public IEntityDesignCommandSet CommandSet
        {
            get { return null; }
        }

        public ExplorerWindow ExplorerWindow
        {
            get { return null; }
        }

        public MappingDetailsWindow MappingDetailsWindow
        {
            get { return null; }
        }

        public DocumentFrameMgr DocumentFrameMgr
        {
            get { return null; }
        }

        public ConnectionManager ConnectionManager
        {
            get { return null; }
        }

        public AggregateProjectTypeGuidCache AggregateProjectTypeGuidCache
        {
            get { return null; }
        }

        public ModelGenErrorCache ModelGenErrorCache
        {
            get { return null; }
        }

        public ModelChangeEventListener ModelChangeEventListener
        {
            get { return null; }
        }

        public EntityDesignModelManager ModelManager
        {
            get { return _manager; }
        }

        ModelManager IXmlDesignerPackage.ModelManager
        {
            get { return _manager; }
        }

        public string GetResourceString(string resourceName)
        {
            return string.Empty;
        }

        public event ModelChangeEventHandler FileNameChanged;

        public void OnFileNameChanged(string oldFileName, string newFileName)
        {
            var args = new ModelChangeEventArgs();
            args.OldFileName = oldFileName;
            args.NewFileName = newFileName;
            FileNameChanged?.Invoke(this, args);
        }

        public bool IsBuildingFromCommandLine
        {
            get
            {
                // Command-line builds exclusively instantiate packages, so if we are instantiating this
                // we're not building from the command line
                return false;
            }
        }

        public void SetToolWindowCmdsEnabled(bool enabled)
        {
        }

        public object GetService(Type serviceType)
        {
            return null;
        }

        public bool IsForegroundThread
        {
            get { return true; }
        }

        public void InvokeOnForeground(SimpleDelegateClass.SimpleDelegate simpleDelegate)
        {
        }

        public void Dispose()
        {
            if (_manager != null)
            {
                _manager.Dispose();
            }
            PackageManager.Package = null;
        }
    }
}
