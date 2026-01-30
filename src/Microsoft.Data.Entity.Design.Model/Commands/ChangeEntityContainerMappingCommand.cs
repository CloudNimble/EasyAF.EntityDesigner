// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Mapping;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class ChangeEntityContainerMappingCommand : Command
    {
        private readonly EntityContainerMapping _ecm;
        private readonly bool _generateUpdateViews;

        /// <summary>
        ///     This method lets you change whether an EntityContainerMapping should generate update views or not (no means it is read-only).
        /// </summary>
        /// <param name="ecm">Must point to a valid EntityContainerMapping</param>
        internal ChangeEntityContainerMappingCommand(EntityContainerMapping ecm, bool generateUpdateViews)
        {
            CommandValidation.ValidateEntityContainerMapping(ecm);

            _ecm = ecm;
            _generateUpdateViews = generateUpdateViews;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // safety check, this should never be hit
            Debug.Assert(cpc != null, "InvokeInternal is called when EntityContainerMapping is null.");
            if (_ecm == null)
            {
                throw new InvalidOperationException("InvokeInternal is called when EntityContainerMapping is null.");
            }

            _ecm.GenerateUpdateViews.Value = _generateUpdateViews;
            XmlModelHelper.NormalizeAndResolve(_ecm);
        }
    }
}
