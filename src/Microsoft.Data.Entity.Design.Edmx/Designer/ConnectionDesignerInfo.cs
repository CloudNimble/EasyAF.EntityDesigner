// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{
    internal class ConnectionDesignerInfo : DesignerInfo
    {
        // DesignerProperty objects
        private DesignerProperty _propMetadataArtifactProcessing;
        internal static readonly string ElementName = "Connection";

        // XElement name attribute values
        internal static readonly string AttributeMetadataArtifactProcessing = "MetadataArtifactProcessing";

        // XElement value attribute values
        internal static readonly string MAP_CopyToOutputDirectory = "CopyToOutputDirectory";
        internal static readonly string MAP_EmbedInOutputAssembly = "EmbedInOutputAssembly";

        internal ConnectionDesignerInfo(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        internal override string EFTypeName
        {
            get { return ElementName; }
        }

        internal DesignerProperty MetadataArtifactProcessingProperty
        {
            get
            {
                _propMetadataArtifactProcessing = SafeGetDesignerProperty(
                    _propMetadataArtifactProcessing, AttributeMetadataArtifactProcessing);
                return _propMetadataArtifactProcessing;
            }
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            return base.ParseSingleElement(unprocessedElements, elem);
        }
    }
}
