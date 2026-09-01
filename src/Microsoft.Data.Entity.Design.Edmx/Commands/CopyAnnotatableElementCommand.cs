// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal abstract class CopyAnnotatableElementCommand : Command
    {
        protected virtual void AddAnnotations(AnnotatableElementClipboardFormat clipboardFormat, EFElement element)
        {
            foreach (var t in clipboardFormat.AdditionalAttributes)
            {
                var name = t.Item1;
                var value = t.Item2;
                XName xn = XName.Get(name);
                XAttribute xa = new XAttribute(xn, value);
                element.XElement.Add(xa);
            }

            foreach (var s in clipboardFormat.AdditionalElements)
            {
                try
                {
                    XDocument d = XDocument.Parse(s, LoadOptions.PreserveWhitespace);
                    var xe = d.Root;
                    xe.Remove();
                    element.XElement.Add(xe);
                }
                catch (XmlException)
                {
                    // ignore an XmlException.  There was probalby a problem parsing.
                }
            }
        }
    }
}
