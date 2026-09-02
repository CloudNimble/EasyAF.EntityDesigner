// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    /// <summary>
    ///     Used to ensure that an EntityType's EntitySet GetterAccess attribute is updated consistently
    ///     when the EntityType's TypeAccess attribute is updated. Specifically if the EntityType's TypeAccess
    ///     attribute is set to Internal then it is an error for the EntitySet's GetterAccess to be Public
    ///     or Protected - instead it must be updated to Internal.
    /// </summary>
    internal class UpdateEntityTypeTypeAccessCommand : Command
    {
        private readonly DefaultableValue<string> _typeAccess;
        private readonly string _newValue;

        internal UpdateEntityTypeTypeAccessCommand(DefaultableValue<string> typeAccess, string value)
        {
            _typeAccess = typeAccess;
            _newValue = value;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            UpdateDefaultableValueCommand<string> cmd = new UpdateDefaultableValueCommand<string>(_typeAccess, _newValue);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);

            if (ModelConstants.CodeGenerationAccessInternal.Equals(_newValue, StringComparison.Ordinal))
            {
                ConceptualEntityType cet = _typeAccess.GetParentOfType(typeof(ConceptualEntityType)) as ConceptualEntityType;
                Debug.Assert(null != cet, "parent of _typeAccess should be of type " + typeof(ConceptualEntityType).FullName);
                if (null != cet)
                {
                    // Note: it is valid for the EntitySet to be null
                    if (cet.EntitySet is ConceptualEntitySet ces)
                    {
                        var entitySetGetterAccess = ces.GetterAccess.Value;
                        if (ModelConstants.CodeGenerationAccessPublic.Equals(entitySetGetterAccess)
                            || ModelConstants.CodeGenerationAccessProtected.Equals(entitySetGetterAccess))
                        {
                            // new value is Internal and EntitySet's existing value is Public or Protected
                            // so need to also update the GetterAccess attribute on the EntitySet
                            // (otherwise will get runtime error 6036)
                            UpdateDefaultableValueCommand<string> cmd2 = new UpdateDefaultableValueCommand<string>(ces.GetterAccess, _newValue);
                            CommandProcessor.InvokeSingleCommand(cpc, cmd2);
                        }
                    }
                }
            }
        }
    }
}
