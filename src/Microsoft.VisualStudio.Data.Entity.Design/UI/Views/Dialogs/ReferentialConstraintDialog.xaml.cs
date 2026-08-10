// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal partial class ReferentialConstraintDialog : DialogWindow
    {
        private bool _needsValidation;
        private bool _shouldDeleteOnly;
        private readonly Association _association;
        private readonly AssociationEnd _end1;
        private readonly AssociationEnd _end2;

        private AssociationEnd _principal;
        private AssociationEnd _dependent;

        private readonly Dictionary<AssociationEnd, RoleListItem> _roleListItems = [];
        private readonly RoleListItem _blankRoleListItem = new RoleListItem(null, false);

        private readonly Dictionary<Symbol, MappingListItem> _mappingListItems = [];

        private readonly Dictionary<Symbol, KeyListItem> _dependentListItems = [];
        private readonly KeyListItem _blankDependentKeyListItem = new KeyListItem(null);

        private bool _handlingSelection;

        #region Test support

        private static event EventHandler DialogActivatedTestEventStorage;

        internal static event EventHandler DialogActivatedTestEvent
        {
            add { DialogActivatedTestEventStorage += value; }
            remove { DialogActivatedTestEventStorage -= value; }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            DialogActivatedTestEventStorage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        internal static IEnumerable<Command> LaunchReferentialConstraintDialog(Association association)
        {
            List<Command> commands = new List<Command>();
            if (association != null)
            {
                if (association.ReferentialConstraint == null
                    ||
                    (association.ReferentialConstraint != null &&
                     association.ReferentialConstraint.Principal != null &&
                     association.ReferentialConstraint.Principal.Role.Target != null &&
                     association.ReferentialConstraint.Principal.Role.Target.Type.Target != null &&
                     association.ReferentialConstraint.Dependent != null &&
                     association.ReferentialConstraint.Dependent.Role.Target != null &&
                     association.ReferentialConstraint.Dependent.Role.Target.Type.Target != null
                    )
                    )
                {
                    var dlg = new ReferentialConstraintDialog(association);
                    var result = dlg.ShowModal();
                    if (result == true
                        && dlg.Principal != null
                        && dlg.Dependent != null)
                    {
                        if (association.ReferentialConstraint != null)
                        {
                            // first, enqueue the delete command (always)
                            commands.Add(association.ReferentialConstraint.GetDeleteCommand());
                        }

                        if (dlg.ShouldDeleteOnly == false)
                        {
                            List<Property> principalProps = new List<Property>();
                            List<Property> dependentProps = new List<Property>();

                            var keys = GetKeysForType(dlg.Principal.Type.Target);

                            foreach (var mli in dlg.MappingList)
                            {
                                if (mli.IsValidPrincipalKey)
                                {
                                    // try to resolve the symbol into a property
                                    Property p = null;
                                    Property d = null;
                                    if (mli.PrincipalKey != null)
                                    {
                                        p = GetKeyForType(mli.PrincipalKey, dlg.Principal.Type.Target, keys);
                                    }

                                    if (mli.DependentProperty != null)
                                    {
                                        d = dlg.Dependent.Artifact.ArtifactSet.LookupSymbol(mli.DependentProperty) as Property;
                                    }

                                    if (p != null
                                        && d != null)
                                    {
                                        principalProps.Add(p);
                                        dependentProps.Add(d);
                                    }
                                }
                            }

                            // now enqueue the command to create a new one if the user didn't click Delete
                            Debug.Assert(
                                principalProps.Count == dependentProps.Count,
                                "principal (" + principalProps.Count + ") & dependent (" + dependentProps.Count
                                + ") property counts must match!");
                            if (principalProps.Count > 0)
                            {
                                Command cmd = new CreateReferentialConstraintCommand(
                                    dlg.Principal,
                                    dlg.Dependent,
                                    principalProps,
                                    dependentProps);
                                commands.Add(cmd);
                            }
                        }
                    }
                }
                else
                {
                    VsUtils.ShowMessageBox(
                        PackageManager.Package,
                        Design.Resources.Error_CannotEditRefConstraint,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                        OLEMSGICON.OLEMSGICON_WARNING);
                }
            }
            return commands;
        }

        internal ReferentialConstraintDialog(Association association)
        {
            if (association == null)
            {
                throw new ArgumentNullException(nameof(association));
            }

            _association = association;
            _end1 = _association.AssociationEnds()[0];
            _end2 = _association.AssociationEnds()[1];

            var selfAssociation = (_end1.Type.Target == _end2.Type.Target);

            _roleListItems.Add(_end1, new RoleListItem(_end1, selfAssociation));
            _roleListItems.Add(_end2, new RoleListItem(_end2, selfAssociation));

            InitializeComponent();
            this.HasHelpButton = false;

            // Update dependent key header if foreign keys are supported
            if (EdmFeatureManager.GetForeignKeysInModelFeatureState(association.Artifact.SchemaVersion).IsEnabled())
            {
                DependentKeyHeader.Text = DialogsResource.RefConstraintDialog_DependentKeyHeader_SupportFKs;
            }

            // Load list of roles
            PrincipalRoleComboBox.Items.Add(_blankRoleListItem);
            PrincipalRoleComboBox.Items.Add(_roleListItems[_end1]);
            PrincipalRoleComboBox.Items.Add(_roleListItems[_end2]);

            // Initialize state
            Loaded += ReferentialConstraintDialog_Loaded;
        }

        private void ReferentialConstraintDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Select the one we have (if one)
            if (_association.ReferentialConstraint != null)
            {
                var principal = _association.ReferentialConstraint.Principal.Role.Target;

                if (principal == _end1 || principal == _end2)
                {
                    PrincipalRoleComboBox.SelectedItem = _roleListItems[principal];
                }
                else
                {
                    Debug.Fail("unexpected principal end doesn't match the ends on the association");
                    PrincipalRoleComboBox.SelectedItem = _blankRoleListItem;
                }
                DeleteButton.IsEnabled = true;
            }
            else
            {
                PrincipalRoleComboBox.SelectedItem = _blankRoleListItem;
                DeleteButton.IsEnabled = false;
                DependentKeyComboBox.IsEnabled = false;
            }

            if (_association.EntityModel.IsCSDL == false)
            {
                PrincipalRoleComboBox.IsEnabled = false;
                MappingsListView.IsEnabled = false;
                OkButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        internal AssociationEnd Principal => _principal;

        internal AssociationEnd Dependent => _dependent;

        internal IEnumerable<MappingListItem> MappingList
        {
            get
            {
                foreach (var mli in _mappingListItems.Values)
                {
                    yield return mli;
                }
            }
        }

        internal List<Symbol> PrincipalProperties
        {
            get
            {
                List<Symbol> props = new List<Symbol>();
                if (_principal != null)
                {
                    foreach (var item in _mappingListItems)
                    {
                        if (item.Value.DependentProperty != null)
                        {
                            props.Add(item.Value.PrincipalKey);
                        }
                    }
                }
                return props;
            }
        }

        internal List<Symbol> DependentProperties
        {
            get
            {
                List<Symbol> props = new List<Symbol>();
                if (_dependent != null)
                {
                    foreach (var item in _mappingListItems)
                    {
                        if (item.Value.DependentProperty != null)
                        {
                            props.Add(item.Value.DependentProperty);
                        }
                    }
                }
                return props;
            }
        }

        internal bool ShouldDeleteOnly => _shouldDeleteOnly;

        private void PrincipalRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AssociationEnd principal = null;
            AssociationEnd dependent = null;
            if (PrincipalRoleComboBox.SelectedItem == _roleListItems[_end1])
            {
                principal = _end1;
                dependent = _end2;
            }
            else if (PrincipalRoleComboBox.SelectedItem == _roleListItems[_end2])
            {
                principal = _end2;
                dependent = _end1;
            }

            if (principal == _principal)
            {
                return; // no change
            }

            // Remember new choice
            _principal = principal;
            _dependent = dependent;

            if (_dependent == null)
            {
                DependentRoleTextBox.Text = string.Empty;
            }
            else
            {
                DependentRoleTextBox.Text = _dependent.Role.Value;
            }

            // Clear our lists
            MappingsListView.Items.Clear();
            _mappingListItems.Clear();

            DependentKeyComboBox.Items.Clear();
            _dependentListItems.Clear();

            // User might have chosen the blank row
            if (_principal != null && _dependent != null)
            {
                PopulateMappingListItems();
                PopulateListView();
                MappingsListView.IsEnabled = true;

                // Load dependent role keys into the combo box
                if (_dependent.Type.Target != null)
                {
                    DependentKeyComboBox.Items.Add(_blankDependentKeyListItem);

                    foreach (var key in GetMappableDependentProperties())
                    {
                        KeyListItem item = new KeyListItem(key.NormalizedName);
                        _dependentListItems.Add(key.NormalizedName, item);
                        DependentKeyComboBox.Items.Add(item);
                    }

                    // In the SSDL, ref constraints can be to non-key columns
                    if (_association.EntityModel.IsCSDL == false)
                    {
                        foreach (var prop in _dependent.Type.Target.Properties())
                        {
                            if (prop.IsKeyProperty)
                            {
                                continue;
                            }

                            KeyListItem item = new KeyListItem(prop.NormalizedName);
                            _dependentListItems.Add(prop.NormalizedName, item);
                            DependentKeyComboBox.Items.Add(item);
                        }
                    }
                }
                DependentKeyComboBox.IsEnabled = true;

                // Select the first row
                if (MappingsListView.Items.Count > 0)
                {
                    MappingsListView.Focus();
                    MappingsListView.SelectedIndex = 0;
                }
            }
            else
            {
                MappingsListView.IsEnabled = false;
                DependentKeyComboBox.IsEnabled = false;
                HideDependentKeyComboBox();
            }
        }

        private IEnumerable<Property> GetMappableDependentProperties()
        {
            IEnumerable<Property> rtrn;
            if (_dependent == null)
            {
                rtrn = new Property[0];
            }
            else if (_association.EntityModel.IsCSDL == false)
            {
                // In the SSDL, ref constraints can be to non-key columns
                rtrn = _dependent.Type.Target.Properties();
            }
            else if (EdmFeatureManager.GetForeignKeysInModelFeatureState(_association.Artifact.SchemaVersion).IsEnabled())
            {
                // Targeting netfx 4.0 or greater, so allow all properties on the dependent end
                List<Property> l = new List<Property>();
                ConceptualEntityType t = _dependent.Type.Target as ConceptualEntityType;
                Debug.Assert(_dependent.Type.Target == null || t != null, "EntityType is not ConceptualEntityType");
                while (t != null)
                {
                    foreach (var p in t.Properties())
                    {
                        if ((p is ComplexConceptualProperty) == false)
                        {
                            l.Add(p);
                        }
                    }
                    t = t.BaseType.Target;
                }
                rtrn = l;
            }
            else
            {
                // Targeting netfx 3.5, so allow only allow keys on the dependent end
                if (_dependent.Type.Target != null)
                {
                    ConceptualEntityType cet = _dependent.Type.Target as ConceptualEntityType;
                    Debug.Assert(cet != null, "entity type is not a conceptual entity type");
                    rtrn = cet.ResolvableTopMostBaseType.ResolvableKeys;
                }
                else
                {
                    rtrn = new Property[0];
                }
            }
            return rtrn;
        }

        private void MappingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_handlingSelection)
            {
                return;
            }

            var mappingListItem = GetSelectedMappingListItem();
            if (mappingListItem == null)
            {
                DependentKeyComboBox.IsEnabled = false;
                DependentKeyComboBox.SelectedItem = _blankDependentKeyListItem;
            }
            else
            {
                DependentKeyComboBox.IsEnabled = true;

                if (mappingListItem.DependentProperty == null)
                {
                    DependentKeyComboBox.SelectedItem = _blankDependentKeyListItem;
                }
                else if (_dependentListItems.ContainsKey(mappingListItem.DependentProperty))
                {
                    DependentKeyComboBox.SelectedItem = _dependentListItems[mappingListItem.DependentProperty];
                }
            }

            HideDependentKeyComboBox();
        }

        private void MappingsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = MappingsListView.SelectedItem as MappingListItemViewModel;
            if (item != null && item.IsValidPrincipalKey)
            {
                ShowDependentKeyComboBox();
            }
        }

        private void MappingsListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F2)
            {
                var item = MappingsListView.SelectedItem as MappingListItemViewModel;
                if (item != null && item.IsValidPrincipalKey)
                {
                    ShowDependentKeyComboBox();
                    e.Handled = true;
                }
            }
        }

        private void ShowDependentKeyComboBox()
        {
            var item = MappingsListView.SelectedItem as MappingListItemViewModel;
            if (item == null || !item.IsValidPrincipalKey)
            {
                return;
            }

            // Position the ComboBox over the dependent key column
            DependentKeyComboBox.Visibility = Visibility.Visible;
            DependentKeyComboBox.Focus();
        }

        private void HideDependentKeyComboBox()
        {
            DependentKeyComboBox.Visibility = Visibility.Collapsed;
        }

        private void DependentKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AssignDependentKeySelection();
        }

        private void DependentKeyComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            HideDependentKeyComboBox();
        }

        private void DependentKeyComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                HideDependentKeyComboBox();
                MappingsListView.Focus();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                AssignDependentKeySelection();
                HideDependentKeyComboBox();
                MappingsListView.Focus();
                e.Handled = true;
            }
        }

        private void AssignDependentKeySelection()
        {
            var viewModel = MappingsListView.SelectedItem as MappingListItemViewModel;
            if (viewModel == null)
            {
                return;
            }

            var mappingListItem = viewModel.MappingItem;
            if (mappingListItem == null)
            {
                return;
            }

            if (DependentKeyComboBox.SelectedItem != _blankDependentKeyListItem)
            {
                // User chose nothing, or else they chose the existing one - just return
                if (DependentKeyComboBox.SelectedItem is not KeyListItem keyListItem
                    || keyListItem.Key.Equals(mappingListItem.DependentProperty))
                {
                    HideDependentKeyComboBox();
                    return;
                }

                // Set the new dependent key
                mappingListItem.DependentProperty = keyListItem.Key;
            }
            else
            {
                // User chose the blank row, but the key is already clear - just leave
                if (mappingListItem.DependentProperty == null)
                {
                    HideDependentKeyComboBox();
                    return;
                }

                // User chose the blank row, clear it out
                mappingListItem.DependentProperty = null;
            }

            // Reload the list view
            var selectedIndex = MappingsListView.SelectedIndex;
            PopulateListView();

            // Reselect the current item
            _handlingSelection = true;
            if (selectedIndex >= 0 && selectedIndex < MappingsListView.Items.Count)
            {
                MappingsListView.SelectedIndex = selectedIndex;
            }
            _handlingSelection = false;

            HideDependentKeyComboBox();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _needsValidation = true;
            _shouldDeleteOnly = false;
            DialogResult = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _needsValidation = true;
            _shouldDeleteOnly = true;
            DialogResult = true;
        }

        /// <summary>
        /// Shows a warning dialog if a dependent property is used more than once.
        /// </summary>
        /// <returns>Whether all dependent properties are used at most once.</returns>
        private bool CheckDepPropMappedOnlyOnce()
        {
            HashSet<Symbol> depPropsAlreadyUsed = new HashSet<Symbol>();
            HashSet<Symbol> dupeProps = new HashSet<Symbol>();

            foreach (var mli in MappingList)
            {
                if (mli.IsValidPrincipalKey)
                {
                    if (mli.DependentProperty != null)
                    {
                        if (depPropsAlreadyUsed.Contains(mli.DependentProperty))
                        {
                            if (!dupeProps.Contains(mli.DependentProperty))
                            {
                                dupeProps.Add(mli.DependentProperty);
                            }
                        }
                        else
                        {
                            depPropsAlreadyUsed.Add(mli.DependentProperty);
                        }
                    }
                }
            }

            if (0 == dupeProps.Count)
            {
                return true;
            }
            else
            {
                StringBuilder listOfDupeProps = new StringBuilder();
                var isFirst = true;
                foreach (var prop in dupeProps)
                {
                    if (!isFirst)
                    {
                        listOfDupeProps.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    }
                    isFirst = false;
                    listOfDupeProps.Append(prop.ToDisplayString());
                }

                VsUtils.ShowMessageBox(
                    PackageManager.Package,
                    string.Format(
                        CultureInfo.CurrentCulture, DialogsResource.RefConstraintDialog_DependentPropMappedMultipleTimes, listOfDupeProps),
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_WARNING);

                return false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // If user pressed the OK button check if a dependent property is mapped more than once
            if (DialogResult == true && false == CheckDepPropMappedOnlyOnce())
            {
                e.Cancel = true;
                return;
            }

            // Validate if the flag is set
            if (_needsValidation)
            {
                _needsValidation = false;

                // Only validate if we aren't deleting
                if (!_shouldDeleteOnly)
                {
                    // Check to see if nothing has been selected
                    if (Principal == null || Dependent == null)
                    {
                        // If nothing selected treat this as a delete
                        _shouldDeleteOnly = true;
                    }
                }
            }
        }

        private void PopulateMappingListItems()
        {
            _mappingListItems.Clear();

            // Load existing mappings if we have a ref constraint and the user
            // hasn't changed the principal role
            if (_association.ReferentialConstraint != null
                && _association.ReferentialConstraint.Principal.Role.Target == _principal)
            {
                var principalType = _principal.Type.Target;
                var principalKeys = GetKeysForType(principalType);

                var pnum = _association.ReferentialConstraint.Principal.PropertyRefs.GetEnumerator();
                var dnum = _association.ReferentialConstraint.Dependent.PropertyRefs.GetEnumerator();

                while (pnum.MoveNext())
                {
                    var psym = pnum.Current.Name.NormalizedName();
                    Symbol dsym = null;
                    if (dnum.MoveNext())
                    {
                        dsym = dnum.Current.Name.NormalizedName();
                    }

                    MappingListItem item = new MappingListItem(psym, dsym, IsValidPrincipalKey(psym, principalKeys));
                    _mappingListItems.Add(item.PrincipalKey, item);
                }
            }

            // Add any remaining principal keys that aren't mapped yet
            ConceptualEntityType principalEntityType = _principal.Type.Target as ConceptualEntityType;
            Debug.Assert(principalEntityType != null, "EntityType is not ConceptualEntityType");
            foreach (var key in principalEntityType.ResolvableTopMostBaseType.ResolvableKeys)
            {
                if (_mappingListItems.ContainsKey(key.NormalizedName) == false)
                {
                    MappingListItem item = new MappingListItem(key.NormalizedName, null, true);
                    _mappingListItems.Add(item.PrincipalKey, item);
                }
            }

            // Attempt to auto-map the keys if we don't have a ref constraint
            if (_association.ReferentialConstraint == null
                && principalEntityType != null
                && _dependent.Type.Target != null)
            {
                List<Symbol> dependentProperties = new List<Symbol>();

                foreach (var dprop in GetMappableDependentProperties())
                {
                    dependentProperties.Add(dprop.NormalizedName);
                }

                // Process each principal key first by name
                foreach (var pkey in principalEntityType.ResolvableTopMostBaseType.ResolvableKeys)
                {
                    var item = _mappingListItems[pkey.NormalizedName];

                    if (item.DependentProperty == null)
                    {
                        item.DependentProperty = dependentProperties.Where(p => p.GetLocalName() == pkey.LocalName.Value).FirstOrDefault();
                        if (item.DependentProperty != null)
                        {
                            dependentProperties.Remove(item.DependentProperty);
                        }
                    }
                }

                // Process any unmapped primary keys now ordinally
                foreach (var pkey in principalEntityType.ResolvableTopMostBaseType.ResolvableKeys)
                {
                    var item = _mappingListItems[pkey.NormalizedName];
                    if (item.DependentProperty == null)
                    {
                        if (dependentProperties.Count > 0)
                        {
                            item.DependentProperty = dependentProperties[0];
                            dependentProperties.Remove(item.DependentProperty);
                        }
                    }
                }
            }
        }

        private void PopulateListView()
        {
            MappingsListView.Items.Clear();

            foreach (var pair in _mappingListItems)
            {
                var item = pair.Value;
                var viewModel = new MappingListItemViewModel(item, _dependent?.Artifact);
                MappingsListView.Items.Add(viewModel);
            }
        }

        private bool IsValidPrincipalKey(Symbol key, HashSet<Property> principalKeys)
        {
            IEnumerable<EFElement> symbols = _association.Artifact.ArtifactSet.GetSymbolList(key);
            foreach (var el in symbols)
            {
                if (el is Property p)
                {
                    if (principalKeys.Contains(p))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private MappingListItem GetSelectedMappingListItem()
        {
            var viewModel = MappingsListView.SelectedItem as MappingListItemViewModel;
            return viewModel?.MappingItem;
        }

        private static Property GetKeyForType(Symbol symbol, EntityType entityType, HashSet<Property> keys)
        {
            IEnumerable<EFElement> elements = entityType.Artifact.ArtifactSet.GetSymbolList(symbol);
            foreach (var e in elements)
            {
                Property p = e as Property;
                if (keys.Contains(p))
                {
                    return p;
                }
            }
            return null;
        }

        private static HashSet<Property> GetKeysForType(EntityType entityType)
        {
            HashSet<Property> principalKeys = new HashSet<Property>();
            if (entityType != null)
            {
                if (entityType is ConceptualEntityType cet)
                {
                    foreach (var c in cet.SafeSelfAndBaseTypes)
                    {
                        foreach (var k in c.ResolvableKeys)
                        {
                            principalKeys.Add(k);
                        }
                    }
                }
                else
                {
                    foreach (var p in entityType.ResolvableKeys)
                    {
                        principalKeys.Add(p);
                    }
                }
            }
            return principalKeys;
        }
    }

    /// <summary>
    /// ViewModel for displaying MappingListItem in the ListView.
    /// </summary>
    internal class MappingListItemViewModel
    {
        private readonly MappingListItem _item;
        private readonly EFArtifact _dependentArtifact;

        internal MappingListItemViewModel(MappingListItem item, EFArtifact dependentArtifact)
        {
            _item = item;
            _dependentArtifact = dependentArtifact;
        }

        internal MappingListItem MappingItem => _item;

        internal bool IsValidPrincipalKey => _item.IsValidPrincipalKey;

        public string PrincipalKeyDisplay
        {
            get
            {
                if (_item.IsValidPrincipalKey == false)
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.RefConstraintDialog_ErrorInRCPrincipalProperty,
                        _item.PrincipalKey.GetLocalName());
                }
                return _item.PrincipalKey.GetLocalName();
            }
        }

        public string DependentPropertyDisplay
        {
            get
            {
                if (_item.DependentProperty == null)
                {
                    return string.Empty;
                }

                if (_dependentArtifact?.ArtifactSet.LookupSymbol(_item.DependentProperty) is Property)
                {
                    return _item.DependentProperty.GetLocalName();
                }
                else
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.RefConstraintDialog_ErrorInRCDependentProperty,
                        _item.DependentProperty.GetLocalName());
                }
            }
        }
    }

    internal class RoleListItem
    {
        private readonly AssociationEnd _end;
        private readonly bool _useRoleName;

        internal RoleListItem(AssociationEnd end, bool useRoleName)
        {
            _end = end;
            _useRoleName = useRoleName;
        }

        internal AssociationEnd End => _end;

        public override string ToString()
        {
            if (_end != null && _useRoleName)
            {
                return _end.Role.Value;
            }
            else if (_end != null && _end.Type.Target != null && _useRoleName == false)
            {
                return _end.Type.Target.LocalName.Value;
            }

            return string.Empty;
        }
    }

    internal class MappingListItem
    {
        private readonly Symbol _principalSymbol;

        internal bool IsValidPrincipalKey { get; private set; }

        internal MappingListItem(Symbol principalSymbol, Symbol dependentSymbol, bool isValidPrincipalKey)
        {
            _principalSymbol = principalSymbol;
            DependentProperty = dependentSymbol;
            IsValidPrincipalKey = isValidPrincipalKey;
        }

        internal Symbol PrincipalKey => _principalSymbol;

        internal Symbol DependentProperty { get; set; }

        internal int CurrentIndex { get; set; }

        public override string ToString()
        {
            if (_principalSymbol != null)
            {
                return _principalSymbol.GetLocalName();
            }
            return string.Empty;
        }
    }

    internal class KeyListItem
    {
        private readonly Symbol _key;

        internal KeyListItem(Symbol key)
        {
            _key = key;
        }

        internal Symbol Key => _key;

        public override string ToString()
        {
            if (_key != null)
            {
                return _key.GetLocalName();
            }
            return string.Empty;
        }
    }
}
