// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Specializes the TypeEditorHost to use a ListBox as the control to drop down,
    ///     making this essentially a ComboBox.
    /// </summary>
    internal class TypeEditorHostListBox : TypeEditorHost
    {
        private readonly ListBox _listBox;
        private TypeConverter _typeConverter;

        /// <summary>
        ///     Creates a new drop-down control to display the given TypeConverter
        /// </summary>
        /// <param name="typeConverter">The TypeConverter instance to retrieve drop-down values from</param>
        /// <param name="propertyDescriptor">Property descriptor used to get/set values in the drop-down.</param>
        /// <param name="instance">Instance object used to get/set values in the drop-down.</param>
        protected internal TypeEditorHostListBox(TypeConverter typeConverter, PropertyDescriptor propertyDescriptor, object instance)
            :
                this(
                typeConverter, propertyDescriptor, instance,
                (typeConverter != null && typeConverter.GetStandardValuesExclusive())
                    ? TypeEditorHostEditControlStyle.ReadOnlyEdit
                    : TypeEditorHostEditControlStyle.Editable)
        {
        }

        /// <summary>
        ///     Creates a new drop-down list to display the given type converter.
        ///     The type converter must support a standard values collection.
        /// </summary>
        /// <param name="typeConverter">The TypeConverter instance to retrieve drop-down values from</param>
        /// <param name="propertyDescriptor">Property descriptor used to get/set values in the drop-down.</param>
        /// <param name="instance">Instance object used to get/set values in the drop-down.</param>
        /// <param name="editControlStyle">Edit control style.</param>
        protected internal TypeEditorHostListBox(
            TypeConverter typeConverter, PropertyDescriptor propertyDescriptor, object instance,
            TypeEditorHostEditControlStyle editControlStyle)
            :
                base(UITypeEditorEditStyle.DropDown, propertyDescriptor, instance, editControlStyle)
        {
            _typeConverter = typeConverter;

            // UNDONE: currently, this class only supports exclusive values.

            // create the list box
            _listBox = new ListBox { BorderStyle = BorderStyle.None };
            _listBox.MouseUp += OnDropDownMouseUp;

            _listBox.BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey);
            _listBox.ForeColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey);

            if (_typeConverter != null
                && _typeConverter.GetStandardValuesSupported(this))
            {
                // populate it with values from the type converter
                foreach (var value in _typeConverter.GetStandardValues())
                {
                    _listBox.Items.Add(value);
                }
            }

            // set list box as the drop control
            SetComponent(_listBox);
        }

        private void OnDropDownMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                CloseDropDown(true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_listBox != null)
                {
                    _listBox.MouseUp -= OnDropDownMouseUp;
                    _listBox.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override int DropDownHeight
        {
            get
            {
                var itemCount = _listBox.Items.Count;
                return itemCount < 8 ? (itemCount * _listBox.ItemHeight) + (_listBox.ItemHeight / 2) : 8 * _listBox.ItemHeight;
            }
        }

        /// <summary>
        ///     Do not call the DropDownWidth override
        /// </summary>
        protected override bool IgnoreDropDownWidth
        {
            get { return true; }
        }

        protected override void OnEditKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (_listBox.SelectedIndex > 0)
                    {
                        _listBox.SelectedIndex--;
                        Text = _listBox.Text;
                        if (Edit != null)
                        {
                            Edit.SelectionStart = 0;
                            Edit.SelectionLength = Edit.Text.Length;
                        }
                    }
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                    {
                        _listBox.SelectedIndex++;
                        Text = _listBox.Text;
                        if (Edit != null)
                        {
                            Edit.SelectionStart = 0;
                            Edit.SelectionLength = Edit.Text.Length;
                        }
                    }
                    e.Handled = true;
                    break;
            }
            base.OnEditKeyDown(e);
        }

        protected override void OnEditKeyPress(KeyPressEventArgs e)
        {
            base.OnEditKeyPress(e);

            if (!e.Handled)
            {
                var caret = (Edit == null) ? 0 : Edit.SelectionStart;

                string currentString = null;
                if (caret == 0)
                {
                    currentString = new string(e.KeyChar, 1);
                }
                else if (caret > 0)
                {
                    currentString = Edit.Lines[0].Substring(0, caret) + e.KeyChar;
                }

                if (currentString != null
                    && currentString.Length > 0)
                {
                    var index = _listBox.SelectedIndex;

                    if (index == -1)
                    {
                        index = 0;
                    }

                    var endIndex = index;

                    var foundMatch = false;

                    while (true)
                    {
                        // TODO : refine this algorithm.  Should we assume the
                        // list is sorted?
                        var itemText = _listBox.Items[index].ToString();

                        if (currentString.Length <= itemText.Length
                            && String.Compare(itemText, 0, currentString, 0, currentString.Length, true, CultureInfo.CurrentUICulture) == 0)
                        {
                            foundMatch = true;
                            break;
                        }

                        index++;

                        if (index == _listBox.Items.Count)
                        {
                            index = 0;
                        }

                        if (index == endIndex)
                        {
                            break;
                        }
                    }

                    if (foundMatch)
                    {
                        _listBox.SelectedIndex = index;
                        Text = _listBox.Text;
                        Edit?.SelectionStart = currentString.Length;
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Overriden to set up the list index based on current text
        /// </summary>
        protected override void OnOpeningDropDown(EventArgs e)
        {
            // make sure list box is hardened appropriately against exceptions.
            if (!(_listBox.WindowTarget is SafeWindowTarget))
            {
                _listBox.WindowTarget = new SafeWindowTarget(Site, _listBox.WindowTarget);
            }
            // if we have a type converter, populate with values
            if (_listBox.Items.Count > 0)
            {
                SetListBoxIndexForCurrentText();
            }

            base.OnOpeningDropDown(e);
        }

        /// <summary>
        ///     Overriden to set up the list index based on current text
        /// </summary>
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            // ensure that list box index is set appropriately
            if (_listBox.Items.Count > 0
                && (_listBox.SelectedIndex == -1
                    || String.Compare(_listBox.Items[_listBox.SelectedIndex].ToString(), Text, true, CultureInfo.CurrentUICulture) != 0))
            {
                SetListBoxIndexForCurrentText();
            }
        }

        private void SetListBoxIndexForCurrentText()
        {
            var selectedIndex = 0;
            var found = false;

            for (var i = 0; i < _listBox.Items.Count; i++)
            {
                var itemText = _listBox.Items[i].ToString();

                if (String.Compare(itemText, Text, true, CultureInfo.CurrentUICulture) == 0)
                {
                    selectedIndex = i;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _listBox.SelectedIndex = selectedIndex;
            }
        }

        public ListBox.ObjectCollection Items
        {
            get { return _listBox.Items; }
        }
    }

}
