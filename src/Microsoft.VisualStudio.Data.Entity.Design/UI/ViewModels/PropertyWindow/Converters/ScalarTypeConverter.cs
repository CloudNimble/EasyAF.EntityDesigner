// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.UI.ViewModels.PropertyWindow.Descriptors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    // TODO: consider merging with ConceptualPropertyTypeConverter - code in both classes seems very similar if not identical
    internal class ScalarTypeConverter<TElementDescriptor, TEFElement> : StringConverter
        where TElementDescriptor : ElementDescriptor<TEFElement>
        where TEFElement : EFElement
    {
        private HashSet<string> _typeList;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            PopulateTypeList(context);
            return new StandardValuesCollection(_typeList.ToList());
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            PopulateTypeList(context);
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            PopulateTypeList(context);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        private void PopulateTypeList(ITypeDescriptorContext context)
        {
            _typeList = [];

            if (context != null)
            {
                if (context.Instance is TElementDescriptor elementDescriptor
                    && elementDescriptor.TypedEFElement != null)
                {
                    var artifact = elementDescriptor.TypedEFElement.Artifact;
                    Debug.Assert(artifact != null, "Unable to find artifact.");
                    if (artifact != null)
                    {
                        foreach (var primType in ModelHelper.AllPrimitiveTypesSorted(artifact.SchemaVersion))
                        {
                            _typeList.Add(primType);
                        }
                    }

                    ConceptualEntityModel conceptualModel =
                        (ConceptualEntityModel)elementDescriptor.TypedEFElement.GetParentOfType(typeof(ConceptualEntityModel));
                    Debug.Assert(conceptualModel != null, "Unable to find conceptual model.");
                    if (conceptualModel != null)
                    {
                        foreach (var enumType in conceptualModel.EnumTypes())
                        {
                            _typeList.Add(enumType.NormalizedNameExternal);
                        }
                    }
                }
            }
        }
    }
}
