// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Core.Controls
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="targetType">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="parameter">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="culture">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="targetType">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="parameter">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="culture">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
