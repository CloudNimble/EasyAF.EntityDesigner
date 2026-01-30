// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerEnumTypes : EntityDesignExplorerEFElement
    {
        private readonly TypedChildList<ExplorerEnumType> _enumTypes = new TypedChildList<ExplorerEnumType>();

        public ExplorerEnumTypes(string name, EditingContext context, ExplorerEFElement parent)
            : base(context, null, parent)
        {
            if (name != null)
            {
                base.Name = name;
            }
        }

        public IList<ExplorerEnumType> EnumTypes
        {
            get { return _enumTypes.ChildList; }
        }

        private void LoadEnumTypesFromModel()
        {
            // load children from model
            // note: have to go to parent to get this as this is a dummy node
            if (Parent.ModelItem is ConceptualEntityModel entityModel)
            {
                foreach (var child in entityModel.EnumTypes())
                {
                    _enumTypes.Insert(
                        (ExplorerEnumType)
                        ModelToExplorerModelXRef.GetNewOrExisting(_context, child, this, typeof(ExplorerEnumType)));
                }
            }
        }

        protected override void LoadChildrenFromModel()
        {
            LoadEnumTypesFromModel();
        }

        protected override void LoadWpfChildrenCollection()
        {
            _children.Clear();
            foreach (var child in EnumTypes)
            {
                _children.Add(child);
            }
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is EnumType enumType)
            {
                var explorerEnumType = AddEnumType(enumType);
                var index = _enumTypes.IndexOf(explorerEnumType);
                _children.Insert(index, explorerEnumType);
            }
            else
            {
                base.InsertChild(efElementToInsert);
            }
        }

        protected override bool RemoveChild(ExplorerEFElement efElementToRemove)
        {
            if (efElementToRemove is not ExplorerEnumType explorerEnumType)
            {
                Debug.Fail(
                    string.Format(
                        CultureInfo.CurrentCulture, Resources.BadRemoveBadChildType,
                        efElementToRemove.GetType().FullName, Name, GetType().FullName));
                return false;
            }

            var indexOfRemovedChild = _enumTypes.Remove(explorerEnumType);
            return (indexOfRemovedChild < 0) ? false : true;
        }

        private ExplorerEnumType AddEnumType(EnumType enumType)
        {
            ExplorerEnumType explorerEnumType = ModelToExplorerModelXRef.GetNew(_context, enumType, this, typeof(ExplorerEnumType)) as ExplorerEnumType;
            _enumTypes.Insert(explorerEnumType);
            return explorerEnumType;
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "FolderPngIcon"; }
        }
    }
}