// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Model.XLinqAnnotations
{
    internal class ModelAnnotation
    {
        internal long NextIdentity { get; set; }

        internal static long GetNextIdentity(XObject element)
        {
            var annotation = element.Annotation<ModelAnnotation>();
            if (annotation == null)
            {
                annotation = new ModelAnnotation();
                element.AddAnnotation(annotation);
            }

            var nextIdentity = annotation.NextIdentity;
            annotation.NextIdentity = ++nextIdentity;

            return nextIdentity;
        }
    }
}
