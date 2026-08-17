// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    internal class NormalizingVisitor : MissedItemCollectingVisitor
    {
        internal override void Visit(IVisitable visitable)
        {
            if (visitable is not EFContainer item)
            {
                return;
            }

            if (item.State == EFElementState.None
                ||
                item.State == EFElementState.Normalized
                ||
                item.State == EFElementState.Resolved)
            {
                return;
            }

            try
            {
                item.Normalize();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture, "Error normalizing item {0}",
                        item.SemanticName
                        ),
                    e);
            }

            if (item.State != EFElementState.Normalized)
            {
                // only do this for elements
                if (item is EFElement efElement)
                {
                    _missedCount++;
                    if (!_missed.Contains(efElement))
                    {
                        _missed.Add(efElement);
                    }
                }
            }
        }
    }
}
