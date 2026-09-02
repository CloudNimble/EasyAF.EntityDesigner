// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Clipboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    [Serializable]
    internal abstract class AnnotatableElementClipboardFormat : EFElementClipboardFormat
    {
        private readonly List<String> _additionalElements = [];
        private readonly List<Tuple<String, String>> _additionalAttributes = [];

        internal AnnotatableElementClipboardFormat(EFElement efElement)
            : base(efElement)
        {
            // scan through the XML and identify any "extra" attributes we want to include in the copy
            foreach (var xo in ModelHelper.GetStructuredAnnotationsForElement(efElement))
            {
                if (xo is XAttribute xa)
                {
                    Tuple<string, string> t = new Tuple<string, string>(xa.Name.ToString(), xa.Value);
                    _additionalAttributes.Add(t);
                }
                else if (xo is XElement xe)
                {
                    _additionalElements.Add(xe.ToString(SaveOptions.None));
                }
                else
                {
                    Debug.Fail("unexepected type of XObject returned from GetAnnotationsForElement()");
                }
            }
        }

        internal IEnumerable<string> AdditionalElements
        {
            get { return _additionalElements; }
        }

        internal IEnumerable<Tuple<String, String>> AdditionalAttributes
        {
            get { return _additionalAttributes; }
        }
    }
}
