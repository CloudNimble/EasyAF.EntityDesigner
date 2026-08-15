// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Dsl.View.Events;
using Microsoft.Data.Entity.Design.UI.Views.Dialogs;
using Microsoft.VisualStudio.Data.Entity.Design.UI.Views.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;
using DesignRes = Microsoft.Data.Entity.Design.Resources;

namespace Microsoft.Data.Entity.Design.Package
{
    /// <summary>
    ///     Answers the designer's requests for user input, by showing Visual Studio dialogs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The designer raises an event describing what it needs and waits for values back. This is the only
    ///         place those events meet a dialog, which is what lets the same designer assembly run under the
    ///         command line renderer, where nothing subscribes and every request is simply declined.
    ///     </para>
    ///     <para>
    ///         Attached per document view and disposed with it, so the handlers are removed rather than
    ///         accumulating. See specs/platform-independence.md.
    ///     </para>
    /// </remarks>
    internal sealed class VsDiagramRequestHandler : IDisposable
    {

        #region Fields

        private readonly EntityDesignerSurface _surface;
        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        ///     Subscribes to <paramref name="surface" />'s requests.
        /// </summary>
        /// <param name="surface">The designer surface to answer for.</param>
        internal VsDiagramRequestHandler(EntityDesignerSurface surface)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));

            _surface.NewEntityTypeRequested += OnNewEntityTypeRequested;
            _surface.NewAssociationRequested += OnNewAssociationRequested;
            _surface.NewInheritanceRequested += OnNewInheritanceRequested;
            _surface.UnmappedStorageEntitySetsDeletionRequested += OnUnmappedStorageEntitySetsDeletionRequested;
            _surface.ReferentialConstraintRequested += OnReferentialConstraintRequested;
            _surface.CircularInheritanceDetected += OnCircularInheritanceDetected;
            _surface.MappingDetailsNavigationRequested += OnMappingDetailsNavigationRequested;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Stops answering requests.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _surface.NewEntityTypeRequested -= OnNewEntityTypeRequested;
            _surface.NewAssociationRequested -= OnNewAssociationRequested;
            _surface.NewInheritanceRequested -= OnNewInheritanceRequested;
            _surface.UnmappedStorageEntitySetsDeletionRequested -= OnUnmappedStorageEntitySetsDeletionRequested;
            _surface.ReferentialConstraintRequested -= OnReferentialConstraintRequested;
            _surface.CircularInheritanceDetected -= OnCircularInheritanceDetected;
            _surface.MappingDetailsNavigationRequested -= OnMappingDetailsNavigationRequested;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Tells the user an inheritance was rejected because it would have been circular.
        /// </summary>
        private void OnCircularInheritanceDetected(object sender, CircularInheritanceDetectedEventArgs e)
        {
            VsUtils.ShowErrorDialog(
                String.Format(
                    CultureInfo.CurrentCulture,
                    DesignRes.Error_CircularInheritanceAborted,
                    e.DerivedEntityType.LocalName.Value,
                    e.BaseEntityType.LocalName.Value));
        }

        /// <summary>
        ///     Follows the designer's navigation in the mapping details window, if one is open.
        /// </summary>
        private void OnMappingDetailsNavigationRequested(object sender, MappingDetailsNavigationRequestedEventArgs e)
        {
            var context = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(
                e.MappingElement.Artifact.Uri);
            var mappingDetailsInfo = context.Items.GetValue<MappingDetailsInfo>();

            if (e.UsesFunctionMapping.HasValue)
            {
                mappingDetailsInfo.EntityMappingMode = e.UsesFunctionMapping.Value
                    ? EntityMappingModes.Functions
                    : EntityMappingModes.Tables;
            }

            mappingDetailsInfo.MappingDetailsWindow?.NavigateTo(e.MappingElement);
        }

        /// <summary>
        ///     Collects the details of a new association.
        /// </summary>
        private void OnNewAssociationRequested(object sender, NewAssociationRequestedEventArgs e)
        {
            var dialog = new NewAssociationDialog(e.EntityTypes, e.SuggestedEnd1, e.SuggestedEnd2);

            if (dialog.ShowModal() != true)
            {
                return;
            }

            e.AssociationName = dialog.AssociationName;
            e.End1Entity = dialog.End1Entity;
            e.End1Multiplicity = dialog.End1Multiplicity;
            e.End1NavigationPropertyName = dialog.End1NavigationPropertyName;
            e.End2Entity = dialog.End2Entity;
            e.End2Multiplicity = dialog.End2Multiplicity;
            e.End2NavigationPropertyName = dialog.End2NavigationPropertyName;
            e.CreateForeignKeyProperties = dialog.CreateForeignKeyProperties;
            e.Cancelled = false;
        }

        /// <summary>
        ///     Collects the details of a new entity type.
        /// </summary>
        private void OnNewEntityTypeRequested(object sender, NewEntityTypeRequestedEventArgs e)
        {
            var dialog = new NewEntityDialog(e.Model);

            if (dialog.ShowModal() != true)
            {
                return;
            }

            e.EntityName = dialog.EntityName;
            e.EntitySetName = dialog.EntitySetName;
            e.BaseEntityType = dialog.BaseEntityType;
            e.CreateKeyProperty = dialog.CreateKeyProperty;
            e.KeyPropertyName = dialog.KeyPropertyName;
            e.KeyPropertyType = dialog.KeyPropertyType;
            e.Cancelled = false;
        }

        /// <summary>
        ///     Collects the base and derived types for a new inheritance.
        /// </summary>
        private void OnNewInheritanceRequested(object sender, NewInheritanceRequestedEventArgs e)
        {
            var dialog = new NewInheritanceDialog(e.SuggestedBaseEntityType, e.EntityTypes);

            if (dialog.ShowModal() != true)
            {
                return;
            }

            e.BaseEntityType = dialog.BaseEntityType;
            e.DerivedEntityType = dialog.DerivedEntityType;
            e.Cancelled = false;
        }

        /// <summary>
        ///     Lets the user edit an association's referential constraint.
        /// </summary>
        private void OnReferentialConstraintRequested(object sender, ReferentialConstraintRequestedEventArgs e)
        {
            e.Commands = ReferentialConstraintDialog.LaunchReferentialConstraintDialog(e.Association);
        }

        /// <summary>
        ///     Asks whether storage entity sets left unmapped by a delete should also be deleted.
        /// </summary>
        private void OnUnmappedStorageEntitySetsDeletionRequested(
            object sender, UnmappedStorageEntitySetsDeletionRequestedEventArgs e)
        {
            var dialog = new DeleteStorageEntitySetsDialog(e.UnmappedStorageEntitySets);
            dialog.ShowModal();

            e.DeleteUnmappedSets = dialog.UserChoice;
        }

        #endregion

    }
}
