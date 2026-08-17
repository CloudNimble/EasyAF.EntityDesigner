// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class SetRowsAffectedParameterCommand : Command
    {
        private readonly ModificationFunction _modificationFunction;
        private readonly Parameter _param;

        internal SetRowsAffectedParameterCommand(ModificationFunction modificationFunction, Parameter param)
        {
            Debug.Assert(
                modificationFunction != null,
                typeof(SetRowsAffectedParameterCommand).Name + ": constructor cannot operate on null ModificationFunction");

            _modificationFunction = modificationFunction;
            _param = param;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // validate parameter
            if (null != _param)
            {
                // check that the Parameter is suitable for use as a RowsAffectedParameter
                if (!_param.CanBeUsedAsRowsAffectedParameter())
                {
                    var errMsg = string.Format(
                        CultureInfo.CurrentCulture, EdmxResources.SetRowsAffectedParameterErrorMessage_CannotUse, _param.NormalizedNameExternal,
                        _param.InOut.ToString(), _param.Type.Value);
                    throw new CommandValidationFailedException(errMsg);
                }
            }

            // if _param is null this will delete the RowsAffectedParameter (which is as expected)
            _modificationFunction.RowsAffectedParameter.SetRefName(_param);

            XmlModelHelper.NormalizeAndResolve(_modificationFunction);
        }
    }
}
