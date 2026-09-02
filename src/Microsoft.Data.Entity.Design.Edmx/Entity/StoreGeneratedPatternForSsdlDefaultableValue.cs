// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{

    internal class StoreGeneratedPatternForSsdlDefaultableValue : DefaultableValue<string>
    {
        // below must be const (rather than static readonly) because referenced in an attribute in PropertyClipboardFormat
        internal const string AttributeStoreGeneratedPattern = "StoreGeneratedPattern";

        internal StoreGeneratedPatternForSsdlDefaultableValue(EFElement parent)
            : base(parent, AttributeStoreGeneratedPattern)
        {
        }

        internal override string AttributeName
        {
            get { return AttributeStoreGeneratedPattern; }
        }

        public override string DefaultValue
        {
            get { return ModelConstants.StoreGeneratedPattern_None; }
        }
    }

}
