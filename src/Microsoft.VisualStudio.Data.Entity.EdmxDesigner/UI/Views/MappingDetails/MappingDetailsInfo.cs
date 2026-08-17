// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails
{
    internal enum EntityMappingModes
    {
        None,
        Tables,
        Functions
    }

    internal enum EntityMappingSelectionSource
    {
        None,
        EntityDesigner,
        ModelBrowser
    }

    internal class MappingDetailsInfo : ContextItem
    {
        private SelectionContainer<MappingDetailsSelection> _selectionContainer;
        private MappingViewModel _viewModel;
        private EditingContext _context;
        private MappingDetailsWindow _mappingWindow;
        private EntityMappingModes _mode = EntityMappingModes.Tables;
        private EntityMappingSelectionSource _selectionSource = EntityMappingSelectionSource.None;

        internal SelectionContainer<MappingDetailsSelection> SelectionContainer
        {
            get { return _selectionContainer; }
        }

        internal MappingViewModel ViewModel
        {
            get { return _viewModel; }
            set { _viewModel = value; }
        }

        internal MappingDetailsWindow MappingDetailsWindow
        {
            get { return _mappingWindow; }
        }

        internal EditingContext EditingContext
        {
            get { return _context; }
        }

        internal EntityMappingModes EntityMappingMode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        internal void SetMappingDetailsInfo(
            MappingDetailsWindow mappingWindow,
            EditingContext context,
            SelectionContainer<MappingDetailsSelection> selectionContainer)
        {
            _mappingWindow = mappingWindow;
            _context = context;
            _selectionContainer = selectionContainer;

            _context.Disposing += OnContextDisposing;
            _context.Items.SetValue(this);
        }

        internal void ToggleEntityMappingMode()
        {
            if (_mode == EntityMappingModes.Tables)
            {
                _mode = EntityMappingModes.Functions;
            }
            else
            {
                _mode = EntityMappingModes.Tables;
            }
        }

        private void OnContextDisposing(object sender, EventArgs e)
        {
            EditingContext context = (EditingContext)sender;
            Debug.Assert(_context == context, "incorrect context");
            _selectionContainer?.Dispose();
            _selectionContainer = null;
            _viewModel?.Dispose();
            _viewModel = null;

            context.Items.SetValue(new MappingDetailsInfo());
            context.Disposing -= OnContextDisposing;
        }

        internal override Type ItemType
        {
            get { return typeof(MappingDetailsInfo); }
        }

        internal EntityMappingSelectionSource SelectionSource
        {
            get { return _selectionSource; }
            set { _selectionSource = value; }
        }
    }
}
