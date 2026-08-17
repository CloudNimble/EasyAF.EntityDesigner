// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    // <summary>
    //     Abstract base class for all Mapping XyzColumn classes.
    // </summary>
    internal abstract class BaseColumn : TreeGridDesignerColumnDescriptor
    {
        internal TypeConverter _converter;
        internal MappingEFElement _currentElement; // cached element, used to know whether we need to recreate the type converters

        protected BaseColumn(string name)
            : base(name)
        {
        }

        protected override void OnValueChanged(object component, EventArgs e)
        {
            base.OnValueChanged(component, e);

            if (e is ColumnValueChangedEventArgs columnArgs)
            {
                BaseColumn column = component as BaseColumn;

                MappingDetailsWindow mappingDetailsWindow = column.Host as MappingDetailsWindow;
                Debug.Assert(mappingDetailsWindow != null, "MappingWindow is null");
                if (mappingDetailsWindow != null)
                {
                    // check if branch modifications are required
                    if (columnArgs.Args != null)
                    {
                        var treeItemInfo = mappingDetailsWindow.TreeControl.SelectedItemInfo;
                        if (treeItemInfo.Branch is ITreeGridDesignerBranch branch)
                        {
                            // need to set Row and Column values here
                            columnArgs.Args.Row = treeItemInfo.Row;
                            columnArgs.Args.Column = treeItemInfo.Column;
                            branch.OnColumnValueChanged(columnArgs.Args);
                            if (!columnArgs.Args.DeletingItem)
                            {
                                // expand added or changed branch here
                                mappingDetailsWindow.TreeControl.ExpandRecurse(mappingDetailsWindow.TreeControl.CurrentIndex, 0);
                            }
                        }
                    }

                    // calling this should null out the converter mappings and display an updated drop down
                    // whenever a descriptor value has changed.
                    IResettableConverter resettableConverter = _converter as IResettableConverter;
                    resettableConverter?.Reset();

                    // refreshes the property window
                    mappingDetailsWindow.UpdateSelection();
                }
            }
        }

        internal override void Delete(object component)
        {
            if (IsDeleteSupported(component))
            {
                SetValue(component, MappingEFElement.LovDeletePlaceHolder);
            }
        }

        internal abstract bool IsDeleteSupported(object component);

        internal MappingEFElement Element
        {
            get { return _currentElement; }
        }

        public override TypeConverter /* PropertyDescriptor */ Converter
        {
            get { return _converter != null ? _converter : base.Converter; }
        }

        internal abstract void EnsureTypeConverters(MappingEFElement element);

        public override Type /* PropertyDescriptor */ ComponentType
        {
            get { return typeof(MappingLovEFElement); }
        }
    }

}
