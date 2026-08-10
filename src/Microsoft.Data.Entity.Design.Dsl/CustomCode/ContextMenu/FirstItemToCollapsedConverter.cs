// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// Converts the first item in an ItemsControl to Collapsed, all others to Visible.
    /// Used to hide the separator before the first item.
    /// </summary>
    internal class FirstItemToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ContentPresenter presenter)
            {
                var itemsControl = ItemsControl.ItemsControlFromItemContainer(presenter);
                if (itemsControl != null)
                {
                    int index = itemsControl.ItemContainerGenerator.IndexFromContainer(presenter);
                    return index == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
