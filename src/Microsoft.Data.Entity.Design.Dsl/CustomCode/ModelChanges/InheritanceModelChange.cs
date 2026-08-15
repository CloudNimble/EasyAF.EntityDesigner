// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Dsl.Rules;
using Microsoft.Data.Entity.Design.Dsl.View.Events;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Dsl.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Dsl.ModelChanges
{
    internal abstract class InheritanceModelChange : ViewModelChange
    {
        private readonly Inheritance _inheritance;

        protected InheritanceModelChange(Inheritance inheritance)
        {
            _inheritance = inheritance;
        }

        public Inheritance Inheritance
        {
            get { return _inheritance; }
        }
    }

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
