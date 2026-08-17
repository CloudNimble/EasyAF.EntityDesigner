// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    /// <summary>
    ///     Strongly/uniquely-typed command associated with changing the property's SetterAccess
    /// </summary>
    internal class ChangePropertySetterAccessCommand : UpdateDefaultableValueCommand<string>
    {
        public Property Property { get; set; }

        internal string SetterAccess
        {
            get { return Value; }
        }

        public ChangePropertySetterAccessCommand()
            : base(null, null)
        {
        }

        internal ChangePropertySetterAccessCommand(Property property, string value)
            : base(property.Setter, value)
        {
            Property = property;
        }
    }
}
