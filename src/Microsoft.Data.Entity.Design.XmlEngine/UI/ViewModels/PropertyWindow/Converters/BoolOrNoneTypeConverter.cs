// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.XmlDesignerBase;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class BoolOrNoneTypeConverter : StringConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                // do not use BoolOrNone.Converter.StringConverter - that has to return the lower-case
                // values for insertion into XML, whereas this needs to return the capitalized versions
                // for display in drop-down
                if (value is BoolOrNone v)
                {
                    if (BoolOrNone.TrueValue.Equals(v))
                    {
                        return Resources.PropertyWindow_Value_True;
                    }
                    else if (BoolOrNone.FalseValue.Equals(v))
                    {
                        return Resources.PropertyWindow_Value_False;
                    }
                    else
                    {
                        return Resources.NoneDisplayValueUsedForUX;
                    }
                }
            }
            return null;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                return BoolOrNoneConverter.ValueConverter(stringValue);
            }
            return null;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            BoolOrNone[] standardValues =
                {
                    BoolOrNone.NoneValue,
                    BoolOrNone.TrueValue,
                    BoolOrNone.FalseValue,
                };
            return new StandardValuesCollection(standardValues);
        }
    }
}
