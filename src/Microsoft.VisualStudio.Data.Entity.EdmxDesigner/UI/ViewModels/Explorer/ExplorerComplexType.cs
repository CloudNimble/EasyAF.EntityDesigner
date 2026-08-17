// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    internal class ExplorerComplexType : EntityDesignExplorerEFElement
    {
        private readonly TypedChildList<ExplorerProperty> _properties = new TypedChildList<ExplorerProperty>();

        public ExplorerComplexType(EditingContext context, ComplexType complexType, ExplorerEFElement parent)
            : base(context, complexType, parent)
        {
            // do nothing
        }

        public IList<ExplorerProperty> Properties
        {
            get { return _properties.ChildList; }
        }

        private void LoadPropertiesFromModel()
        {
            // load Properties from model
            ComplexType complexType = ModelItem as ComplexType;
            Debug.Assert(
                complexType != null, "Underlying ComplexType is null calculating Properties for ExplorerComplexType with name " + Name);
            if (complexType != null)
            {
                foreach (var child in complexType.Properties())
                {
                    _properties.Insert(
                        (ExplorerConceptualProperty)
                        ModelToExplorerModelXRef.GetNewOrExisting(_context, child, this, typeof(ExplorerConceptualProperty)));
                }
            }
        }

        protected override void LoadChildrenFromModel()
        {
            LoadPropertiesFromModel();
        }

        protected override void LoadWpfChildrenCollection()
        {
            _children.Clear();
            foreach (var property in Properties)
            {
                _children.Add(property);
            }
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is Property prop)
            {
                var explorerProp = AddProperty(prop);
                var index = _properties.IndexOf(explorerProp);
                _children.Insert(index, explorerProp);
                return;
            }

            base.InsertChild(efElementToInsert);
        }

        protected override bool RemoveChild(ExplorerEFElement efElementToRemove)
        {
            if (efElementToRemove is ExplorerProperty explorerProperty)
            {
                var indexOfRemovedChild = _properties.Remove(explorerProperty);
                return (indexOfRemovedChild < 0) ? false : true;
            }

            Debug.Fail(
                string.Format(
                    CultureInfo.CurrentCulture, EdmxDesignerResources.BadRemoveBadChildType,
                    efElementToRemove.GetType().FullName, Name, GetType().FullName));
            return false;
        }

        private ExplorerProperty AddProperty(Property prop)
        {
            ExplorerProperty explorerProp = null;
            explorerProp =
                ModelToExplorerModelXRef.GetNew(_context, prop, this, typeof(ExplorerConceptualProperty)) as ExplorerConceptualProperty;
            _properties.Insert(explorerProp);
            return explorerProp;
        }

        // the name of Complex Types are editable inline in the Explorer
        public override bool IsEditableInline
        {
            get { return true; }
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "ComplexTypePngIcon"; }
        }
    }
}