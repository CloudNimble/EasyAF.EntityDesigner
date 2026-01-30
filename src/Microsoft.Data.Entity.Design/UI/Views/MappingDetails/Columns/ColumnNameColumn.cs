// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Base.Shell;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Associations;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.FunctionImports;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Tables;

namespace Microsoft.Data.Entity.Design.UI.Views.MappingDetails.Columns
{
    // <summary>
    //     Based on the type of item being shown, show the correct text for the Column Name column.
    // </summary>
    internal class ColumnNameColumn : BaseColumn
    {
        public ColumnNameColumn()
            : base(Resources.MappingDetails_ColumnName)
        {
        }

        internal override object GetInPlaceEdit(object component, ref string alternateText)
        {
            EnsureTypeConverters(component as MappingEFElement);
            if (component is MappingFunctionImportScalarProperty mfisp
                && mfisp.ScalarProperty == null)
            {
                // this will clear the gray text from the column when it enters an edit mode
                alternateText = String.Empty;
            }
            return base.GetInPlaceEdit(component, ref alternateText);
        }

        public override object /* PropertyDescriptor */ GetValue(object component)
        {
            if (component is MappingEndScalarProperty mesp)
            {
                EnsureTypeConverters(mesp);
                return new MappingLovEFElement(mesp, mesp.Value);
            }

            if (component is MappingFunctionImportScalarProperty mfisp)
            {
                EnsureTypeConverters(mfisp);
                return new MappingLovEFElement(mfisp, mfisp.ColumnName);
            }

            if (component is MappingFunctionImport mfi)
            {
                return MappingEFElement.LovBlankPlaceHolder;
            }

            if (component is MappingEFElement elem)
            {
                EnsureTypeConverters(elem);
                return new MappingLovEFElement(elem, elem.Name);
            }

            return MappingEFElement.LovBlankPlaceHolder;
        }

        // <summary>
        //     Overriding this allows the list-of-values dropdowns to use
        //     the converter to convert back from a string to an object (in our
        //     case a MappingLovEFElement object)
        // </summary>
        public override Type /* PropertyDescriptor */ PropertyType
        {
            get { return typeof(ColumnNameColumnConverter); }
        }

        internal override bool IsDeleteSupported(object component)
        {
            if (component is MappingStorageEntityType
                || component is MappingCondition
                || component is MappingEndScalarProperty
                || component is MappingFunctionImportScalarProperty)
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
            // if they picked on the "Empty" placeholder, ignore it
            var valueAsString = value as string;
            if ((valueAsString != null && MappingEFElement.LovEmptyPlaceHolder.DisplayName == valueAsString)
                || MappingEFElement.LovEmptyPlaceHolder == value)
            {
                return;
            }

            // if we get a blank, that is never valid
            if (value == null
                || MappingEFElement.LovBlankPlaceHolder == value)
            {
                return;
            }

            MappingLovEFElement lovElement = value as MappingLovEFElement;
            if (lovElement == null
                && valueAsString == null)
            {
                Debug.Fail(
                    "value is not a MappingLovEFElement nor a string. Actual type is " + value.GetType().FullName);
                return;
            }

            if (component is MappingFunctionImportScalarProperty mfisp)
            {
                if (mfisp.ModelItem != null)
                {
                    if (string.IsNullOrEmpty(valueAsString))
                    {
                        // they cleared out the columnName field, so delete it
                        mfisp.DeleteModelItem(null);
                        OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    }
                    else
                    {
                        // they just changed the columnName, just update it in place
                        mfisp.ColumnName = valueAsString;
                        OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(valueAsString) == false)
                    {
                        // a brand new one, so create it
                        mfisp.CreateModelItem(null, Host.Context, valueAsString);
                        OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    }
                }
                return;
            }

            // the trid will sometimes send an empty string when the user drops down a list
            // and then clicks away; if this happens just return without doing anything.
            // Note: _very_ important not to turn this into a string.IsNullOrEmpty() check.
            // If you do and a MappingLovElement is passed to this method everything below this
            // if() will not be executed.
            if (string.Empty == valueAsString)
            {
                return;
            }

            if (component is MappingStorageEntityType mset)
            {
                // if value is a string then we've called SetValue after an edit using keyboard
                // so lookup correct MappingLovElement and use that going forward
                lovElement = mset.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.FirstColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    mset.Delete(null);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.DeleteItemArgs));
                    return;
                }

                EntityType et = lovElement.ModelElement as EntityType;
                Debug.Assert(
                    et != null, "component is a MappingStorageEntityType but value.ModelElement is of type " + value.GetType().FullName);

