// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    [AttributeUsage(AttributeTargets.All)]
    internal class CommonLocDescriptionAttribute : DescriptionAttribute
    {
        private bool replaced;

        public CommonLocDescriptionAttribute(string description)
            : base(description)
        {
        }

        protected virtual ResourceManager ResourceManager
        {
            get { return global::Microsoft.Data.Entity.Design.XmlEngine.XmlEngineResources.ResourceManager; }
        }

        public override string Description
        {
            get
            {
                if (!replaced)
                {
                    replaced = true;
                    var result = ResourceManager.GetString(base.Description, CultureInfo.CurrentUICulture);
                    if (result == null)
                    {
                        Debug.Assert(false, "String resource '" + base.Description + "' is missing");
                        result = base.Description;
                    }
                    DescriptionValue = result;
                }
                return base.Description;
            }
        }
    }

}
