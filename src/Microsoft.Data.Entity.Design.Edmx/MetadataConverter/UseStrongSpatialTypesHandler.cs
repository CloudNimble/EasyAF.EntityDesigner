// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.Model
{
    /// <summary>
    ///     Handles UseStrongSpatialTypes attribute conversion for EDMX files.
    ///     Only supports UPGRADE to Version3 (EF6).
    /// </summary>
    internal sealed class UseStrongSpatialTypesHandler : MetadataConverterHandler
    {
        private readonly Version _targetSchemaVersion;

        internal UseStrongSpatialTypesHandler(Version targetSchemaVersion)
        {
            Debug.Assert(
                targetSchemaVersion == EntityFrameworkVersion.Version3,
                "UseStrongSpatialTypesHandler only supports upgrade to Version3");
            _targetSchemaVersion = targetSchemaVersion;
        }

        /// <summary>
        ///     Ensure UseStrongSpatialTypes="false" is set on the CSDL Schema Element.
        ///     Since we only target Version3 (EF6), this feature is always enabled.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected override XmlDocument DoHandleConversion(XmlDocument doc)
        {
            var nsmgr = SchemaManager.GetEdmxNamespaceManager(doc.NameTable, _targetSchemaVersion);
            var annotationNamespace = SchemaManager.GetAnnotationNamespaceName();
            XmlElement csdlSchemaElement = (XmlElement)doc.SelectSingleNode("/edmx:Edmx/edmx:Runtime/edmx:ConceptualModels/csdl:Schema", nsmgr);
            if (csdlSchemaElement != null)
            {
                var useStrongSpatialTypesAttr =
                    csdlSchemaElement.Attributes[UseStrongSpatialTypesDefaultableValue.AttributeUseStrongSpatialTypes, annotationNamespace];

                // UseStrongSpatialTypes is always supported in Version3 (EF6)
                // Add UseStrongSpatialTypes="false" if it is not present
                if (useStrongSpatialTypesAttr == null)
                {
                    useStrongSpatialTypesAttr = doc.CreateAttribute(
                        "annotation", UseStrongSpatialTypesDefaultableValue.AttributeUseStrongSpatialTypes, annotationNamespace);
                    useStrongSpatialTypesAttr.Value = "false";
                    csdlSchemaElement.Attributes.Append(useStrongSpatialTypesAttr);

                    // setting the xmlns:annotation attribute explicitly will ensure that the XmlReader does not come up
                    // with an auto-generated namespace prefix which may cause an NRE in the XmlEditor leading to a VS crash
                    var annotationXmlnsAttr = doc.CreateAttribute("xmlns", "annotation", "http://www.w3.org/2000/xmlns/");
                    annotationXmlnsAttr.Value = annotationNamespace;
                    csdlSchemaElement.SetAttributeNode(annotationXmlnsAttr);
                }
            }

            return doc;
        }
    }
}
