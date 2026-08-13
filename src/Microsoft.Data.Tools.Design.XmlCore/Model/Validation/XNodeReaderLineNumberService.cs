// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.Tools.XmlDesignerBase.Model;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    internal class XNodeReaderLineNumberService : XObjectLineNumberService, IXmlLineInfo
    {
        private readonly XmlReader _xmlReader;
        private readonly Uri _uri;
        private static readonly FieldInfo _sourceFieldInfo;
        private static readonly FieldInfo _parentFieldInfo;

        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        static XNodeReaderLineNumberService()
        {
            var xnodeReaderType = Assembly.GetAssembly(typeof(XObject)).GetType("System.Xml.Linq.XNodeReader");
            if (xnodeReaderType is null)
            {
                return;
            }

#if NETSTANDARD2_0
            // A .NET Standard build can be loaded by either runtime, so the name cannot be decided here: .NET
            // Framework calls these fields "source" and "parent", while .NET Core and later prefix them with an
            // underscore. Probe the underscore form first, since that is the runtime still being maintained.
            _sourceFieldInfo = xnodeReaderType.GetField("_source", FieldFlags) ?? xnodeReaderType.GetField("source", FieldFlags);
            _parentFieldInfo = xnodeReaderType.GetField("_parent", FieldFlags) ?? xnodeReaderType.GetField("parent", FieldFlags);
#else
            _sourceFieldInfo = xnodeReaderType.GetField("_source", FieldFlags);
            _parentFieldInfo = xnodeReaderType.GetField("_parent", FieldFlags);
#endif
        }

        internal XNodeReaderLineNumberService(XmlModelProvider xmlModelProvider, XmlReader xmlReader, Uri uri)
            : base(xmlModelProvider)
        {
            Debug.Assert(xmlReader != null, "xmlReader != null");
            Debug.Assert(uri != null, "uri != null");

            Debug.Assert(
                xmlReader.GetType().Name == "XNodeReader",
                "Unexpected type for XmlReader.  Expected reader to be System.Xml.Linq.XNodeReader");

            Debug.Assert(_sourceFieldInfo != null, "_sourceFieldInfo != null");
            Debug.Assert(_parentFieldInfo != null, "_parentFieldInfo != null");

            _xmlReader = xmlReader;
            _uri = uri;
        }

        public int LineNumber
        {
            get
            {
                var xobject = GetSourceFieldValue();
                return GetLineNumber(xobject, _uri);
            }
        }

        public int LinePosition
        {
            get
            {
                var xobject = GetSourceFieldValue();
                return GetColumnNumber(xobject, _uri);
            }
        }

        // We have to get this via reflection since there is no other way to 
        // access the XNode behind the XNodeReader used by XLinq.
        private XObject GetSourceFieldValue()
        {
            XObject xobject = null;

            // in theory this should never happen but since we are using reflection to get these 
            // fields it's safer to return false if we could not find any of these fileds
            if (_sourceFieldInfo != null
                && _parentFieldInfo != null)
            {
                var source = _sourceFieldInfo.GetValue(_xmlReader);
                Debug.Assert(source != null, "Unexpected null value for source field on specified reader");
                xobject = source as XObject;
                if (xobject == null)
                {
                    if (source is string)
                    {
                        // this is a text element, so the source field is of type string.  We can access the 
                        // parent fields LastObject and get the XObject associated with the xml text.  Again,
                        // we have to do this via reflection. 
                        var parent = _parentFieldInfo.GetValue(_xmlReader);
                        Debug.Assert(parent != null, "Unexpected null value for parent field on specified reader");
                        XElement xe = parent as XElement;
                        Debug.Assert(xe != null, "parent value is not an XElement");
                        if (xe != null)
                        {
                            xobject = xe.LastNode;
                        }
                    }
                }
            }
            Debug.Assert(xobject != null, "Unexpected type for source value.  It is not an XObject");
            return xobject;
        }

        public bool HasLineInfo()
        {
            // in theory this should never happen but since we are using reflection to get these 
            // fields it's safer to return false if we could not find any of these fileds
            return _sourceFieldInfo != null && _parentFieldInfo != null;
        }
    }
}
