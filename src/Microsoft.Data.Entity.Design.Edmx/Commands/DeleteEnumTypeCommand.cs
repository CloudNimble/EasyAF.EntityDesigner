// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class DeleteEnumTypeCommand : DeleteEFElementCommand
    {
        private readonly List<ConceptualProperty> _propertiesToResolve = [];

        /// <summary>
        ///     Deletes the passed in EnumType
        /// </summary>
        /// <param name="complexType"></param>
        internal DeleteEnumTypeCommand(EnumType enumType)
            : base(enumType)
        {
            CommandValidation.ValidateEnumType(enumType);
        }

        /// <summary>
        ///     We override this method because we need to do some extra things before
        ///     the normal PreInvoke gets called and our antiDeps are removed
        /// </summary>
        /// <param name="cpc"></param>
        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            // Unbind all ConceptualProperty that reference the enum type so they will not get deleted.
            foreach (var property in EFElement.GetAntiDependenciesOfType<ConceptualProperty>())
            {
                property.UnbindEnumType();
                ;
                _propertiesToResolve.Add(property);
            }

            base.PreInvoke(cpc);
        }

        protected override void PostInvoke(CommandProcessorContext cpc)
        {
            base.PostInvoke(cpc);

            foreach (var property in _propertiesToResolve)
            {
                property.State = EFElementState.Normalized;
                property.Resolve(property.Artifact.ArtifactSet);
            }
        }
    }
}
