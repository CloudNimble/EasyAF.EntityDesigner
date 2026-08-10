// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerFunction : EntityDesignExplorerEFElement
    {
        private readonly TypedChildList<ExplorerParameter> _parameters =
            new TypedChildList<ExplorerParameter>();

        public ExplorerFunction(EditingContext context, Function func, ExplorerEFElement parent)
            : base(context, func, parent)
        {
            // do nothing
        }

        public IList<ExplorerParameter> Parameters
        {
            get { return _parameters.ChildList; }
        }

        internal bool IsComposable
        {
            get
            {
                if (ModelItem is Function func)
                {
                    return func.IsComposable.Value;
                }

                return true; // assume the worst, that this can't be a function import
            }
        }

        private void LoadParametersFromModel()
        {
            // load children from model
            Function function = ModelItem as Function;
            Debug.Assert(function != null, "Underlying Function is null for ExplorerFunction with name " + Name);
            if (function != null)
            {
                foreach (var child in function.Parameters())
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

        internal override string ExplorerImageResourceKeyName
        {
            get { return "SprocPngIcon"; }
        }
    }
}
