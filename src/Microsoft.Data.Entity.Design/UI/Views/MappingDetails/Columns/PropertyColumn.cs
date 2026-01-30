// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Base.Shell;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Associations;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.FunctionImports;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Functions;

namespace Microsoft.Data.Entity.Design.UI.Views.MappingDetails.Columns
{
    internal class PropertyColumn : BaseColumn
    {
        public PropertyColumn()
            : base(Resources.MappingDetails_Property)
        {
        }

        internal override object GetInPlaceEdit(object component, ref string alternateText)
        {
            EnsureTypeConverters(component as MappingEFElement);
            return base.GetInPlaceEdit(component, ref alternateText);
        }

        // <summary>
        //     Overriding this allows the list-of-values dropdowns to use
        //     the converter to convert back from a string to an object (in our
        //     case a MappingLovEFElement object)
        // </summary>
        public override Type /* PropertyDescriptor */ PropertyType
        {
            get { return typeof(PropertyColumnConverter); }
        }

        public override object /* PropertyDescriptor */ GetValue(object component)
        {
            if (component is MappingFunctionScalarProperty mfsp)
            {
                EnsureTypeConverters(mfsp);
                return new MappingLovEFElement(mfsp, mfsp.Value);
            }

            if (component is MappingResultBinding mrb)
            {
                EnsureTypeConverters(mrb);
                return new MappingLovEFElement(mrb, mrb.Value);
            }

            if (component is MappingFunctionImport mfi)
            {
                EnsureTypeConverters(mfi);
                return new MappingLovEFElement(mfi, mfi.FunctionName);
            }

            if (component is MappingEFElement elem)
            {
                EnsureTypeConverters(elem);
                return new MappingLovEFElement(elem, elem.Name);
            }

            return MappingEFElement.LovBlankPlaceHolder;
        }

        internal override bool IsDeleteSupported(object component)
        {
            if (component is MappingAssociationSet
                || component is MappingFunctionScalarProperty
                || component is MappingResultBinding)
            {
                MappingEFElement mappingElement = component as MappingEFElement;
                Debug.Assert(mappingElement != null, "The component should be a MappingEFElement");
                if (mappingElement.ModelItem != null)
                {
                    return true;
                }
            }
            return false;
        }

