// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.Data.Entity.Design.UI.Views
{
    internal class ShowContextMenuEventArgs : EventArgs
    {
        private readonly Point _point;

        public Point Point
        {
            get { return _point; }
        }

        internal ShowContextMenuEventArgs(Point point)
        {
            _point = point;
        }
    }
}
