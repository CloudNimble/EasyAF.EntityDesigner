// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerFunctionImport : EntityDesignExplorerEFElement
    {
        private readonly TypedChildList<ExplorerParameter> _parameters =
            new TypedChildList<ExplorerParameter>();

        public ExplorerFunctionImport(EditingContext context, FunctionImport functionImport, ExplorerEFElement parent)
            : base(context, functionImport, parent)
        {
            // do nothing
        }

        public IList<ExplorerParameter> Parameters
        {
            get { return _parameters.ChildList; }
        }

        private void LoadParametersFromModel()
        {
            // load children from model
            FunctionImport functionImport = ModelItem as FunctionImport;
            Debug.Assert(functionImport != null, "Underlying FunctionImport is null for ExplorerFunctionImport with name " + Name);
            if (functionImport != null)
            {
                foreach (var child in functionImport.Parameters())
                {
                    _parameters.Insert(
                        (ExplorerParameter)
                        ModelToExplorerModelXRef.GetNewOrExisting(_context, child, this, typeof(ExplorerParameter)));
                }
            }
        }

        protected override void LoadChildrenFromModel()
        {
            LoadParametersFromModel();
        }

        protected override void LoadWpfChildrenCollection()
        {
            _children.Clear();

            foreach (var child in Parameters)
            {
                _children.Add(child);
            }
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is Parameter param)
            {
                var explorerParam = AddParameter(param);
                var index = _parameters.IndexOf(explorerParam);
                _children.Insert(index, explorerParam);
            }
            else
            {
                base.InsertChild(efElementToInsert);
            }
        }

        protected override bool RemoveChild(ExplorerEFElement efElementToRemove)
        {
            if (efElementToRemove is not ExplorerParameter explorerParameter)
            {
                Debug.Fail(
                    string.Format(
                        CultureInfo.CurrentCulture, Resources.BadRemoveBadChildType,
                        efElementToRemove.GetType().FullName, Name, GetType().FullName));
                return false;
            }

            var indexOfRemovedChild = _parameters.Remove(explorerParameter);
            return (indexOfRemovedChild < 0) ? false : true;
        }

        private ExplorerParameter AddParameter(Parameter param)
        {
            ExplorerParameter explorerParam =
                ModelToExplorerModelXRef.GetNew(_context, param, this, typeof(ExplorerParameter)) as ExplorerParameter;
            _parameters.Insert(explorerParam);
            return explorerParam;
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "FunctionImportPngIcon"; }
        }
    }
}