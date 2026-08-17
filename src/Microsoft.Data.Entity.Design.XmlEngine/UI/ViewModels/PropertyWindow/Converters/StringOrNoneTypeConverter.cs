// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.XmlDesignerBase;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class StringOrNoneTypeConverter : StringConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // this method returns the text displayed in drop-down
            if (destinationType == typeof(string))
            {
                if (value is StringOrNone v)
                {
                    if (StringOrNone.NoneValue.Equals(v))
                    {
                        return Resources.NoneDisplayValueUsedForUX;
                    }
                    else
                    {
                        return v.ToString();
                    }
                }
            }
            return null;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // this method converts the text displayed in the Property Grid textbox to a StringOrNone object
            // Note: does _not_ apply if user comes direct from NoneOptionListBoxTypeEditor which means
            // we can tell the difference between a text entry "(None)" and the TypeEditor '(None)'
            // by always returning a non-NoneValue StringOrNone here
            if (value is string stringValue)
            {
                return new StringOrNone(stringValue);
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
            StringOrNone[] standardValues = { StringOrNone.NoneValue };
            return new StandardValuesCollection(standardValues);
        }
    }
}
