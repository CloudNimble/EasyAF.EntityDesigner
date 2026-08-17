// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     A helper class used by external (and internal) inplace controls.
    ///     Implements the IVirtualTreeInPlaceControlDefer interface, allowing
    ///     custom inplace control implementations an implementation to defer to
    ///     for most of the IVirtualTreeInPlaceControl methods.
    /// </summary>
    internal abstract class VirtualTreeInPlaceControlHelper : IVirtualTreeInPlaceControlDefer
    {
        private VirtualTreeControl myParent;
        private readonly Control myInPlaceControl;
        private VirtualTreeInPlaceControls myFlags;

        /// <summary>
        ///     The default value for the extra width placed in an inplace edit
        ///     box to allow extra space for typing.
        /// </summary>
        public static readonly int DefaultExtraEditWidth = SystemInformation.Border3DSize.Width * 5;

        private VirtualTreeInPlaceControlHelper()
        {
        }

        /// <summary>
        ///     A constructor for derived classes.
        /// </summary>
        /// <param name="inPlaceControl">The control to inplace activate</param>
        protected VirtualTreeInPlaceControlHelper(Control inPlaceControl)
        {
            myInPlaceControl = inPlaceControl;

            // default flags
            myFlags = VirtualTreeInPlaceControls.SizeToText | VirtualTreeInPlaceControls.DisposeControl;
        }

        /// <summary>
        ///     The tree control we're current in-place active in.
        /// </summary>
        public VirtualTreeControl Parent
        {
            get { return myParent; }
            set
            {
                myParent = value;
                myInPlaceControl.Parent = value;
            }
        }

        /// <summary>
        ///     The inplace control associated with this helper object
        /// </summary>
        public Control InPlaceControl
        {
            get { return myInPlaceControl; }
        }

        /// <summary>
        ///     Settings indicating how the inplace control interacts with the tree control. Defaults
        ///     to SizeToText | DisposeControl.
        /// </summary>
        public VirtualTreeInPlaceControls Flags
        {
            get { return myFlags; }
            set
            {
                var oldValue = myFlags;
                if (oldValue != value)
                {
                    myFlags = value;
                    OnFlagsChanged(oldValue);
                }
            }
        }

        int IVirtualTreeInPlaceControlDefer.LaunchedByMessage
        {
            get { return LaunchedByMessage; }
            set { LaunchedByMessage = value; }
        }

        /// <summary>
        ///     Indicates the windows message used to create the control. Facilitates correct
        ///     mouse handling for ImmediateMouseLabelEdits where the control is transparent.
        /// </summary>
        public abstract int LaunchedByMessage { get; set; }

        /// <summary>
        ///     Value indicates whether the in-place edit control is currently dirty.
        ///     At commit time, only a dirty in-place edit control generates calls
        ///     to IBranch.CommitLabelEdit or a custom commit delegate.
        /// </summary>
        public abstract bool Dirty { get; set; }

        /// <summary>
        ///     Call this method from the OnKeyDown override in
        ///     the in place control.
        /// </summary>
        /// <param name="e">The KeyEventArgs</param>
        /// <returns>Returns true if the keystroke was handled, indicating that further processing is not needed.</returns>
        public abstract bool OnKeyDown(KeyEventArgs e);

        /// <summary>
        ///     Call this method from the OnKeyPress override in
        ///     the in place control.
        /// </summary>
        /// <param name="e">The KeyPressEventArgs</param>
        /// <returns>Returns true if the keystroke was handled, indicating that further processing is not needed.</returns>
        public abstract bool OnKeyPress(KeyPressEventArgs e);

        /// <summary>
        ///     Called at the beginning of the Control.OnTextChanged override.
        /// </summary>
        public abstract void OnTextChanged();

        /// <summary>
        ///     Called at the beginning of the Control.OnLostFocus override.
        /// </summary>
        public abstract void OnLostFocus();

        /// <summary>
        ///     Called when the Flags value is changed.
        /// </summary>
        /// <param name="oldFlags">The old value of the Flags property</param>
        protected abstract void OnFlagsChanged(VirtualTreeInPlaceControls oldFlags);

        /// <summary>
        ///     Called when a mouse message is received by the inplace control. This callback
        ///     should only be used by controls that need to support transparent edit regions.
        /// </summary>
        /// <param name="message">Message structure passed to WndProc</param>
        /// <returns>True to indicate that the message has been handled and should not be forwarded to base.WndProc</returns>
        public abstract bool OnMouseMessage(ref Message message);

        /// <summary>
        ///     Allows in-place controls to call back on the tree to display an error.
        /// </summary>
        /// <param name="exception">Exception containing information to be displayed.</param>
        /// <returns>
        ///     True if the exception is displayed to the user, false otherwise.  Callers should generally rethrow
        ///     the exception if the return value is false.
        /// </returns>
        public abstract bool DisplayException(Exception exception);
    }

}
