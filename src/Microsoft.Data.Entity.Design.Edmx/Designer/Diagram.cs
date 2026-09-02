// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{
    internal class Diagram : EFNameableItem, IDiagram
    {
        internal static readonly string ElementName = "Diagram";
        internal static readonly string AttributeZoomLevel = "ZoomLevel";
        internal static readonly string AttributeShowGrid = "ShowGrid";
        internal static readonly string AttributeSnapToGrid = "SnapToGrid";
        internal static readonly string AttributeDisplayType = "DisplayType";
        internal static readonly string AttributeId = "DiagramId";
        internal static readonly string AttributeLayoutMode = "LayoutMode";
        internal static readonly string AttributeConnectorMode = "ConnectorMode";
        internal static readonly string AttributeEnableGrouping = "EnableGrouping";
        internal static readonly string AttributeGenerateGroupNames = "GenerateGroupNames";
        internal static readonly string AttributeManualRouteRebakePolicy = "ManualRouteRebakePolicy";

        private readonly List<EntityTypeShape> _entityTypeShapes = [];
        private readonly List<AssociationConnector> _associationConnectors = [];
        private readonly List<InheritanceConnector> _inheritanceConnectors = [];
        private DefaultableValue<int> _zoomLevelAttr;
        private DefaultableValue<bool> _showGridAttr;
        private DefaultableValue<bool> _snapToGridAttr;
        private DefaultableValue<bool> _displayTypeAttr;
        private DefaultableValue<LayoutMode> _layoutModeAttr;
        private DefaultableValue<ConnectorMode> _connectorModeAttr;
        private DefaultableValue<bool> _enableGroupingAttr;
        private DefaultableValue<bool> _generateGroupNamesAttr;
        private DefaultableValue<RebakePolicy> _manualRouteRebakePolicyAttr;
        private DiagramIdDefaultableValue _id;

        internal Diagram(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        internal DefaultableValue<string> Id
        {
            get
            {
                _id ??= new DiagramIdDefaultableValue(this);
                return _id;
            }
        }

        #region IDiagram interface

        string IDiagram.Id
        {
            get { return Id.Value; }
        }

        string IDiagram.Name
        {
            get { return DisplayName; }
        }

        #endregion

        internal DefaultableValue<int> ZoomLevel
        {
            get
            {
                _zoomLevelAttr ??= new ZoomLevelDefaultableValue(this);
                return _zoomLevelAttr;
            }
        }

        internal DefaultableValue<bool> ShowGrid
        {
            get
            {
                _showGridAttr ??= new ShowGridDefaultableValue(this);
                return _showGridAttr;
            }
        }

        internal DefaultableValue<bool> SnapToGrid
        {
            get
            {
                _snapToGridAttr ??= new SnapToGridDefaultableValue(this);
                return _snapToGridAttr;
            }
        }

        /// <summary>
        ///     Which engine arranges this diagram.
        /// </summary>
        /// <remarks>
        ///     Absent from every file written before this attribute existed, so the default has to be
        ///     <see cref="Designer.LayoutMode.Legacy" /> for those to keep arranging the way they always have.
        /// </remarks>
        internal DefaultableValue<LayoutMode> LayoutMode
        {
            get
            {
                _layoutModeAttr ??= new LayoutModeDefaultableValue(this);
                return _layoutModeAttr;
            }
        }

        /// <summary>
        ///     How this diagram's connectors are drawn.
        /// </summary>
        /// <remarks>
        ///     Only meaningful alongside <see cref="Designer.LayoutMode.Modern" />. The legacy engine draws the only
        ///     connectors it knows how to draw, so <see cref="Designer.LayoutMode.Legacy" /> pairs with
        ///     <see cref="Designer.ConnectorMode.Legacy" /> and nothing else.
        /// </remarks>
        internal DefaultableValue<ConnectorMode> ConnectorMode
        {
            get
            {
                _connectorModeAttr ??= new ConnectorModeDefaultableValue(this);
                return _connectorModeAttr;
            }
        }

        /// <summary>
        ///     Whether a modern layout clusters shapes into groups.
        /// </summary>
        /// <remarks>
        ///     Only meaningful alongside <see cref="Designer.LayoutMode.Modern" />, and off by default: grouping
        ///     helps some diagrams and hurts others, so it is opt-in rather than imposed. See
        ///     specs/diagram-layout-engines.md.
        /// </remarks>
        internal DefaultableValue<bool> EnableGrouping
        {
            get
            {
                _enableGroupingAttr ??= new EnableGroupingDefaultableValue(this);
                return _enableGroupingAttr;
            }
        }

        /// <summary>
        ///     Whether a modern layout writes a detected group name onto a shape that has none.
        /// </summary>
        /// <remarks>
        ///     Off by default, and subordinate to <see cref="EnableGrouping" />: with grouping off nothing is
        ///     generated regardless of this value. See specs/diagram-layout-engines.md.
        /// </remarks>
        internal DefaultableValue<bool> GenerateGroupNames
        {
            get
            {
                _generateGroupNamesAttr ??= new GenerateGroupNamesDefaultableValue(this);
                return _generateGroupNamesAttr;
            }
        }

        /// <summary>
        ///     What a modern re-layout does to connectors a human has hand-routed.
        /// </summary>
        /// <remarks>
        ///     Defaults to <see cref="RebakePolicy.Ask" /> so nothing is discarded without the user's say-so. See
        ///     specs/diagram-layout-engines.md.
        /// </remarks>
        internal DefaultableValue<RebakePolicy> ManualRouteRebakePolicy
        {
            get
            {
                _manualRouteRebakePolicyAttr ??= new ManualRouteRebakePolicyDefaultableValue(this);
                return _manualRouteRebakePolicyAttr;
            }
        }

        internal DefaultableValue<bool> DisplayType
        {
            get
            {
                _displayTypeAttr ??= new DisplayTypeDefaultableValue(this);
                return _displayTypeAttr;
            }
        }

        /// <summary>
        ///     Return true if there is diagram object that represents efelement/efobject in the diagram.
        /// </summary>
        /// <param name="efObject"></param>
        /// <returns></returns>
        internal bool IsEFObjectRepresentedInDiagram(EFObject efObject)
        {
            List<EntityType> entityTypesInDiagram = EntityTypeShapes.Select(ets => ets.EntityType.Target).ToList();

            Association association = efObject.GetParentOfType(typeof(Association)) as Association;

            // if efobject is an associationset, check if the corresponding association is in the diagram.
            if (efObject is AssociationSet associationSet
                && associationSet.Association.Status == BindingStatus.Known)
            {
                association = associationSet.Association.Target;
            }

            // if efobject is an entity-set, Return true only if all the entity-types contained in the set are represented in the diagram.
            if (efObject is EntitySet entitySet)
            {
                foreach (var et in entitySet.GetEntityTypesInTheSet())
                {
                    if (entityTypesInDiagram.Contains(et) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (efObject.GetParentOfType(typeof(ConceptualEntityType)) is ConceptualEntityType entityType)
            {
                return entityTypesInDiagram.Contains(entityType);
            }
            else if (association != null)
            {
                List<Association> associationsInDiagram = AssociationConnectors.Select(a => a.Association.Target).ToList();
                return associationsInDiagram.Contains(association);
            }
            return false;
        }

        internal ICollection<EntityTypeShape> EntityTypeShapes
        {
            get { return _entityTypeShapes.AsReadOnly(); }
        }

        internal ICollection<AssociationConnector> AssociationConnectors
        {
            get { return _associationConnectors.AsReadOnly(); }
        }

        internal ICollection<InheritanceConnector> InheritanceConnectors
        {
            get { return _inheritanceConnectors.AsReadOnly(); }
        }

        internal void AddEntityTypeShape(EntityTypeShape shape)
        {
            _entityTypeShapes.Add(shape);
        }

        internal void AddAssociationConnector(AssociationConnector connector)
        {
            _associationConnectors.Add(connector);
        }

        internal void AddInheritanceConnector(InheritanceConnector connector)
        {
            _inheritanceConnectors.Add(connector);
        }

        #region overrides

        // we unfortunately get a warning from the compiler when we use the "base" keyword in "iterator" types generated by using the
        // "yield return" keyword.  By adding this method, I was able to get around this.  Unfortunately, I wasn't able to figure out
        // a way to implement this once and have derived classes share the implementation (since the "base" keyword is resolved at 
        // compile-time and not at runtime.
        private IEnumerable<EFObject> BaseChildren
        {
            get { return base.Children; }
        }

        internal override IEnumerable<EFObject> Children
        {
            get
            {
                foreach (var efobj in BaseChildren)
                {
                    yield return efobj;
                }

                foreach (EFObject efobj in _entityTypeShapes)
                {
                    yield return efobj;
                }

                foreach (EFObject efobj in _associationConnectors)
                {
                    yield return efobj;
                }

                foreach (EFObject efobj in _inheritanceConnectors)
                {
                    yield return efobj;
                }

                yield return ZoomLevel;
                yield return ShowGrid;
                yield return SnapToGrid;
                yield return DisplayType;
                yield return LayoutMode;
                yield return ConnectorMode;
                yield return EnableGrouping;
                yield return GenerateGroupNames;
                yield return ManualRouteRebakePolicy;
                yield return Id;
            }
        }

        protected override void OnChildDeleted(EFContainer efContainer)
        {
            if (efContainer is EntityTypeShape shape)
            {
                _entityTypeShapes.Remove(shape);
            }

            if (efContainer is AssociationConnector associationConnector)
            {
                _associationConnectors.Remove(associationConnector);
            }

            if (efContainer is InheritanceConnector inheritanceConnector)
            {
                _inheritanceConnectors.Remove(inheritanceConnector);
            }

            base.OnChildDeleted(efContainer);
        }

#if DEBUG
        internal override ICollection<string> MyAttributeNames()
        {
            var s = base.MyAttributeNames();
            s.Add(AttributeZoomLevel);
            s.Add(AttributeShowGrid);
            s.Add(AttributeSnapToGrid);
            s.Add(AttributeDisplayType);
            s.Add(AttributeLayoutMode);
            s.Add(AttributeConnectorMode);
            s.Add(AttributeEnableGrouping);
            s.Add(AttributeGenerateGroupNames);
            s.Add(AttributeManualRouteRebakePolicy);
            s.Add(AttributeId);
            return s;
        }

        internal override ICollection<string> MyChildElementNames()
        {
            var s = base.MyChildElementNames();
            s.Add(EntityTypeShape.ElementName);
            s.Add(AssociationConnector.ElementName);
            s.Add(InheritanceConnector.ElementName);
            return s;
        }
#endif

        protected override void PreParse()
        {
            Debug.Assert(State != EFElementState.Parsed, "this object should not already be in the parsed state");

            ClearEFObject(_id);
            _id = null;
            ClearEFObject(_zoomLevelAttr);
            _zoomLevelAttr = null;
            ClearEFObject(_showGridAttr);
            _showGridAttr = null;
            ClearEFObject(_snapToGridAttr);
            _snapToGridAttr = null;
            ClearEFObject(_displayTypeAttr);
            _displayTypeAttr = null;
            ClearEFObject(_layoutModeAttr);
            _layoutModeAttr = null;
            ClearEFObject(_connectorModeAttr);
            _connectorModeAttr = null;
            ClearEFObject(_enableGroupingAttr);
            _enableGroupingAttr = null;
            ClearEFObject(_generateGroupNamesAttr);
            _generateGroupNamesAttr = null;
            ClearEFObject(_manualRouteRebakePolicyAttr);
            _manualRouteRebakePolicyAttr = null;

            ClearEFObjectCollection(_entityTypeShapes);
            ClearEFObjectCollection(_associationConnectors);
            ClearEFObjectCollection(_inheritanceConnectors);

            base.PreParse();
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            if (elem.Name.LocalName == EntityTypeShape.ElementName)
            {
                EntityTypeShape shape = new EntityTypeShape(this, elem);
                shape.Parse(unprocessedElements);
                _entityTypeShapes.Add(shape);
            }
            else if (elem.Name.LocalName == AssociationConnector.ElementName)
            {
                AssociationConnector associationConnector = new AssociationConnector(this, elem);
                associationConnector.Parse(unprocessedElements);
                _associationConnectors.Add(associationConnector);
            }
            else if (elem.Name.LocalName == InheritanceConnector.ElementName)
            {
                InheritanceConnector inheritanceConnector = new InheritanceConnector(this, elem);
                inheritanceConnector.Parse(unprocessedElements);
                _inheritanceConnectors.Add(inheritanceConnector);
            }
            else
            {
                return base.ParseSingleElement(unprocessedElements, elem);
            }
            return true;
        }

        #endregion

        private class DiagramIdDefaultableValue : DefaultableValue<string>
        {
            private readonly string _defaultValue;

            internal DiagramIdDefaultableValue(EFElement parent)
                : base(parent, AttributeId)
            {
                // TODO: this is a temporary fix so that the old edmx file (doesn't contain diagramid) could still be loaded.
                // This should be go away once we implement upgrade/diagram fixup logic.
                _defaultValue = Guid.NewGuid().ToString("N");
            }

            internal override string AttributeName
            {
                get { return AttributeId; }
            }

            public override string DefaultValue
            {
                get { return _defaultValue; }
            }
        }

        private class ZoomLevelDefaultableValue : DefaultableValue<int>
        {
            internal ZoomLevelDefaultableValue(EFElement parent)
                : base(parent, AttributeZoomLevel)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeZoomLevel; }
            }

            public override int DefaultValue
            {
                get { return 100; }
            }
        }

        private class ShowGridDefaultableValue : DefaultableValue<bool>
        {
            internal ShowGridDefaultableValue(EFElement parent)
                : base(parent, AttributeShowGrid)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeShowGrid; }
            }

            public override bool DefaultValue
            {
                get { return false; }
            }
        }

        private class SnapToGridDefaultableValue : DefaultableValue<bool>
        {
            internal SnapToGridDefaultableValue(EFElement parent)
                : base(parent, AttributeSnapToGrid)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeSnapToGrid; }
            }

            public override bool DefaultValue
            {
                get { return true; }
            }
        }

        private class LayoutModeDefaultableValue : DefaultableValue<LayoutMode>
        {
            internal LayoutModeDefaultableValue(EFElement parent)
                : base(parent, AttributeLayoutMode)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeLayoutMode; }
            }

            public override LayoutMode DefaultValue
            {
                get { return Designer.LayoutMode.Legacy; }
            }
        }

        private class ConnectorModeDefaultableValue : DefaultableValue<ConnectorMode>
        {
            internal ConnectorModeDefaultableValue(EFElement parent)
                : base(parent, AttributeConnectorMode)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeConnectorMode; }
            }

            public override ConnectorMode DefaultValue
            {
                get { return Designer.ConnectorMode.Legacy; }
            }
        }

        private class EnableGroupingDefaultableValue : DefaultableValue<bool>
        {
            internal EnableGroupingDefaultableValue(EFElement parent)
                : base(parent, AttributeEnableGrouping)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeEnableGrouping; }
            }

            public override bool DefaultValue
            {
                get { return false; }
            }
        }

        private class GenerateGroupNamesDefaultableValue : DefaultableValue<bool>
        {
            internal GenerateGroupNamesDefaultableValue(EFElement parent)
                : base(parent, AttributeGenerateGroupNames)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeGenerateGroupNames; }
            }

            public override bool DefaultValue
            {
                get { return false; }
            }
        }

        private class ManualRouteRebakePolicyDefaultableValue : DefaultableValue<RebakePolicy>
        {
            internal ManualRouteRebakePolicyDefaultableValue(EFElement parent)
                : base(parent, AttributeManualRouteRebakePolicy)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeManualRouteRebakePolicy; }
            }

            public override RebakePolicy DefaultValue
            {
                get { return RebakePolicy.Ask; }
            }
        }

        private class DisplayTypeDefaultableValue : DefaultableValue<bool>
        {
            internal DisplayTypeDefaultableValue(EFElement parent)
                : base(parent, AttributeDisplayType)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeDisplayType; }
            }

            public override bool DefaultValue
            {
                get { return false; }
            }
        }
    }
}
