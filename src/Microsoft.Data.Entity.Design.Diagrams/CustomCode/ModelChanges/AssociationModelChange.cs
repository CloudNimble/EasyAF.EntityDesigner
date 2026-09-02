// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View.Events;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    /// <summary>
    ///     Creates the association described by an answered <see cref="NewAssociationRequestedEventArgs" />.
    /// </summary>
    /// <remarks>
    ///     Replaces <c>Association_AddFromDialog</c>, which held a live WPF dialog and read its controls from
    ///     inside the transaction.
    /// </remarks>
    internal class AssociationAddFromRequest : ViewModelChange
    {

        #region Fields

        private readonly NewAssociationRequestedEventArgs _request;

        #endregion

        #region Properties

        internal override int InvokeOrderPriority
        {
            get { return 130; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a change from an answered request.
        /// </summary>
        /// <param name="request">The answered request describing the association to create.</param>
        internal AssociationAddFromRequest(NewAssociationRequestedEventArgs request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        #endregion

        #region Internal Methods

        internal override void Invoke(CommandProcessorContext cpc)
        {
            CreateConceptualAssociationCommand cmd = new CreateConceptualAssociationCommand(
                _request.AssociationName,
                _request.End1Entity,
                _request.End1Multiplicity,
                _request.End1NavigationPropertyName,
                _request.End2Entity,
                _request.End2Multiplicity,
                _request.End2NavigationPropertyName,
                false, // uniquify names
                _request.CreateForeignKeyProperties);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);
            Debug.Assert(cmd.CreatedAssociation != null, "cmd.CreatedAssociation != null");
        }

        #endregion

    }
}
