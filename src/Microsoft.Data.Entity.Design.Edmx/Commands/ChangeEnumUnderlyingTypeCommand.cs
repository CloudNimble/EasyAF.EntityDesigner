// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class ChangeEnumUnderlyingTypeCommand : Command
    {
        public EnumType EnumType { get; set; }
        internal string NewTypeName { get; set; }

        internal ChangeEnumUnderlyingTypeCommand(EnumType enumType, string newType)
        {
            CommandValidation.ValidateEnumType(enumType);
            ValidateString(newType);

            EnumType = enumType;
            NewTypeName = newType;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            if (ModelHelper.UnderlyingEnumTypes.Count(t => String.CompareOrdinal(t.Name, NewTypeName) == 0) == 0)
            {
                throw new CommandValidationFailedException(
                    String.Format(CultureInfo.CurrentCulture, EdmxResources.Incorrect_Enum_UnderlyingType, NewTypeName));
            }
            EnumType.UnderlyingType.Value = NewTypeName;
        }
    }
}
