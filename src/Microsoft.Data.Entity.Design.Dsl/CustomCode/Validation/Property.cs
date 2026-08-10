// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.VisualStudio.Modeling.Validation;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;
using ModelRes = Microsoft.Data.Entity.Design.Model.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ViewModel
{
    [ValidationState(ValidationState.Disabled)]
    internal partial class Property
    {
        /// <summary>
        ///     Validate property name
        /// </summary>
        [ValidationMethod(ValidationCategories.Open | ValidationCategories.Save, CustomCategory = "OnTransactionCommited")]
        private void ValidateName(ValidationContext context)
        {
            if (!EscherAttributeContentValidator.IsValidCsdlPropertyName(Name))
            {
                var message = String.Format(CultureInfo.CurrentCulture, ModelRes.Error_PropertyNameInvalid, Name);
                context.LogError(message, EntityDesignerRes.ErrorCode_PropertyNameInvalid, this);
            }
        }
    }
}
