// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    internal abstract class BaseColumnConverter<T> : DynamicListConverter<MappingLovEFElement, T>
        where T : BaseColumn
    {
        // call EnsureTypeConverters() to ensure that we are resetting
        // the _currentElement
        protected override void InitializeMapping(ITypeDescriptorContext context)
        {
            Debug.Assert(context != null, "Null context");
            if (context != null)
            {
                if (context.Instance is MappingEFElement mappingElement
                    && context.PropertyDescriptor is BaseColumn propertyDescriptor)
                {
                    propertyDescriptor.EnsureTypeConverters(mappingElement);
                }
            }

            base.InitializeMapping(context);
        }

        // editing using keyboard can pass in a string - allow this
        public override bool /* TypeConverter */ CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        // editing using keyboard can pass in a string - just return it unaltered
        public override object /* TypeConverter */ ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // if context is null we are passing in a string (possibly from dropdown) via keyboard entry
            if (context == null
                && value is string stringValue)
            {
                return stringValue;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

}
