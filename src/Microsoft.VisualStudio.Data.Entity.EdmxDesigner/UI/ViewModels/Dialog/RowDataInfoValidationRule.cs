// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{
    internal class RowDataInfoValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            BindingGroup group = (BindingGroup)value;

            StringBuilder errorMessages = new StringBuilder();
            foreach (var item in group.Items)
            {
                // aggregate errors
                if (item is IDataErrorInfo info)
                {
                    if (string.IsNullOrWhiteSpace(info.Error) == false)
                    {
                        errorMessages.Append((errorMessages.Length != 0 ? ", " : "") + info.Error);
                    }
                }
            }

            if (errorMessages.Length > 0)
            {
                return new ValidationResult(false, errorMessages.ToString().TrimEnd());
            }
            return ValidationResult.ValidResult;
        }
    }
}