        // This method receives changes as MappingLovElements (user used the mouse)
        // or as strings (user used the keyboard)
        public override void SetValue(object component, object value)
        {
            // the user clicked off of the drop-down without choosing a value
            if (value == null
                || MappingEFElement.LovBlankPlaceHolder == value)
            {
                return;
            }

            // if they picked on the "Empty" placeholder, ignore it
            if (MappingEFElement.LovEmptyPlaceHolder == value)
            {
                return;
            }

            MappingLovEFElement lovElement = value as MappingLovEFElement;
            var valueAsString = value as string;
            Debug.Assert(
                lovElement != null || valueAsString != null,
                "value is not a MappingLovEFElement nor a string. Actual type is " + value.GetType().FullName);
            if (lovElement == null
                && string.IsNullOrEmpty(valueAsString))
            {
                // Both null is an error condition and should not happen.
                // But the trid will sometimes send an empty string when the user drops down a list
                // and then clicks away; in both cases just return without doing anything.
                return;
            }

            if (component is MappingAssociationSet mas)
            {
                lovElement = mas.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.FirstColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    mas.Delete(null);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.DeleteItemArgs));
                }
                else if (MappingEFElement.LovEmptyPlaceHolder == lovElement)
                {
                    return;
                }
                else
                {
                    EntityType et = lovElement.ModelElement as EntityType;
                    Debug.Assert(
                        et != null, "component is a MappingAssociationSet but value.ModelElement is of type " + value.GetType().FullName);

                    if (mas.AssociationSet.AssociationSetMapping == null)
                    {
                        // this row is being created via the creator node
                        mas.CreateModelItem(null, Host.Context, et);
                        OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.InsertItemArgs));
                    }
                    else
                    {
                        // they switched to a different table so delete the old
                        // underlying model item and create a new one
                        CommandProcessorContext cpc = new CommandProcessorContext(
                            Host.Context, EfiTransactionOriginator.MappingDetailsOriginatorId, Resources.Tx_UpdateMappingFragment);
                        mas.SwitchModelItem(cpc, Host.Context, et, true);
                        OnValueChanged(this, new ColumnValueChangedEventArgs(new TreeGridDesignerBranchChangedArgs()));
                    }
                }

                return;
            }

            if (component is MappingFunctionScalarProperty mfsp)
            {
                lovElement = mfsp.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.ThirdColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    // don't delete the view model row since we want one for every parameter
                    mfsp.DeleteModelItem(null);
                }
                else if (MappingEFElement.LovEmptyPlaceHolder == lovElement)
                {
                    return;
                }
                else
                {
                    // Note: properties (even simple ScalarProperties that are not part of a ComplexProperty) are
                    //       added to lovElement.Object as a List<Property> whereas properties from the other end
                    //       of a NavigationProperty are added as a Property (see 
                    //       MappingFunctionScalarProperty.GetListOfValues()).
                    if (lovElement.Object is List<Property> propertiesChain)
                    {
                        // there is no NavProp involved - clear out any old one
                        mfsp.SetNavigationProperty(null);

                        // create or update scalar property
                        mfsp.CreateOrUpdateModelItem(Host.Context, propertiesChain);
                    }
                    else
                    {
                        Property property = lovElement.ModelElement as Property;

                        Debug.Assert(
                            property != null,
                            "component is a " + component.GetType().Name + " but value.ModelElement is of type "
                            + lovElement.ModelElement.GetType().FullName);
                        if (property != null)
                        {
                            // if the element's display name consists of "NavProp.Property" then figure out what
                            // the navigation property is
                            if (!String.IsNullOrEmpty(lovElement.DisplayName))
                            {
                                NavigationProperty navProp = null;
                                var dotSeparatorIndex = lovElement.DisplayName.IndexOf(Symbol.NORMALIZED_NAME_SEPARATOR_FOR_DISPLAY);
                                if (dotSeparatorIndex == -1)
                                {
                                    Debug.Fail(
                                        "For MappingFunctionScalarProperty component, lovElement is of type Property. This should indicate that the property was"
                                        +
                                        " reached via a NavigationProperty but the DisplayName (" + lovElement.DisplayName
                                        + ") does not have the right format.");
                                }
                                else
                                {
                                    var navPropRefName = lovElement.DisplayName.Substring(0, dotSeparatorIndex);
                                    Debug.Assert(
                                        mfsp.MappingFunctionEntityType != null,
                                        "Trying to get the navigation property, but where is the mapping function entity type tied to this mapping function scalar property?");
                                    Debug.Assert(
                                        mfsp.MappingFunctionEntityType.EntityType != null,
                                        "Trying to get the navigation property, but where is the entity type represented by the mapping function entity type?");

                                    if (mfsp.MappingFunctionEntityType != null
                                        && mfsp.MappingFunctionEntityType.EntityType != null)
                                    {
                                        ConceptualEntityType cet = mfsp.MappingFunctionEntityType.EntityType as ConceptualEntityType;
                                        Debug.Assert(cet != null, "EntityType is not ConceptualEntityType");
                                        navProp = ModelHelper.FindNavigationPropertyByName(cet, navPropRefName);
                                        Debug.Assert(
                                            navProp != null,
                                            ""
                                            + String.Format(
                                                CultureInfo.CurrentCulture,
                                                "Could not find the navigation property {0} in the entity type {1}", navPropRefName,
                                                mfsp.MappingFunctionEntityType.EntityType.ToPrettyString()));
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                mfsp.SetNavigationProperty(navProp);
                            }

                            // create or update the item dependent on whether it already exists in the model
                            List<Property> propChain = new List<Property>(1)
                            {
                                property
                            };
                            mfsp.CreateOrUpdateModelItem(Host.Context, propChain);
                        }
                    }
                }

                OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                return;
            }

            if (component is MappingResultBinding mrb)
            {
                lovElement = mrb.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.ThirdColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    mrb.Delete(null);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.DeleteItemArgs));
                }
                else if (MappingEFElement.LovEmptyPlaceHolder == lovElement)
                {
                    return;
                }
                else
                {
                    Property prop = lovElement.ModelElement as Property;
                    Debug.Assert(
                        prop != null,
                        "component is a " + component.GetType().Name + " but value.ModelElement is of type " + value.GetType().FullName);
                    Debug.Assert(mrb.ModelItem != null, "MappingResultBinding should not have null ModelItem");
                    mrb.SetProperty(prop);
                }

                OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                return;
            }

            if (component is MappingFunctionImport mfi)
            {
                lovElement = mfi.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.FirstColumn);
                if (lovElement != null)
                {
                    if (lovElement.ModelElement is Function function)
                    {
                        mfi.ChangeModelItem(Host.Context, function);
                        OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    }
                }
                return;
            }
        }

        internal override TreeGridDesignerValueSupportedStates GetValueSupported(object component)
        {
            if (component is MappingAssociationSetEnd
                || component is MappingEndScalarProperty
                || component is MappingFunctionEntityType
                || component is MappingModificationFunctionMapping
                || component is MappingFunctionScalarProperties
                || component is MappingResultBindings
                || component is MappingFunctionImportScalarProperty)
            {
                return TreeGridDesignerValueSupportedStates.None;
            }
            else if (component is MappingFunctionScalarProperty mfsp)
            {
                if (null != mfsp.StoreParameter
                    && Parameter.InOutMode.Out == mfsp.StoreParameter.InOut)
                {
                    // do not support mapping to Out parameters
                    return TreeGridDesignerValueSupportedStates.None;
                }
            }

            return base.GetValueSupported(component);
        }

        internal override void EnsureTypeConverters(MappingEFElement element)
        {
            if (_converter == null
                || _currentElement != element)
            {
                // create initial type converter
                _currentElement = element;
                _converter = new PropertyColumnConverter();
            }
        }
    }

    internal class PropertyColumnConverter : BaseColumnConverter<PropertyColumn>
    {
        protected override void PopulateMapping(ITypeDescriptorContext context)
        {
            if (context != null)
            {
                _context = context;
            }

            Debug.Assert(_context != null, "Should have a context for the PopulateMapping call.");

            PopulateMappingForSelectedObject(_context.PropertyDescriptor as PropertyColumn);
        }

        protected override void PopulateMappingForSelectedObject(PropertyColumn selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null
                && selectedObject.Element != null)
            {
                if (selectedObject.Element is MappingAssociationSet
                    || selectedObject.Element is MappingFunctionImport)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.FirstColumn);
                    foreach (var key in lov.Keys)
                    {
                        AddMapping(key, lov[key]);
                    }
                    return;
                }

                if (selectedObject.Element is MappingFunctionScalarProperty
                    || selectedObject.Element is MappingResultBinding)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.ThirdColumn);
                    foreach (var key in lov.Keys)
                    {
                        AddMapping(key, lov[key]);
                    }
                    return;
                }
            }
        }
    }
}