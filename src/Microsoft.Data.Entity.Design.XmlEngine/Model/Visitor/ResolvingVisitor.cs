// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Common;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Tools.XmlDesignerBase;

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    internal class ResolvingVisitor : MissedItemCollectingVisitor
    {
        private readonly EFArtifactSet _artifactSet;

        internal ResolvingVisitor(EFArtifactSet artifactSet)
        {
            _artifactSet = artifactSet;
        }

        internal override void Visit(IVisitable visitable)
        {
            if (visitable is not EFContainer item)
            {
                return;
            }

            if (item.State != EFElementState.Normalized)
            {
                return;
            }

            try
            {
                item.Resolve(_artifactSet);
            }
            catch (Exception e)
            {
                // reset element state
                item.State = EFElementState.ResolveAttempted;

                string name = null;
                if (item is EFNameableItem nameable)
                {
                    name = nameable.LocalName.Value;
                }
                else
                {
                    if (item.XObject is XElement element)
                    {
                        name = element.Name.LocalName;
                    }
                    else
                    {
                        name = item.SemanticName;
                    }
                }
                var message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorResolvingItem, name, e.Message);
                ErrorInfo errorInfo = new ErrorInfo(
                    ErrorInfo.Severity.ERROR, message, item, ErrorCodes.FATAL_RESOLVE_ERROR, ErrorClass.ResolveError);
                _artifactSet.AddError(errorInfo);
            }

            if (item.State != EFElementState.Resolved)
            {
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