                if (mset.ModelItem == null)
                {
                    // this row is being created via the creator node
                    mset.CreateModelItem(null, Host.Context, et);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.InsertItemArgs));
                }
                else
                {
                    // they switched to a different table so delete the old
                    // underlying model item and create a new one
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        Host.Context, EfiTransactionOriginator.MappingDetailsOriginatorId, Resources.Tx_UpdateMappingFragment);
                    mset.SwitchModelItem(cpc, Host.Context, et, false);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(new TreeGridDesignerBranchChangedArgs()));
                }
                return;
            }

            if (component is MappingCondition mc)
            {
                // if value is a string then we've called SetValue after an edit using keyboard
                // so lookup correct MappingLovElement and use that going forward
                lovElement = mc.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.FirstColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    mc.Delete(null);
                    OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.DeleteItemArgs));
                    return;
                }

                Property property = lovElement.ModelElement as Property;
                Debug.Assert(
                    property != null, "component is a MappingCondition but value.ModelElement is of type " + value.GetType().FullName);

                if (property != null)
                {
                    if (mc.ModelItem == null)
                    {
                        // this row is being created via the creator node
                        mc.CreateModelItem(null, Host.Context, property);
                        OnValueChanged(this, new ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs.InsertItemArgs));
                    }
                    else
                    {
                        // the condition already exists, just update the column being used
                        mc.ColumnName = property.LocalName.Value;
                        OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    }
                }

                return;
            }

            // remember that the columns are reversed for association mappings, so this is column 3
            // if its an end scalar property
            if (component is MappingEndScalarProperty mesp)
            {
                // if value is a string then we've called SetValue after an edit using keyboard
                // so lookup correct MappingLovElement and use that going forward
                lovElement = mesp.GetLovElementFromLovElementOrString(lovElement, valueAsString, ListOfValuesCollection.ThirdColumn);
                if (lovElement == null)
                {
                    return;
                }

                if (MappingEFElement.LovDeletePlaceHolder == lovElement)
                {
                    // they selected the delete me row, so clear out the item
                    // don't delete the view model row since we want one for every key column
                    mesp.DeleteModelItem(null);
                    OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    return;
                }

                Property property = lovElement.ModelElement as Property;
                Debug.Assert(
                    property != null,
                    "component is a MappingEndScalarProperty but value.ModelElement is of type " + value.GetType().FullName);

                if (property != null)
                {
                    if (mesp.ModelItem == null)
                    {
                        // this means that this is a line where the key column has not been mapped yet,
                        // so create a new scalar property
                        mesp.CreateModelItem(null, Host.Context, property);
                    }
                    else
                    {
                        // the scalar property already exists, they are changing which column
                        mesp.ColumnName = property.LocalName.Value;
                    }
                    OnValueChanged(this, ColumnValueChangedEventArgs.Default);
                    return;
                }
            }
        }

        internal override TreeGridDesignerValueSupportedStates GetValueSupported(object component)
        {
            if (component is MappingColumnMappings
                || component is MappingConceptualEntityType
                || component is MappingScalarProperty
                || component is MappingAssociationSet
                || component is MappingAssociationSetEnd
                || component is MappingFunctionImport)
            {
                return TreeGridDesignerValueSupportedStates.None;
            }
            else if (component is MappingEndScalarProperty)
            {
                MappingEndScalarProperty mesp = component as MappingEndScalarProperty;
                if (mesp.MappingAssociationSet.AssociationSet.AssociationSetMapping == null)
                {
                    return TreeGridDesignerValueSupportedStates.None;
                }
                else
                {
                    return base.GetValueSupported(component);
                }
            }
            else if (component is MappingFunctionImportScalarProperty)
            {
                MappingFunctionImportScalarProperty mfisp = component as MappingFunctionImportScalarProperty;
                if (mfisp.IsComplexProperty)
                {
                    return TreeGridDesignerValueSupportedStates.None;
                }
                else
                {
                    return base.GetValueSupported(component);
                }
            }
            else
            {
                return base.GetValueSupported(component);
            }
        }

        internal override void EnsureTypeConverters(MappingEFElement element)
        {
            if (_converter == null
                || _currentElement != element)
            {
                // create initial type converter
                _currentElement = element;
                if (element is MappingFunctionImportScalarProperty)
                {
                    _converter = new NoDropDownColumnNameColumnConverter();
                }
                else
                {
                    _converter = new ColumnNameColumnConverter();
                }
            }
        }
    }

    internal class ColumnNameColumnConverter : BaseColumnConverter<ColumnNameColumn>
    {
        protected override void PopulateMapping(ITypeDescriptorContext context)
        {
            if (context != null)
            {
                _context = context;
            }

            Debug.Assert(_context != null, "Should have a context for the PopulateMapping call.");

            PopulateMappingForSelectedObject(_context.PropertyDescriptor as ColumnNameColumn);
        }

        protected override void PopulateMappingForSelectedObject(ColumnNameColumn selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null
                && selectedObject.Element != null)
            {
                if (selectedObject.Element is MappingCondition
                    || selectedObject.Element is MappingStorageEntityType)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.FirstColumn);
                    foreach (var key in lov.Keys)
                    {
                        AddMapping(key, lov[key]);
                    }
                    return;
                }

                if (selectedObject.Element is MappingEndScalarProperty)
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

    // <summary>
    //     Used to override conversion for the case where the ColumnNameColumn is representing
    //     a MappingFunctionImportScalarProperty
    // </summary>
    internal class NoDropDownColumnNameColumnConverter : ColumnNameColumnConverter
    {
        // needs to return false so as to provide ordinary Textbox for editing
        // (instead of drop-down) see TreeGridDesignerTreeControl.CreateTypeEditorHost()
        public override bool /* TypeConverter */ GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }
}