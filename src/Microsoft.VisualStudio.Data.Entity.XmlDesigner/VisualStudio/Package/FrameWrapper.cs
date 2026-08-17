// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio.Package
{
    internal abstract class FrameWrapper
    {
        protected IVsWindowFrame _frame;

        protected FrameWrapper(IVsWindowFrame frame)
        {
            _frame = frame;
        }

        public override bool Equals(object obj)
        {
            if (_frame == null
                && obj == null)
            {
                return true;
            }

            if (obj is FrameWrapper frameWrapper2)
            {
                return _frame == frameWrapper2._frame;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (_frame != null)
            {
                return _frame.GetHashCode();
            }
            return 0;
        }

        internal Uri Uri
        {
            get
            {
                if (_frame != null)
                {
                    if (_frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object value) == NativeMethods.S_OK)
                    {
                        if (value is string filename)
                        {
                            try
                            {
                                return new Uri(filename);
                            }
                            catch (Exception)
                            {
                                // numerous exceptions could occur here since the moniker property of the frame doesn't have
                                // to be in the URI format. There could also be security exceptions, FNF exceptions, etc.
                                return null;
                            }
                        }
                    }
                }
                return null;
            }
        }

        protected Guid Editor
        {
            get
            {
                var editorGuid = Guid.Empty;
                _frame?.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out editorGuid);
                return editorGuid;
            }
        }

        internal abstract bool ShouldShowToolWindows { get; }
        internal abstract bool IsDesignerDocInDesigner { get; }
        internal abstract bool IsDesignerDocInXmlEditor { get; }

        internal IVsTextView TextView
        {
            get
            {
                if (_frame != null)
                {
                    NativeMethods.ThrowOnFailure(_frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object value));
                    if (value is IVsCodeWindow codeWindow)
                    {
                        var hr = codeWindow.GetLastActiveView(out IVsTextView textView);
                        if (!NativeMethods.Succeeded(hr) || textView == null)
                        {
                            textView = VsShellUtilities.GetTextView(_frame);
                        }
                        return textView;
                    }
                }
                return null;
            }
        }

        internal void Show()
        {
            _frame?.Show();
        }

        internal bool IsDocumentOpen(IServiceProvider sp)
        {
            if (sp != null)
            {
                var uri = Uri;
                if (uri != null)
                {
                    if (VsShellUtilities.IsDocumentOpen(sp, uri.LocalPath, Guid.Empty, out IVsUIHierarchy hier, out uint itemId, out IVsWindowFrame frame))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
