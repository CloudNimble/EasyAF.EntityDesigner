// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    /// <summary>
    ///     Inheritance edits that are shared by the designer and the properties window.
    /// </summary>
    internal static class InheritanceHelper
    {

        #region Internal Methods

        /// <summary>
        ///     Sets <paramref name="baseEntity" /> as the base type of <paramref name="derivedEntity" />, replacing
        ///     any existing base type, or removes the base type when <paramref name="baseEntity" /> is null.
        /// </summary>
        /// <param name="cpc">Context the commands run in.</param>
        /// <param name="derivedEntity">The entity type whose base type is being set.</param>
        /// <param name="baseEntity">The new base type, or null to remove the existing one.</param>
        /// <returns>
        ///     False when the change would create circular inheritance, in which case nothing is changed. True
        ///     otherwise.
        /// </returns>
        /// <remarks>
        ///     Reports the circular case by returning false rather than telling the user about it. Callers decide
        ///     how to surface it: the designer raises an event the shell answers, the properties window shows a
        ///     dialog directly. This used to live in the Visual Studio layer and show the dialog itself, which
        ///     meant the designer had to reach into the shell to change a base type.
        /// </remarks>
        internal static bool TrySetBaseEntityType(
            CommandProcessorContext cpc, ConceptualEntityType derivedEntity, ConceptualEntityType baseEntity)
        {
            if (ModelHelper.CheckForCircularInheritance(derivedEntity, baseEntity))
            {
                return false;
            }

            CommandProcessor cp = new CommandProcessor(cpc);

            if (derivedEntity.BaseType.Target != null)
            {
                // CreateInheritanceCommand works only for entities that don't have base type set
                // so we need to remove base type first in this case
                cp.EnqueueCommand(new DeleteInheritanceCommand(derivedEntity));
            }

            if (baseEntity != null)
            {
                // in case the user has chosen "(None)" then we just want to delete the existing one
                cp.EnqueueCommand(new CreateInheritanceCommand(derivedEntity, baseEntity));
            }

            // a quick check to be sure
            Debug.Assert(cp.CommandCount > 0, "Why didn't we enqueue at least one command?");
            if (cp.CommandCount > 0)
            {
                cp.Invoke();
            }

            return true;
        }

        #endregion

    }
}
