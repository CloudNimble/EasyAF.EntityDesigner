// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    internal static class Extensions
    {
        public static TextRange GetTextRange(this XObject attribute)
        {
            var saa = attribute.Annotation<XmlAttributeAnnotation>();
            if (saa == null)
            {
                return null;
            }
            return saa.TextRange;
        }

        public static void SetTextRange(this XObject attribute, TextRange textRange)
        {
            var saa = attribute.Annotation<XmlAttributeAnnotation>();
            if (saa == null)
            {
                saa = new XmlAttributeAnnotation();
                attribute.AddAnnotation(saa);
            }
            saa.TextRange = textRange;
        }

        private class XmlFileAnnotation
        {
            internal ElementTextRange TextRange;
        }

        public static ElementTextRange GetTextRange(this XElement element)
        {
            var sfa = element.Annotation<XmlFileAnnotation>();
            if (sfa == null)
            {
                return null;
            }
            return sfa.TextRange;
        }

        public static void SetTextRange(this XElement element, ElementTextRange textRange)
        {
            var sfa = element.Annotation<XmlFileAnnotation>();
            if (sfa == null)
            {
                sfa = new XmlFileAnnotation();
                element.AddAnnotation(sfa);
            }
            sfa.TextRange = textRange;
        }

        public static void EnsureAnnotation(this XElement element)
        {
            var sfa = element.Annotation<XmlFileAnnotation>();
            if (sfa == null)
            {
                sfa = new XmlFileAnnotation();
                element.AddAnnotation(sfa);
            }
        }
    }

}
