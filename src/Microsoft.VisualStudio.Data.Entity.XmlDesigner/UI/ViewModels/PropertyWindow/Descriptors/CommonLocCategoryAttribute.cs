// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    [AttributeUsage(AttributeTargets.All)]
    internal class CommonLocCategoryAttribute : CategoryAttribute
    {
        public CommonLocCategoryAttribute(string category)
            : base(category)
        {
        }

        protected virtual ResourceManager ResourceManager
        {
            get { return global::Microsoft.Data.Entity.Design.XmlEngine.XmlEngineResources.ResourceManager; }
        }

        protected override string GetLocalizedString(string value)
        {
            var result = ResourceManager.GetString(value, CultureInfo.CurrentUICulture);
            if (result == null)
            {
                Debug.Assert(false, "String resource '" + value + "' is missing");
                result = value;
            }
            return result;
        }
    }

}
