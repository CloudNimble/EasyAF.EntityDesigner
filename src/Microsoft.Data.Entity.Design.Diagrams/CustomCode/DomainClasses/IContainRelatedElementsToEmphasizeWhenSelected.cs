// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Modeling;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Diagrams.DomainClasses
{
    internal interface IContainRelatedElementsToEmphasizeWhenSelected
    {
        IEnumerable<ModelElement> RelatedElementsToEmphasizeOnSelected { get; }
    }
}
