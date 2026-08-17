// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Tools.XmlDesignerBase;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class UIntOrNoneTypeConverter : StringConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is StringOrPrimitive<UInt32> v)
                {
                    return StringOrPrimitiveConverter<UInt32>.StringConverter(v);
                }
            }

            return null;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                try
                {
                    return DefaultableValueUIntOrNone.Converter.ValueConverter(stringValue);
                }
                catch (ConversionException)
                {
                    // if the Converter throws a ConversionException then user has put in a incorrect
                    // value e.g. text into an uint field - so throw a new exception with a better
                    // error message
                    var attributeName = context.PropertyDescriptor.DisplayName;
                    var message = string.Format(
                        CultureInfo.CurrentCulture, Resources.ConverterIncorrectValueForAttribute, stringValue, attributeName);
                    throw new ConversionException(message);
                }
            }

            return null;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StringOrPrimitive<UInt32>[] standardValues = { DefaultableValueUIntOrNone.NoneValue };
            return new StandardValuesCollection(standardValues);
        }
    }
}
