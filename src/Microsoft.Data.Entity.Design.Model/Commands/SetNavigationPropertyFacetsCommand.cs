// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    /// <summary>
    ///     Navigation Properties have some optional facets that can be set and you use this command to set them.
    /// </summary>
    internal class SetNavigationPropertyFacetsCommand : Command
    {
        private readonly NavigationProperty _property;
        private readonly string _getterAccessModifier;
        private readonly string _setterAccessModifier;

        /// <summary>
        ///     Sets facets on the passed in property
        /// </summary>
        /// <param name="property">Must be a non-null conceptual property</param>
        /// <param name="getterAccessModifier">Optional facet</param>
        /// <param name="setterAccessModifier">Optional facet</param>
        internal SetNavigationPropertyFacetsCommand(NavigationProperty property, string getterAccessModifier, string setterAccessModifier)
        {
            CommandValidation.ValidateNavigationProperty(property);
            _property = property;
            _getterAccessModifier = getterAccessModifier;
            _setterAccessModifier = setterAccessModifier;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            Debug.Assert(_property != null, "InvokeInternal is called when _property is null.");
            if (_property == null)
            {
                throw new InvalidOperationException("InvokeInternal is called when _property is null.");
            }

            if (!String.IsNullOrEmpty(_getterAccessModifier))
            {
                _property.Getter.Value = _getterAccessModifier;
            }

            if (!String.IsNullOrEmpty(_setterAccessModifier))
            {
                _property.Setter.Value = _setterAccessModifier;
            }
        }
    }
}
