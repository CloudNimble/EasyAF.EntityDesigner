// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal class LazyLoadingDefaultableValue : DefaultableValue<bool>
    {
        internal const string AttributeLazyLoadingEnabled = "LazyLoadingEnabled";

        internal LazyLoadingDefaultableValue(EFElement parent)
            : base(parent, AttributeLazyLoadingEnabled, SchemaManager.GetAnnotationNamespaceName())
        {
        }

        internal override string AttributeName
        {
            get { return AttributeLazyLoadingEnabled; }
        }

        /// <summary>
        ///     The non-existence of the attribute should be interpreted as false.
        /// </summary>
        public override bool DefaultValue
        {
            get { return false; }
        }

        internal override bool ValidateValueAgainstSchema()
        {
            if (EdmFeatureManager.GetLazyLoadingFeatureState(Parent.Artifact.SchemaVersion).IsEnabled())
            {
                return true;
            }
            return false;
        }
    }
}
