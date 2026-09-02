// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class CommonLocDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string name;

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayNameAttribute"]/*' />
        public CommonLocDisplayNameAttribute(string name)
        {
            this.name = name;
        }

        protected virtual ResourceManager ResourceManager
        {
            get { return global::Microsoft.Data.Entity.Design.XmlEngine.XmlEngineResources.ResourceManager; }
        }

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayName"]/*' />
        public override string DisplayName
        {
            get
            {
                var result = ResourceManager.GetString(name, CultureInfo.CurrentUICulture);
                if (result == null)
                {
                    Debug.Assert(false, "String resource '" + name + "' is missing");
                    result = name;
                }
                return result;
            }
        }
    }

}
