// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.View.Events;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{

    /// <summary>
    ///     Creates the inheritance described by an answered <see cref="NewInheritanceRequestedEventArgs" />.
    /// </summary>
    /// <remarks>
    ///     Replaces <c>Inheritance_AddFromDialog</c>, which held a live WPF dialog and read its controls from
    ///     inside the transaction.
    /// </remarks>
    internal class InheritanceAddFromRequest : ViewModelChange
    {

        #region Fields

        private readonly NewInheritanceRequestedEventArgs _request;
        private readonly EntityDesignerSurface _surface;

        #endregion

        #region Properties

        internal override int InvokeOrderPriority
        {
            get { return 120; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a change from an answered request.
        /// </summary>
        /// <param name="request">The answered request describing the inheritance to create.</param>
        /// <param name="surface">The surface to report a circular inheritance through.</param>
        internal InheritanceAddFromRequest(NewInheritanceRequestedEventArgs request, EntityDesignerSurface surface)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        #endregion

        #region Internal Methods

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var derived = _request.DerivedEntityType as ConceptualEntityType;

            if (!InheritanceHelper.TrySetBaseEntityType(cpc, derived, _request.BaseEntityType))
            {
                _surface.OnCircularInheritanceDetected(derived, _request.BaseEntityType);
            }
        }

        #endregion

    }

}
