// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    /// <summary>
    ///     This class will traverse from the starting node, visiting all children, and accumulate
    ///     all ItemBindings that point to the child, or ItemBindings that point to any duplicate symbols
    ///     of a child.
    /// </summary>
    internal class AntiDependencyCollectorVisitor : Visitor
    {
        private readonly HashSet<ItemBinding> _antiDeps = [];

        internal HashSet<ItemBinding> AntiDependencyBindings
        {
            get { return _antiDeps; }
        }

        internal override void Visit(IVisitable visitable)
        {
            if (visitable is EFNameableItem ni)
            {
                // if this is a nameable item, include any deps for any other elements that have the same normalized name
                foreach (EFObject efobj in ni.Artifact.ArtifactSet.GetSymbolList(ni.NormalizedName))
                {
                    foreach (var antiDep in efobj.GetDependentBindings())
                    {
                        _antiDeps.Add(antiDep);
                    }
                }
            }
            else
            {
                EFObject efobj = visitable as EFObject;
                foreach (var antiDep in efobj.GetDependentBindings())
                {
                    _antiDeps.Add(antiDep);
                }
            }
        }
    }
}
