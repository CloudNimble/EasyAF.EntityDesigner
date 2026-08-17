// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Class used to drop down an arbitrary Control, or display a modal dialog editor
    ///     akin to what's being done by the property browser
    /// </summary>
    internal class TypeEditorHost : Control, IWindowsFormsEditorService, ITypeDescriptorContext, IVirtualTreeInPlaceControl
    {
        private TypeEditorHostTextBox _edit;
        private Control _instructionLabel;
        private Control _dropDown;
        private DropDownButton _button;
        private DropDownHolder _dropDownHolder;
        private object _instance; // instance object currently being edited
        private PropertyDescriptor _propertyDescriptor; // property descriptor describing instance object property being edited
        private readonly UITypeEditor _uiTypeEditor; // cached uiTypeEditor
        private bool _inShowDialog; // flag set to true if we are showing a dialog.

        private DialogResult _dialogResult;
                             // cached DialogResult from ShowDialog call.  Used in OpenDropDown to determine whether to dismiss the label edit control if commitOnClose is set.

        // cache these because we won't use them until handle creation.
        private TypeEditorHostEditControlStyle _editControlStyle;
        private readonly UITypeEditorEditStyle _editStyle;

        private const int DROP_DOWN_DEFAULT_HEIGHT = 100;

        /// <summary>
        ///     Fired after the dropdown closes
        /// </summary>
        public event EventHandler DropDownClosed;

        /// <summary>
        ///     Creates a new TypeEditorHost to display the given UITypeEditor
        /// </summary>
        /// <param name="editor">The UITypeEditor instance to host</param>
        /// <param name="propertyDescriptor">Property descriptor used to get/set values in the drop-down.</param>
        /// <param name="instance">Instance object used to get/set values in the drop-down.</param>
        protected TypeEditorHost(UITypeEditor editor, PropertyDescriptor propertyDescriptor, object instance)
            :
                this(UITypeEditorEditStyle.DropDown, propertyDescriptor, instance, TypeEditorHostEditControlStyle.Editable)
        {
            _uiTypeEditor = editor;
            if (editor != null)
            {
                _editStyle = editor.GetEditStyle(this);
            }
        }

        /// <summary>
        ///     Creates a new TypeEditorHost to display the given UITypeEditor
        /// </summary>
        /// <param name="editor">The UITypeEditor instance to host</param>
        /// <param name="editControlStyle">The type of control to show in the edit area.</param>
        /// <param name="propertyDescriptor">Property descriptor used to get/set values in the drop-down.</param>
        /// <param name="instance">Instance object used to get/set values in the drop-down.</param>
        protected TypeEditorHost(
            UITypeEditor editor, PropertyDescriptor propertyDescriptor, object instance, TypeEditorHostEditControlStyle editControlStyle)
            :
                this(UITypeEditorEditStyle.DropDown, propertyDescriptor, instance, editControlStyle)
        {
            _uiTypeEditor = editor;
            if (editor != null)
            {
                _editStyle = editor.GetEditStyle(this);
            }
        }

        /// <summary>
        ///     Creates a new TypeEditorHost with the given editStyle.
        /// </summary>
        /// <param name="editStyle">Style of editor to create.</param>
        /// ///
        /// <param name="propertyDescriptor">Property descriptor used to get/set values in the drop-down.</param>
        /// <param name="instance">Instance object used to get/set values in the drop-down.</param>
        /// <param name="editControlStyle">Style of text box to create.</param>
        protected TypeEditorHost(
            UITypeEditorEditStyle editStyle, PropertyDescriptor propertyDescriptor, object instance,
            TypeEditorHostEditControlStyle editControlStyle)
        {
            if (m_inPlaceHelper != null)
            {
                return; // only allow initialization once
            }

            _editStyle = editStyle;
            _editControlStyle = editControlStyle;
            CurrentPropertyDescriptor = propertyDescriptor;
            CurrentInstance = instance;

            // initialize VirtualTree in-place edit stuff
            m_inPlaceHelper = VirtualTreeControl.CreateInPlaceControlHelper(this);
            m_inPlaceHelper.Flags &= ~VirtualTreeInPlaceControls.SizeToText; // size to full item width by default

            // set accessible role of the parent control of the text box/button to combo box, 
            // so accessibility clients have a clue they're dealing with a combo box-like control.
            AccessibleRole = AccessibleRole.ComboBox;
            TabStop = false;
        }

        /// <summary>
        ///     Factory method for creating the appropriate drop-down control based on the given property descriptor
        /// </summary>
        /// <param name="propertyDescriptor">A property descriptor describing the property being set</param>
        /// <param name="instance">The object instance being edited</param>
        /// <returns>A TypeEditorHost instance if the given property descriptor supports it, null otherwise.</returns>
        public static TypeEditorHost Create(PropertyDescriptor propertyDescriptor, object instance)
        {
            TypeEditorHost dropDown = null;

            if (propertyDescriptor != null)
            {
                if (propertyDescriptor.GetEditor(typeof(UITypeEditor)) is UITypeEditor uiTypeEditor) // UITypeEditor case
                {
                    dropDown = new TypeEditorHost(uiTypeEditor, propertyDescriptor, instance);
                }
                else
                {
                    var converter = propertyDescriptor.Converter;
                    if (converter != null
                        && converter.GetStandardValuesSupported(null)) // converter case
                    {
                        dropDown = new TypeEditorHostListBox(converter, propertyDescriptor, instance);
                    }
                }
            }

            return dropDown;
        }

        /// <summary>
        ///     Factory method for creating the appropriate drop-down control based on the given property descriptor.
        ///     If the property descriptor supports a UITypeEditor, a TypeEditorHost will be created with that editor.
        ///     If not, and the TypeConverver attached to the PropertyDescriptor supports standard values, a
        ///     TypeEditorHostListBox will be created with this TypeConverter.
        /// </summary>
        /// <param name="propertyDescriptor">A property descriptor describing the property being set</param>
        /// <param name="instance">The object instance being edited</param>
        /// <param name="editControlStyle">The type of control to show in the edit area.</param>
        /// <returns>A TypeEditorHost instance if the given property descriptor supports it, null otherwise.</returns>
        public static TypeEditorHost Create(
            PropertyDescriptor propertyDescriptor, object instance, TypeEditorHostEditControlStyle editControlStyle)
        {
            TypeEditorHost dropDown = null;

            if (propertyDescriptor != null)
            {
                if (propertyDescriptor.GetEditor(typeof(UITypeEditor)) is UITypeEditor uiTypeEditor) // UITypeEditor case
                {
                    dropDown = new TypeEditorHost(uiTypeEditor, propertyDescriptor, instance, editControlStyle);
                }
                else
                {
                    var converter = propertyDescriptor.Converter;
                    if (converter != null
                        && converter.GetStandardValuesSupported(null)) // converter case
                    {
                        dropDown = new TypeEditorHostListBox(converter, propertyDescriptor, instance, editControlStyle);
                    }
                }
            }

            return dropDown;
        }

        /// <summary>
        ///     Create the edit/drop-down controls when our handle is created.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // create the edit box.  done before creating the button because
            // DockStyle.Right controls should be added first.
            switch (_editControlStyle)
            {
                case TypeEditorHostEditControlStyle.Editable:
                    InitializeEdit();
                    break;

                case TypeEditorHostEditControlStyle.ReadOnlyEdit:
                    InitializeEdit();
                    _edit.ReadOnly = true;
                    break;

                case TypeEditorHostEditControlStyle.InstructionLabel:
                    InitializeInstructionLabel();
                    UpdateInstructionLabelText();
                    break;
            }

            // create the drop-down button
            if (EditStyle != UITypeEditorEditStyle.None)
            {
                _button = new DropDownButton();
                _button.Dock = DockStyle.Right;
                _button.FlatStyle = FlatStyle.Flat;
                _button.FlatAppearance.BorderSize = 0;
                _button.FlatAppearance.MouseDownBackColor
                    = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxButtonMouseDownBackgroundColorKey);
                _button.FlatAppearance.MouseOverBackColor
                    = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxButtonMouseOverBackgroundColorKey);

                // only allow focus to go to the drop-down button if we don't have an edit control.
                // if we have an edit control, we want this to act like a combo box, where only the edit control
                // is focusable.  If there's no edit control, though, we need to make sure the button can be 
                // focused, to prevent WinForms from forwarding focus somewhere else.
                _button.TabStop = _edit == null;

                if (_editStyle == UITypeEditorEditStyle.DropDown)
                {
                    _button.Image = CreateArrowBitmap();
                    // set button name for accessibility purposes.
                    _button.AccessibleName = VirtualTreeStrings.GetString(VirtualTreeStrings.DropDownButtonAccessibleName);
                }
                else if (_editStyle == UITypeEditorEditStyle.Modal)
                {
                    _button.Image = CreateDotDotDotBitmap();

                    // set button name for accessibility purposes.
                    _button.AccessibleName = VirtualTreeStrings.GetString(VirtualTreeStrings.BrowseButtonAccessibleName);
                }
                // Bug 17449 (Currituck). Use the system prescribed one, property grid uses this approach in 
                // ndp\fx\src\winforms\managed\system\winforms\propertygridinternal\propertygridview.cs 
                _button.Size = new Size(SystemInformation.VerticalScrollBarArrowHeight, Font.Height);

                _button.BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey);

                Controls.Add(_button);
                _button.Click += OnDropDownButtonClick;
                _button.LostFocus += OnContainedControlLostFocus;
            }

            // Create the drop down control container.  Check for null here because this
            // may have already been created via a call to SetComponent.
            if (_dropDownHolder == null
                && EditStyle == UITypeEditorEditStyle.DropDown)
            {
                _dropDownHolder = new DropDownHolder(this);
                _dropDownHolder.Font = Font;
            }
        }

        private void InitializeEdit()
        {
            _edit = CreateTextBox();
            _edit.BorderStyle = BorderStyle.None;
            _edit.AutoSize = false; // with no border, AutoSize causes some overlap with the grid lines on the tree control, so we remove it.
            _edit.Dock = DockStyle.Fill;
            _edit.Text = base.Text; // we store text locally prior to handle creation, so we set it here.
            _edit.KeyDown += OnEditKeyDown;
            _edit.KeyPress += OnEditKeyPress;
            _edit.LostFocus += OnContainedControlLostFocus;
            _edit.TextChanged += OnEditTextChanged;
            Controls.Add(_edit);
        }

        /// <summary>
        ///     Create the edit control.  Derived classes may override to customize the edit control.
        /// </summary>
        protected virtual TypeEditorHostTextBox CreateTextBox()
        {
            return new TypeEditorHostTextBox(this);
        }

        private void InitializeInstructionLabel()
        {
            _instructionLabel = CreateInstructionLabel();
            _instructionLabel.Dock = DockStyle.Fill;
            Controls.Add(_instructionLabel);
        }

        /// <summary>
        ///     Create the instruction label control.  Derived classes may override this to customize the instruction label.
        /// </summary>
        /// <returns>A new instruction label</returns>
        protected virtual Label CreateInstructionLabel()
        {
            Label label = new Label();
            label.UseMnemonic = false;
            return label;
        }

        /// <summary>
        ///     Determine the TypeEditorHostEditControlStyle settings for this control
        /// </summary>
        public TypeEditorHostEditControlStyle EditControlStyle
        {
            get { return _editControlStyle; }
            set
            {
                if (value != _editControlStyle)
                {
                    _editControlStyle = value;
                    // Whatever control is currently visible has to go.
                    _edit?.Dispose();
                    _edit = null;
                    _instructionLabel?.Dispose();
                    _instructionLabel = null;
                    switch (value)
                    {
                        case TypeEditorHostEditControlStyle.Editable:
                            InitializeEdit();
                            break;
                        case TypeEditorHostEditControlStyle.ReadOnlyEdit:
                            InitializeEdit();
                            _edit.ReadOnly = true;
                            break;
                        case TypeEditorHostEditControlStyle.InstructionLabel:
                            InitializeInstructionLabel();
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     UITypeEditorEditStyle used by this TypeEditorHost.
        /// </summary>
        public UITypeEditorEditStyle EditStyle
        {
            get { return _editStyle; }
        }

        /// <summary>
        ///     Get/set the current property descriptor
        /// </summary>
        public PropertyDescriptor CurrentPropertyDescriptor
        {
            get { return _propertyDescriptor; }
            set
            {
                _propertyDescriptor = value;
                UpdateInstructionLabelText();
            }
        }

        /// <summary>
        ///     Get/set the current instance object
        /// </summary>
        public object CurrentInstance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                UpdateInstructionLabelText();
            }
        }

        /// <summary>
        ///     Specifies whether the label edit control should remain active when the drop-down closes.
        ///     Note that this only applies to cases where the value changes, if the value does not change
        ///     or the user cancels the edit, the edit control will remain active regardless of the value of
        ///     this property.
        ///     The default value is false, which means that the label edit conrol remains active.
        /// </summary>
        /// <value></value>
        public bool DismissLabelEditOnDropDownClose { get; set; }

        /// <summary>
        ///     The instruction label text is based on the value returned from the current
        ///     property descriptor. This function should be called when the CurrentPropertyDescriptor
        ///     and Instance properties change to update the text.
        /// </summary>
        protected void UpdateInstructionLabelText()
        {
            if (_instructionLabel != null
                && _propertyDescriptor != null
                && _instance != null)
            {
                _instructionLabel.Text = Text;
            }
        }

        /// <summary>
        ///     Specifies window creation flags.
        /// </summary>
        protected override CreateParams CreateParams
        {
            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                var cp = base.CreateParams;

                // remove WS_CLIPCHILDREN.  Presence of this style causes update regions of our child controls
                // not to be invalidated when the parent control is resized, which causes painting issues.
                cp.Style &= ~NativeMethods.WS_CLIPCHILDREN;
                return cp;
            }
        }

        /// <summary>
        ///     Control.WndProc override
        /// </summary>
        /// <param name="m">Message</param>
        [SecuritySafeCritical]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            try
            {
                // Do special processing if the control is simply in pass through mode
                if (_edit == null)
                {
                    switch (m.Msg)
                    {
                        case NativeMethods.WM_PAINT:
                            if (_instructionLabel == null
                                && m.WParam == IntPtr.Zero)
                            {
                                NativeMethods.Rectangle rect = new NativeMethods.Rectangle(ClientRectangle);

                                if (_button != null)
                                {
                                    rect.Right = _button.Left;
                                }

                                NativeMethods.ValidateRect(m.HWnd, ref rect);
                                if (!NativeMethods.GetUpdateRect(m.HWnd, IntPtr.Zero, false))
                                {
                                    return;
                                }
                            }

                            break;
                        default:
                            if (m.Msg >= NativeMethods.WM_MOUSEFIRST
                                && m.Msg <= NativeMethods.WM_MOUSELAST)
                            {
                                if (m_inPlaceHelper.OnMouseMessage(ref m) || IsDisposed)
                                {
                                    return;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    // if we have an edit control, forward clipboard commands to it.
                    if (m.Msg == NativeMethods.WM_CUT
                        || m.Msg == NativeMethods.WM_COPY
                        || m.Msg == NativeMethods.WM_PASTE)
                    {
                        // forward these to the underlying edit control
                        m.Result = NativeMethods.SendMessage(_edit.Handle, m.Msg, m.WParam, m.LParam);
                        return;
                    }
                }

                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                if (CriticalException.IsCriticalException(e))
                {
                    throw;
                }

                m_inPlaceHelper.DisplayException(e);
            }
        }

        /// <summary>
        ///     Control.IsInputKey override
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns>true if the parent control will need the keys for navigation</returns>
        protected override bool IsInputKey(Keys keyData)
        {
            if (_edit == null)
            {
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                        // Key these to the message loop, where we can forward them to the parent control
                        return true;
                }
            }
            return base.IsInputKey(keyData);
        }

        /// <summary>
        ///     Control.OnKeyDown override. First tries to open dropdown, then defers
        ///     to the helper, and finally the control
        /// </summary>
        /// <param name="e">KeyEventArgs</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (e.Alt
                            && !e.Control)
                        {
                            OpenDropDown();
                            e.Handled = true;
                            return;
                        }
                        break;
                }
            }
            // pass on to the outer control, for further handling
            if (!m_inPlaceHelper.OnKeyDown(e))
            {
                base.OnKeyDown(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _edit?.Dispose();

                _instructionLabel?.Dispose();

                _button?.Dispose();

                _dropDown?.Dispose();

                _dropDownHolder?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Specify the control to display in the drop-down
        /// </summary>
        /// <param name="control">control to display</param>
        public void SetComponent(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            // create the drop-down holder, if necessary
            if (_dropDownHolder == null)
            {
                _dropDownHolder = new DropDownHolder(this);
                _dropDownHolder.Font = Font;
            }

            if (_dropDown != null)
            {
                _dropDown.KeyDown -= OnDropDownKeyDown;
                _dropDown.Dispose();
            }

            _dropDown = control;

            if (_dropDown != null)
            {
                // site the control, to allow it access to services from our container
                control.Site = Site;
                control.KeyDown += OnDropDownKeyDown;
                _dropDownHolder.SetComponent(_dropDown, (_uiTypeEditor == null) ? Resizable : _uiTypeEditor.IsDropDownResizable);
            }
        }

        /// <summary>
        ///     Override to make the dropdown resizable. This property is ignored if a UITypeEditor is
        ///     provided.
        /// </summary>
        protected virtual bool Resizable
        {
            get { return false; }
        }

        protected TextBox Edit
        {
            get { return _edit; }
        }

        /// <summary>
        ///     Allow clients to customize the edit control accessible name.
        /// </summary>
        public string EditAccessibleName
        {
            get { return _edit.AccessibleName; }
            set { _edit.AccessibleName = value; }
        }

        /// <summary>
        ///     Allow clients to customize the edit control accessible description.
        /// </summary>
        public string EditAccessibleDescription
        {
            get { return _edit.AccessibleDescription; }
            set { _edit.AccessibleDescription = value; }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            // size button
            _button?.Height = Height;

            base.OnLayout(e);

            /*if(edit != null)
            {
                // center text box vertically
                edit.Top += ((Height - edit.Height) / 2);
            }*/
        }

        /// <summary>
        ///     Update the drop-down container font when our font changes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFontChanged(EventArgs e)
        {
            // update the font of the drop down container
            _dropDownHolder?.Font = Font;

            base.OnFontChanged(e);
        }

        /// <summary>
        ///     Returns true iff the drop down currently contains the focus
        /// </summary>
        public bool DropDownContainsFocus
        {
            get { return _dropDownHolder != null && _dropDownHolder.ContainsFocus; }
        }

        protected Control DropDown
        {
            get { return _dropDown; }
        }

        public override string Text
        {
            get
            {
                if (_edit != null)
                {
                    return _edit.Text;
                }
                else if (!IsHandleCreated
                         && (EditControlStyle == TypeEditorHostEditControlStyle.Editable
                             || EditControlStyle == TypeEditorHostEditControlStyle.ReadOnlyEdit))
                {
                    // prior to handle creation, the edit control will not be created, so we store text
                    // locally in this control.
                    return base.Text;
                }
                else if (_instance != null
                         && _propertyDescriptor != null)
                {
                    var converter = _propertyDescriptor.Converter;
                    var value = _propertyDescriptor.GetValue(_instance);
                    if (converter != null
                        && converter.CanConvertTo(this, typeof(string)))
                    {
                        return converter.ConvertToString(this, CultureInfo.CurrentUICulture, value);
                    }
                    return value.ToString();
                }
                return String.Empty;
            }
            set
            {
                if (_edit != null)
                {
                    _edit.Text = value;
                    OnTextChanged(EventArgs.Empty);
                }
                else if (!IsHandleCreated
                         && (EditControlStyle == TypeEditorHostEditControlStyle.Editable
                             || EditControlStyle == TypeEditorHostEditControlStyle.ReadOnlyEdit))
                {
                    base.Text = value;
                }
            }
        }

        /// <summary>
        ///     Return the height of the control contained in the drop down control.
        ///     The default implementation returns the width of the contained drop down control.
        /// </summary>
        protected virtual int DropDownHeight
        {
            get { return (_dropDownHolder != null) ? _dropDownHolder.Component.Height : DROP_DOWN_DEFAULT_HEIGHT; }
        }

        /// <summary>
        ///     Returns the width of the control contained in the dropdown. The default implementation
        ///     returns the width of the drop down control, if one is provided.
        ///     This override is not used if IgnoreDropDownWidth returns true.
        /// </summary>
        protected virtual int DropDownWidth
        {
            get { return (_dropDownHolder != null) ? _dropDownHolder.Component.Width : Width; }
        }

        /// <summary>
        ///     Override this property and return true to always give the dropdown
        ///     the same initial size as the dropdown host control. If this property
        ///     is set, the DropDownWidth property will not be called.
        /// </summary>
        protected virtual bool IgnoreDropDownWidth
        {
            get { return false; }
        }

        /// <summary>
        ///     Fired before the drop down is opened.  Clients may sink this event if they need to perform
        ///     some initialization before the drop-down control is shown.
        /// </summary>
        public event EventHandler OpeningDropDown;

        /// <summary>
        ///     Fired after the property descriptor's SetValue method is called and before
        ///     the OnTextChanged method is called to (potentially) resize the control. Responding to
        ///     this event allows the use the change the EditControlStyle property for different values
        /// </summary>
        public event EventHandler PropertyDescriptorValueChanged;

        /// <summary>
        ///     Notify derived classes that the drop-down is opening
        /// </summary>
        protected virtual void OnOpeningDropDown(EventArgs e)
        {
            // inform listeners that the drop-down is about to open
            if (OpeningDropDown != null)
            {
                OpeningDropDown(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     Notify derived classes that CurrentPropertyDescriptor.SetValue has been called.
        /// </summary>
        protected virtual void OnPropertyDescriptorValueChanged(EventArgs e)
        {
            // inform listeners that the value is changing
            if (PropertyDescriptorValueChanged != null)
            {
                PropertyDescriptorValueChanged(this, EventArgs.Empty);
            }
        }

        private void DisplayDropDown()
        {
            Debug.Assert(_dropDownHolder != null, "OpenDropDown called with no control to drop down");
            OnOpeningDropDown(EventArgs.Empty);

            // Get drop down height
            var dropHeight = DropDownHeight + 2 * DropDownHolder.DropDownHolderBorder;
            var dropWidth = IgnoreDropDownWidth ? Width : DropDownWidth + 2 * DropDownHolder.DropDownHolderBorder;

            // position drop-down under the arrow
            Point location = new Point(Width - dropWidth, Height);
            location = PointToScreen(location);

            Rectangle bounds = new Rectangle(location, new Size(dropWidth, dropHeight));
            Screen currentScreen = Screen.FromControl(this);
            if (currentScreen != null
                && bounds.Bottom > currentScreen.WorkingArea.Bottom)
            {
                // open the drop-down upwards if it will go off the bottom of the screen
                bounds.Y -= (dropHeight + Height);
                _dropDownHolder.ResizeUp = true;
            }
            else
            {
                _dropDownHolder.ResizeUp = false;
            }

            _dropDownHolder.Bounds = bounds;
            // display drop-down
            _dropDownHolder.Visible = true;

            _dropDownHolder.Focus();

            _dropDownHolder.HookMouseDown = true;
            _dropDownHolder.DoModalLoop();
            _dropDownHolder.HookMouseDown = false;
        }

        internal void CloseDropDown(bool accept)
        {
            Debug.Assert(_dropDownHolder != null, "CloseDropDown called with no drop-down");
            _dropDownHolder.Visible = false;
            _dropDownHolder.HookMouseDown = false;

            if (accept && (0 != String.Compare(Text, _dropDown.Text, false, CultureInfo.CurrentCulture)))
            {
                Text = _dropDown.Text;
            }

            if (_edit != null)
            {
                _edit.Focus();
            }
            else
            {
                Focus();
            }
        }

        private static Bitmap CreateArrowBitmap()
        {
            Bitmap bitmap = null;
            Icon icon = null;
            try
            {
                icon = new Icon(typeof(TypeEditorHost), "arrow.ico");
                using (Bitmap original = icon.ToBitmap())
                {
                    bitmap = BitmapFromImageReplaceColor(original);
                }
            }
            catch
            {
                bitmap = new Bitmap(16, 16);
                throw;
            }
            finally
            {
                icon?.Dispose();
            }
            return bitmap;
        }

        private static Bitmap CreateDotDotDotBitmap()
        {
            Bitmap bitmap = null;
            Icon icon = null;
            try
            {
                icon = new Icon(typeof(TypeEditorHost), "dotdotdot.ico");
                using (Bitmap original = icon.ToBitmap())
                {
                    bitmap = BitmapFromImageReplaceColor(original);
                }
            }
            catch
            {
                bitmap = new Bitmap(16, 16);
                throw;
            }
            finally
            {
                icon?.Dispose();
            }
            return bitmap;
        }

        private static Bitmap BitmapFromImageReplaceColor(Image original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height, original.PixelFormat);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                using (ImageAttributes attrs = new ImageAttributes())
                {
                    ColorMap cm = new ColorMap();

                    cm.OldColor = Color.Black;
                    // Bug 17449 (Currituck). Use the system prescribed one, property grid uses this approach in 
                    // ndp\fx\src\winforms\managed\system\winforms\propertygridinternal\propertygridview.cs 
                    cm.NewColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxGlyphColorKey);
                    attrs.SetRemapTable(new[] { cm }, ColorAdjustType.Bitmap);
                    g.DrawImage(
                        original, new Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height,
                        GraphicsUnit.Pixel, attrs, null, IntPtr.Zero);
                }
            }

            return newBitmap;
        }

        private void OnDropDownButtonClick(object sender, EventArgs e)
        {
            OpenDropDown();
        }

        /// <summary>
        ///     Open the dropdown
        /// </summary>
        public void OpenDropDown()
        {
            if (_dropDown == null
                || !_dropDown.Visible)
            {
                try
                {
                    if (m_inPlaceHelper.Dirty)
                    {
                        // Handle currently dirty editor.  Follow the property grid here and commit the dirty value before
                        // displaying the drop-down.  Note that this is one case where we take a label edit and convert it
                        // before calling SetValue.  Don't really have much choice, though, since we can't force a CommitLabelEdit
                        // here.
                        Debug.Assert(Edit != null, "how did we become dirty without an in-place edit control?");
                        m_inPlaceHelper.Dirty = false;
                        _propertyDescriptor.SetValue(_instance, ConvertFromString(Text));
                        if (m_inPlaceHelper.Parent == null
                            || m_inPlaceHelper.Parent.LabelEditControl != this)
                        {
                            return; // bail out if we're no longer editing
                        }
                    }

                    // Get the value directly from the property descriptor so that the
                    // EditValue call deals with a raw value, not one that has gone through
                    // type converters, etc.
                    var value = _propertyDescriptor.GetValue(_instance);

                    if (_uiTypeEditor != null)
                    {
                        _dialogResult = DialogResult.None;
                        // we have a UITypeEditor, use it.  UITypeEditor usually calls back on ShowDialog or OpenDropDown
                        // via the IWindowsFormsEditorService, so in most cases this pushes a modal loop.
                        var newValue = _uiTypeEditor.EditValue(this, this, value);
                        if (m_inPlaceHelper.Parent == null
                            || m_inPlaceHelper.Parent.LabelEditControl != this)
                        {
                            // when control is dismissed, one need to call setvalue to prevent loss of user data
                            // See bug VSW 328713
                            // when control is dismissed, one must *not* call setvalue if the new value is the same as the old one
                            // See bug VSW 383162
                            if (value != newValue)
                            {
                                m_inPlaceHelper.Dirty = false;

                                _propertyDescriptor.SetValue(_instance, newValue);
                                OnPropertyDescriptorValueChanged(EventArgs.Empty);
                            }
                        }
                        else
                        {
                            if (value != newValue)
                            {
                                m_inPlaceHelper.OnTextChanged();
                                if (DismissLabelEditOnDropDownClose)
                                {
                                    // dismiss the edit control if DismissLabelEditOnDropDownClose is set.
                                    m_inPlaceHelper.Parent.EndLabelEdit(true /* cancel */);
                                }
                                else
                                {
                                    // user has made an edit, update the text displayed
                                    Text = ConvertToString(newValue);
                                    SelectAllText();
                                    // inform the VirtualTreeControl that the edit should not dirty the in-place edit window.
                                    // All required data store changes should be made as part of the SetValue call, so
                                    // we do not want to generate a CommitLabelEdit call when the in-place edit is committed.
                                    // we make this call prior to the call to SetValue in case that call triggers the commit.
                                    m_inPlaceHelper.Dirty = false;
                                }

                                _propertyDescriptor.SetValue(_instance, newValue);
                                OnPropertyDescriptorValueChanged(EventArgs.Empty);
                            }
                            else
                            {
                                if (DismissLabelEditOnDropDownClose)
                                {
                                    // dismiss the edit control if DismissLabelEditOnDropDownClose is set.
                                    // if a dialog editor was used and OK was pressed, we do this even if 
                                    // value == newValue.  This enables editors that want to internally handle 
                                    // updates in EditValue rather than through a separate SetValue call.
                                    m_inPlaceHelper.Parent.EndLabelEdit(true /* cancel */);
                                }
                                else
                                {
                                    // the property browser refreshes after EditValue returns, no matter what the return value is.
                                    // To mimic this, we'll update the value from the property descriptor if no edit is made.
                                    // This enables editors that want to internally handle updates in EditValue rather than through
                                    // a separate SetValue call.
                                    var refreshValue = _propertyDescriptor.GetValue(_instance);
                                    if (refreshValue != null)
                                    {
                                        Text = ConvertToString(refreshValue);
                                        SelectAllText();
                                        m_inPlaceHelper.Dirty = false;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // non-UITypeEditor case.  May be a TypeEditorHostListBox that uses a TypeConverter,
                        // or a derived class that doesn't use a UITypeEditor.

                        // Open the drop down.  This pushes a modal loop.
                        DisplayDropDown();
                        if (m_inPlaceHelper.Parent == null
                            || m_inPlaceHelper.Parent.LabelEditControl != this
                            || !Dirty)
                        {
                            return; // bail out if we're no longer editing
                        }

                        // use our text to determine the new value.  This will be set 
                        // in CloseDropDown, if the user makes an edit
                        var newValue = (Edit != null) ? ConvertFromString(Text) : null;
                        if ((value != null && !value.Equals(newValue))
                            || (value == null && newValue != null))
                        {
                            m_inPlaceHelper.OnTextChanged();
                            if (DismissLabelEditOnDropDownClose)
                            {
                                // dismiss the edit control if DismissLabelEditOnDropDownClose is set.
                                m_inPlaceHelper.Parent.EndLabelEdit(true /* cancel */);
                            }
                            else
                            {
                                m_inPlaceHelper.Dirty = false; // see comment above
                            }

                            _propertyDescriptor.SetValue(_instance, newValue);
                            OnPropertyDescriptorValueChanged(EventArgs.Empty);
                        }
                    }

                    OnDropDownClosed(EventArgs.Empty);
                }
                catch (Exception e)
                {
                    if (CriticalException.IsCriticalException(e))
                    {
                        throw;
                    }

                    // display exceptions thrown during drop-down open to the user
                    if (!m_inPlaceHelper.DisplayException(e))
                    {
                        throw;
                    }
                }
            }
            else
            {
                CloseDropDown(false);
            }
        }

        private object ConvertFromString(string value)
        {
            if (_propertyDescriptor.PropertyType == typeof(string))
            {
                return value;
            }

            var converter = _propertyDescriptor.Converter;
            if (converter != null
                && converter.CanConvertFrom(this, typeof(string)))
            {
                try
                {
                    return converter.ConvertFromString(this, CultureInfo.CurrentCulture, value);
                }
                catch (Exception e)
                {
                    if (CriticalException.IsCriticalException(e))
                    {
                        // We throw if this is a critical exception.
                        throw;
                    }
                    // If the string cannot be converted by the TypeConverter, we return null.
                    // This change was made for VSW Bug # 448059. When a boolean value had
                    // not been initialized to true or false, and the user hit the drop-down
                    // control and dismissed it without selecting an item, we got an empty
                    // string that we tried to convert to a boolean. Ideally we would be passed
                    // a different type-converter for a boolean that can exist in uninitialized
                    // state, but that would have been a bigger code change towards the end of Beta2.
                    // We put this fix in instead so that if the client passes us a string that
                    // their TypeConverter cannot handle, then we return null. We hope that if
                    // the client passed us a string and a TypeConverter that are incompatible,
                    // then their PropertyDescriptor can handle setting a value to null.
                    return null;
                }
            }

            return null;
        }

        private string ConvertToString(object value)
        {
            if (value is string stringValue)
            {
                return stringValue;
            }

            var converter = _propertyDescriptor.Converter;
            if (converter != null
                && converter.CanConvertTo(this, typeof(string)))
            {
                return converter.ConvertToString(this, CultureInfo.CurrentCulture, value);
            }

            return null;
        }

        /// <summary>
        ///     The dropdown has been closed
        /// </summary>
        protected virtual void OnDropDownClosed(EventArgs e)
        {
            if (DropDownClosed != null)
            {
                DropDownClosed(this, e);
            }
        }

        private void OnEditKeyDown(object sender, KeyEventArgs e)
        {
            OnEditKeyDown(e);
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEditKeyDown(KeyEventArgs e)
        {
            OnKeyDown(e);
        }

        private void OnEditKeyPress(object sender, KeyPressEventArgs e)
        {
            OnEditKeyPress(e);
        }

        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEditKeyPress(KeyPressEventArgs e)
        {
            // pass on to the outer control, for further handling
            OnKeyPress(e);
        }

        private void OnContainedControlLostFocus(object sender, EventArgs e)
        {
            // pass on to the outer control, for further handling
            OnLostFocus(e);
        }

        private void OnEditTextChanged(object sender, EventArgs e)
        {
            // this causes the Text property of this control to 
            // change as well, so treat it as such
            OnTextChanged(e);
        }

        /// <summary>
        ///     Update drop-down button image when the system colors change.
        ///     Required for high-contrast mode support.
        /// </summary>
        protected override void OnSystemColorsChanged(EventArgs e)
        {
            // colors actually haven't been updated at this point.
            // wait for the System event to let us know they have
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Color)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

                // recreate button image, required to support high contrast
                var editStyle = UITypeEditorEditStyle.DropDown;

                if (_uiTypeEditor != null)
                {
                    editStyle = _uiTypeEditor.GetEditStyle();
                }

                var oldImage = _button.Image;

                if (oldImage != null)
                {
                    try
                    {
                        if (editStyle == UITypeEditorEditStyle.DropDown)
                        {
                            _button.Image = CreateArrowBitmap();
                        }
                        else if (editStyle == UITypeEditorEditStyle.Modal)
                        {
                            _button.Image = CreateDotDotDotBitmap();
                        }
                    }
                    finally
                    {
                        oldImage.Dispose();
                    }
                }
            }
        }

        private void OnDropDownKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CloseDropDown(false);
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    CloseDropDown(true);
                    e.Handled = true;
                    break;
            }

            if (!e.Handled)
            {
                // pass this on to the outer control, for further handling
                OnKeyDown(e);
            }
        }

        internal static bool HandleContains(IntPtr parentHandle, IntPtr childHandle)
        {
            if (parentHandle == IntPtr.Zero)
            {
                return false;
            }
            while (childHandle != IntPtr.Zero)
            {
                if (childHandle == parentHandle)
                {
                    return true;
                }
                childHandle = NativeMethods.GetParent(childHandle);
            }
            return false;
        }

        #region ITypeDescriptorContext Members

        /// <summary>
        ///     ITypeDescriptorContext.OnComponentChanged
        /// </summary>
        public void /*ITypeDescriptorContext*/ OnComponentChanged()
        {
            // don't support IComponentChangeService
        }

        IContainer ITypeDescriptorContext.Container
        {
            get { return TypeDescriptorContextContainer; }
        }

        /// <summary>
        ///     Gets the IContainer that contains the Component.
        /// </summary>
        /// <value></value>
        protected static IContainer TypeDescriptorContextContainer
        {
            get
            {
                // we don't support containers
                return null;
            }
        }

        /// <summary>
        ///     ITypeDescriptorContext.OnComponentChanging implementation.
        /// </summary>
        /// <returns>true</returns>
        public bool /*ITypeDescriptorContext*/ OnComponentChanging()
        {
            // Don't really support IComponentChangeService
            // but must return true to allow component update.
            // However, framework will not fire ComponentChanged event.
            return true;
        }

        /// <summary>
        ///     ITypeDescriptorContext.Instance implementation
        /// </summary>
        public object /*ITypeDescriptorContext*/ Instance
        {
            get { return _instance; }
        }

        /// <summary>
        ///     ITypeDescriptorContext.PropertyDescriptor implementation
        /// </summary>
        public PropertyDescriptor /*ITypeDescriptorContext*/ PropertyDescriptor
        {
            get { return _propertyDescriptor; }
        }

        #endregion

        #region IServiceProvider Members

        object IServiceProvider.GetService(Type serviceType)
        {
            return ServiceProviderGetService(serviceType);
        }

        /// <summary>
        ///     Gets the service provided by this TypeEditorHost.
        /// </summary>
        /// <param name="serviceType">type of service being requested.</param>
        /// <returns></returns>
        protected object ServiceProviderGetService(Type serviceType)
        {
            // services we support
            if (serviceType == typeof(IWindowsFormsEditorService)
                || serviceType == typeof(ITypeDescriptorContext))
            {
                return this;
            }

            // delegate to our site for other services
            if (Site != null)
            {
                return Site.GetService(serviceType);
            }

            return null;
        }

        #endregion

        #region IWindowsFormsEditorService Members

        void IWindowsFormsEditorService.DropDownControl(Control control)
        {
            DropDownControl(control);
        }

        /// <summary>
        ///     Display the given control in a dropdown. Implements IWindowsFormsEditorService.DropDownControl
        /// </summary>
        /// <param name="control">The control to display</param>
        protected void DropDownControl(Control control)
        {
            if (_dropDown != control)
            {
                SetComponent(control);
            }

            DisplayDropDown();
        }

        void IWindowsFormsEditorService.CloseDropDown()
        {
            CloseDropDown();
        }

        /// <summary>
        ///     Implements IWindowsFormsEditorService.CloseDropDown
        /// </summary>
        protected void CloseDropDown()
        {
            CloseDropDown(false);
        }

        DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog)
        {
            return ShowDialog(dialog);
        }

        /// <summary>
        ///     Show the type editor dialog. Implements IWindowsFormsEditorService.ShowDialog.
        /// </summary>
        /// <param name="dialog"></param>
        /// <returns></returns>
        protected DialogResult ShowDialog(Form dialog)
        {
            _dialogResult = DialogResult.None;

            // try to shift down if sitting right on top of existing owner.
            if (dialog.StartPosition == FormStartPosition.CenterScreen)
            {
                Control topControl = this;
                if (topControl != null)
                {
                    while (topControl.Parent != null)
                    {
                        topControl = topControl.Parent;
                    }
                    if (topControl.Size.Equals(dialog.Size))
                    {
                        dialog.StartPosition = FormStartPosition.Manual;
                        var location = topControl.Location;
                        // CONSIDER what constant to get here?
                        location.Offset(25, 25);
                        dialog.Location = location;
                    }
                }
            }

            IUIService service = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService));
            try
            {
                _inShowDialog = true;
                if (service != null)
                {
                    _dialogResult = service.ShowDialog(dialog);
                }
                else
                {
                    _dialogResult = dialog.ShowDialog(this);
                }
            }
            finally
            {
                _inShowDialog = false;
            }

            // give focus back to the text box
            if (_edit != null)
            {
                _edit.Focus();
            }
            else
            {
                Focus();
            }

            return _dialogResult;
        }

        #endregion

        #region Implementation of IInPlaceControl

        /// <summary>
        ///     Select all text in the edit area
        /// </summary>
        public void SelectAllText()
        {
            // force handle creation here, because we need the edit control.
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            // Note that m_edit.SelectAll() is not quite the same as the following code
            if (Edit != null)
            {
                Edit.Focus();
                var hwndEdit = Edit.Handle;
                NativeMethods.SendMessage(hwndEdit, NativeMethods.EM_SETSEL, -1, -1); // move to the end
                NativeMethods.SendMessage(hwndEdit, NativeMethods.EM_SETSEL, 0, -1); // select all text
            }
        }

        /// <summary>
        ///     Return the position of the current selection start
        /// </summary>
        public int SelectionStart
        {
            get { return (Edit == null) ? 0 : Edit.SelectionStart; }
            set
            {
                // force handle creation here, because we need the edit control.
                if (!IsHandleCreated)
                {
                    CreateHandle();
                }

                Edit?.SelectionStart = value;
            }
        }

        /// <summary>
        ///     Return maximum length of text in the edit field
        /// </summary>
        public int MaxTextLength
        {
            get { return (Edit == null) ? 0 : Edit.MaxLength; }
            set
            {
                // force handle creation here, because we need the edit control.
                if (!IsHandleCreated)
                {
                    CreateHandle();
                }

                Edit?.MaxLength = value;
            }
        }

        /// <summary>
        /// </summary>
        public Rectangle FormattingRectangle
        {
            get
            {
                // UNDONE: Untrue statement
                // not necessary since we're always sizing this
                // based on cell size, not text size
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// </summary>
        int IVirtualTreeInPlaceControl.ExtraEditWidth
        {
            get { return ExtraEditWidth; }
        }

        /// <summary>
        ///     Implementation of IVirtualTreeInPlaceControl.ExtraEditWidth
        /// </summary>
        protected int ExtraEditWidth
        {
            get
            {
                var retVal = 0;

                if (EditControlStyle == TypeEditorHostEditControlStyle.Editable
                    || EditControlStyle == TypeEditorHostEditControlStyle.ReadOnlyEdit)
                {
                    retVal += VirtualTreeInPlaceControlHelper.DefaultExtraEditWidth;
                }

                if (EditStyle != UITypeEditorEditStyle.None)
                {
                    retVal += 16; // bitmap size for button
                }

                return retVal;
            }
        }

        #endregion

        #region Boilerplate InPlaceControl code

        private readonly VirtualTreeInPlaceControlHelper m_inPlaceHelper;

        /// <summary>
        ///     Parent VirtualTreeControl
        /// </summary>
        VirtualTreeControl IVirtualTreeInPlaceControlDefer.Parent
        {
            get { return VirtualTreeInPlaceControlDeferParent; }
            set { VirtualTreeInPlaceControlDeferParent = value; }
        }

        /// <summary>
        ///     The tree control that activates this object.
        /// </summary>
        protected VirtualTreeControl VirtualTreeInPlaceControlDeferParent
        {
            get { return m_inPlaceHelper.Parent; }
            set { m_inPlaceHelper.Parent = value; }
        }

        /// <summary>
        ///     Windows message used to launch the control. Defers to helper.
        /// </summary>
        int IVirtualTreeInPlaceControlDefer.LaunchedByMessage
        {
            get { return LaunchedByMessage; }
            set { LaunchedByMessage = value; }
        }

        /// <summary>
        ///     The windows message (WM_LBUTTONDOWN) that launched the edit
        ///     control. Use 0 for for a timer, selection change, or explicit
        ///     launch. Used to support mouse behavior while a control is in-place
        ///     activating in response to a mouse click.
        /// </summary>
        protected int LaunchedByMessage
        {
            get { return m_inPlaceHelper.LaunchedByMessage; }
            set { m_inPlaceHelper.LaunchedByMessage = value; }
        }

        bool IVirtualTreeInPlaceControlDefer.Dirty
        {
            get { return Dirty; }
            set { Dirty = value; }
        }

        /// <summary>
        ///     Dirty state of the in-place control.  Defers to helper.
        /// </summary>
        protected bool Dirty
        {
            get { return m_inPlaceHelper.Dirty; }
            set { m_inPlaceHelper.Dirty = value; }
        }

        /// <summary>
        ///     Returns the in-place control itself (this)
        /// </summary>
        public Control InPlaceControl
        {
            get { return m_inPlaceHelper.InPlaceControl; }
        }

        /// <summary>
        ///     Settings indicating how the inplace control interacts with the tree control. Defaults
        ///     to SizeToText | DisposeControl.
        /// </summary>
        public VirtualTreeInPlaceControls Flags
        {
            get { return m_inPlaceHelper.Flags; }
            set { m_inPlaceHelper.Flags = value; }
        }

        /// <summary>
        ///     Pass KeyPress events on to the parent control for special handling
        /// </summary>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (!e.Handled)
            {
                e.Handled = m_inPlaceHelper.OnKeyPress(e);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            m_inPlaceHelper.OnTextChanged();
            base.OnTextChanged(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (!_inShowDialog
                && !ContainsFocus
                && !DropDownContainsFocus)
            {
                m_inPlaceHelper.OnLostFocus();
            }
            base.OnLostFocus(e);
        }

        #endregion // Boilerplace InPlaceControl code

        /// <summary>
        ///     Derived class so we can customize the accessibility keyboard shortcut
        /// </summary>
        private class DropDownButton : Button
        {
            protected override AccessibleObject CreateAccessibilityInstance()
            {
                return new DropDownButtonAccessibleObject(this);
            }

            /// <summary>
            ///     Control.WndProc override
            /// </summary>
            /// <param name="m">Message</param>
            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            protected override void WndProc(ref Message m)
            {
                try
                {
                    base.WndProc(ref m);
                }
                catch (Exception e)
                {
                    if (CriticalException.IsCriticalException(e))
                    {
                        throw;
                    }

                    VirtualTreeControl.DisplayException(Parent.Site, e);
                }
            }
        }

        private class DropDownButtonAccessibleObject : ControlAccessibleObject
        {
            public DropDownButtonAccessibleObject(Control inner)
                : base(inner)
            {
            }

            /// <summary>
            ///     return Alt+Down as our keyboard shortcut
            /// </summary>
            public override string KeyboardShortcut
            {
                get { return VirtualTreeStrings.GetString(VirtualTreeStrings.DropDownAccessibleShortcut); }
            }
        }
    }

}
