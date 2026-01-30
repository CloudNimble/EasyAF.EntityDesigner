// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Designer;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerDiagrams : EntityDesignExplorerEFElement
    {
        // Ghost nodes are grouping nodes in the EDM Browser which 
        // do not correspond to any underlying element in the model
        protected ExplorerTypes _typesGhostNode;

        private readonly TypedChildList<ExplorerDiagram> _diagrams = new TypedChildList<ExplorerDiagram>();

        public ExplorerDiagrams(EditingContext context, Diagrams diagrams, ExplorerEFElement parent)
            : base(context, diagrams, parent)
        {
            var name = Resources.DiagramTypesGhostNodeName;
            base.Name = name;

            _typesGhostNode = new ExplorerTypes(name, context, this);
        }

        public IList<ExplorerDiagram> Diagrams
        {
            get { return _diagrams.ChildList; }
        }

        public ExplorerTypes Types
        {
            get { return _typesGhostNode; }
        }

        private void LoadDiagramsFromModel()
        {
            // load children from model
            if (ModelItem is Diagrams diagrams)
            {
                foreach (var child in diagrams.Items)
                {
                    _diagrams.Insert(
                        (ExplorerDiagram)ModelToExplorerModelXRef.GetNewOrExisting(_context, child, this, typeof(ExplorerDiagram)));
                }
            }
        }

        protected override void LoadChildrenFromModel()
        {
            LoadDiagramsFromModel();
        }

        protected override void LoadWpfChildrenCollection()
        {
            _children.Clear();
            foreach (var child in Diagrams)
            {
                _children.Add(child);
            }
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is Diagram diagram)
            {
                var explorerDiagram = AddDiagram(diagram);
                var index = _diagrams.IndexOf(explorerDiagram);
                _children.Insert(index, explorerDiagram);
            }
            else
            {
                base.InsertChild(efElementToInsert);
            }
        }

        protected override bool RemoveChild(ExplorerEFElement efElementToRemove)
        {
            if (efElementToRemove is not ExplorerDiagram explorerDiagram)
            {
                Debug.Fail(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.BadRemoveBadChildType,
                        efElementToRemove.GetType().FullName,
                        Name,
                        GetType().FullName));

                return false;
            }

            var indexOfRemovedChild = _diagrams.Remove(explorerDiagram);
            return (indexOfRemovedChild < 0) ? false : true;
        }

        private ExplorerDiagram AddDiagram(Diagram diagram)
        {
            ExplorerDiagram explorerDiagram = ModelToExplorerModelXRef.GetNew(_context, diagram, this, typeof(ExplorerDiagram)) as ExplorerDiagram;
            _diagrams.Insert(explorerDiagram);
            return explorerDiagram;
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "FolderPngIcon"; }
        }
    }
}