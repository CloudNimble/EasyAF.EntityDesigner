// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{
    /// <summary>
    ///     This class will provide an XML model over an in-memory string.
    ///     The URI doesn't need to correspond to a file on disk.
    /// </summary>
    internal class InMemoryXmlModelProvider : VanillaXmlModelProvider
    {
        private readonly Uri _inputUri;
        private readonly string _inputXml;

        internal InMemoryXmlModelProvider(Uri inputUri, string inputXml)
        {
            _inputUri = inputUri;
            _inputXml = inputXml;
        }

        protected override XDocument Build(Uri uri)
        {
            if (uri != _inputUri)
            {
                throw new ArgumentException("specified URI does not match the URI used to create this model provider");
            }
            AnnotatedTreeBuilder builder = new AnnotatedTreeBuilder();
            using (XmlReader reader = XmlReader.Create(new StringReader(_inputXml)))
            {
                var doc = builder.Build(reader);
                return doc;
            }
        }
    }
}
