// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class DeleteComplexPropertyCommand : DeleteEFElementCommand
    {
        /// <summary>
        ///     Deletes the passed in ComplexProperty
        /// </summary>
        /// <param name="sp"></param>
        internal DeleteComplexPropertyCommand(ComplexProperty cp)
            : base(cp)
        {
            CommandValidation.ValidateComplexProperty(cp);
        }

        protected ComplexProperty ComplexProperty
        {
            get
            {
                ComplexProperty elem = EFElement as ComplexProperty;
                Debug.Assert(elem != null, "underlying element does not exist or is not a ComplexProperty");
                if (elem == null)
                {
                    throw new InvalidModelItemException();
                }
                return elem;
            }
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            if (ComplexProperty.Parent is ComplexProperty complexProperty
                && complexProperty.ScalarProperties().Count == 0
                && complexProperty.ComplexProperties().Count == 1)
            {
                // if we are about to remove the last item from this ComplexProperty, just remove it
                Debug.Assert(
                    complexProperty.ComplexProperties()[0] == ComplexProperty,
                    "complexProperty.ComplexProperties()[0] should be the same as this.ComplexProperty");
                DeleteInTransaction(cpc, complexProperty);
            }
            else
            {
                base.InvokeInternal(cpc);
            }
        }
    }
}
