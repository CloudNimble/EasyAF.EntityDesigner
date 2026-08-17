// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Diagrams.View.Events;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    /// <summary>
    ///     Creates the entity type described by an answered <see cref="NewEntityTypeRequestedEventArgs" />.
    /// </summary>
    /// <remarks>
    ///     Replaces <c>EntityType_AddFromDialog</c>, which held a live WPF dialog and read its controls from
    ///     inside the transaction. Taking values instead is what lets an entity type be created without a dialog
    ///     existing at all.
    /// </remarks>
    internal class EntityTypeAddFromRequest : ViewModelChange
    {

        #region Fields

        private readonly NewEntityTypeRequestedEventArgs _request;

        #endregion

        #region Properties

        internal override int InvokeOrderPriority
        {
            get { return 100; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a change from an answered request.
        /// </summary>
        /// <param name="request">The answered request describing the entity type to create.</param>
        internal EntityTypeAddFromRequest(NewEntityTypeRequestedEventArgs request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        #endregion

        #region Internal Methods

        internal override void Invoke(CommandProcessorContext cpc)
        {
            if (_request.BaseEntityType is not null)
            {
                CreateEntityTypeCommand.CreateDerivedEntityType(cpc, _request.EntityName, _request.BaseEntityType, false);

                return;
            }

            CreateEntityTypeCommand.CreateConceptualEntityTypeAndEntitySetAndProperty(
                cpc,
                _request.EntityName,
                _request.EntitySetName,
                _request.CreateKeyProperty,
                _request.KeyPropertyName,
                _request.KeyPropertyType,
                ModelHelper.CanTypeSupportIdentity(_request.KeyPropertyType)
                    ? ModelConstants.StoreGeneratedPattern_Identity
                    : ModelConstants.StoreGeneratedPattern_None,
                false);
        }

        #endregion

    }
}
