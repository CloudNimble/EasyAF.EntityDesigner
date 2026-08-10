// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class ReferentialConstraintConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (context.Instance is EFAssociationDescriptor desc)
                {
                    if (desc.WrappedItem is Association assoc)
                    {
                        // make sure that we can resolve basic stuff about the constraint
                        if (assoc.ReferentialConstraint == null
                            || assoc.ReferentialConstraint.Principal == null
                            || assoc.ReferentialConstraint.Principal.Role.Target == null
                            || assoc.ReferentialConstraint.Dependent == null
                            || assoc.ReferentialConstraint.Dependent.Role.Target == null)
                        {
                            return string.Empty;
                        }
                        else
                        {
                            // use the type names unless this is a self association (end types are the same)
                            var principalEnd = assoc.ReferentialConstraint.Principal.Role.Target;
                            var dependentEnd = assoc.ReferentialConstraint.Dependent.Role.Target;
                            if (principalEnd.Type.Target != null
                                &&
                                dependentEnd.Type.Target != null
                                &&
                                principalEnd.Type.Target != dependentEnd.Type.Target)
                            {
                                var priType = principalEnd.Type.Target.LocalName.Value;
                                var depType = dependentEnd.Type.Target.LocalName.Value;
                                return string.Format(CultureInfo.CurrentCulture, "{0} -> {1}", priType, depType);
                            }
                            else
                            {
                                // self association, so use role names
                                var priRole = assoc.ReferentialConstraint.Principal.Role.RefName;
                                var depRole = assoc.ReferentialConstraint.Dependent.Role.RefName;
                                return string.Format(CultureInfo.CurrentCulture, "{0} -> {1}", priRole, depRole);
                            }
                        }
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
