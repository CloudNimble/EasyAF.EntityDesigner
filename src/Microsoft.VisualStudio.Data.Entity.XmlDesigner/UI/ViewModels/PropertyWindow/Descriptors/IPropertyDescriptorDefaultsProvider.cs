// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    /// <summary>
    ///     Used to announce that a component can provide default values for the
    ///     property descriptors it contains
    /// </summary>
    internal interface IPropertyDescriptorDefaultsProvider
    {
        object GetDescriptorDefaultValue(string propertyDescriptorMethodName);
    }

}
