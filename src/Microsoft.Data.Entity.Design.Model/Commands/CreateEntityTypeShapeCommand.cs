// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class CreateEntityTypeShapeCommand : Command
    {
        private readonly Diagram _diagram;
        private readonly EntityType _entity;
        private EntityTypeShape _created;
        private readonly Color _fillColor;
        private const double DEFAULTWIDTH = 1.5;
        internal static Random _rand = new Random();

        internal CreateEntityTypeShapeCommand(Diagram diagram, EntityType entity)
            : this(diagram, entity, EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor)
        {
        }

        internal CreateEntityTypeShapeCommand(Diagram diagram, EntityType entity, Color fillColor)
        {
            CommandValidation.ValidateConceptualEntityType(entity);
            Debug.Assert(diagram != null, "diagram is null");

            _diagram = diagram;
            _entity = entity;
            _created = null;
            _fillColor = fillColor;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            var nExistingTypeShapeCount =
                _entity.GetAntiDependenciesOfType<EntityTypeShape>().Count(ets => ets.Diagram.Id == _diagram.Id.Value);

            Debug.Assert(
                nExistingTypeShapeCount == 0,
                "There is already Entity Type Shape for entity :" + _entity.Name + " in the diagram " + _diagram.Name);

            if (nExistingTypeShapeCount == 0)
            {
                EntityTypeShape shape = new EntityTypeShape(_diagram, null);
                _diagram.AddEntityTypeShape(shape);

                shape.EntityType.SetRefName(_entity);
                shape.Width.Value = DEFAULTWIDTH;

                // The DSL will set the correct locations for the shapes at a later point, but we need to provide initial values for X and Y in the meantime
                // so that we can construct the shape. We're using random numbers here to ensure that if the DSL fails for some reason, new shapes do not
                // stack directly on top of each other.
                shape.PointX.Value = _rand.NextDouble() * 12.0;
                shape.PointY.Value = _rand.NextDouble() * 32.0;

                if (_fillColor != EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor)
                {
                    shape.FillColor.Value = _fillColor;
                }

                XmlModelHelper.NormalizeAndResolve(shape);

                _created = shape;
            }
        }

        internal EntityTypeShape EntityTypeShape
        {
            get { return _created; }
        }

        #region Static methods

        internal static void CreateEntityTypeShapeAndConnectorsInDiagram(
            CommandProcessorContext cpc, Diagram diagram, ConceptualEntityType entity, bool createRelatedEntityTypeShapes)
        {
            CreateEntityTypeShapeAndConnectorsInDiagram(
                cpc, diagram, entity, EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor, createRelatedEntityTypeShapes);
        }

        /// <summary>
        ///     The method do the following:
        ///     - Create the shape for the entity-type in diagram.
        ///     - Create association and inheritance connectors for the entity type shape and all entity type shapes in the diagram.
        ///     - If createRelatedEntityTypeShapes flag is set to true, it will also create all directly related entity-type-shapes not in the diagram.
        /// </summary>
        internal static void CreateEntityTypeShapeAndConnectorsInDiagram(
            CommandProcessorContext cpc, Diagram diagram, ConceptualEntityType entity, Color entityTypeShapeFillColor,
            bool createRelatedEntityTypeShapes)
        {
            // if the entity type shape has been created, return immediately.
            if (entity == null
                || entity.GetAntiDependenciesOfType<EntityTypeShape>().Count(ets => ets.Diagram.Id == diagram.Id.Value) > 0)
            {
                return;
            }

            CreateEntityTypeShapeCommand createEntityTypeShapecommand = new CreateEntityTypeShapeCommand(diagram, entity, entityTypeShapeFillColor);
            createEntityTypeShapecommand.PostInvokeEvent += (o, eventsArgs) =>
                {
                    if (createEntityTypeShapecommand.EntityTypeShape != null)
                    {
                        List<EntityType> relatedEntityTypesNotInDiagram = new List<EntityType>();

                        HashSet<EntityType> entityTypesInDiagram = new HashSet<EntityType>(diagram.EntityTypeShapes.Select(ets => ets.EntityType.Target));

                        // add inheritance connector if the base type exists in the diagram.
                        if (entity.SafeBaseType != null)
                        {
                            if (entityTypesInDiagram.Contains(entity.SafeBaseType))
                            {
                                CommandProcessor.InvokeSingleCommand(cpc, new CreateInheritanceConnectorCommand(diagram, entity));
                            }
                            else
                            {
                                relatedEntityTypesNotInDiagram.Add(entity.SafeBaseType);
                            }
                        }

                        // add the inheritance connector if the derived type exist in the diagram.
                        foreach (var derivedEntityType in entity.ResolvableDirectDerivedTypes)
                        {
                            if (entityTypesInDiagram.Contains(derivedEntityType))
                            {
                                CommandProcessor.InvokeSingleCommand(cpc, new CreateInheritanceConnectorCommand(diagram, derivedEntityType));
                            }
                            else
                            {
                                relatedEntityTypesNotInDiagram.Add(derivedEntityType);
                            }
                        }

                        // Find all associations which the entity type participates.
                        var participatingAssociations = Association.GetAssociationsForEntityType(entity);

                        foreach (var association in participatingAssociations)
                        {
                            List<EntityType> entityTypesInAssociation = association.AssociationEnds().Select(ae => ae.Type.Target).ToList();
                            List<EntityType> entityTypesNotInDiagram = entityTypesInAssociation.Except(entityTypesInDiagram).ToList();

                            if (entityTypesNotInDiagram.Count == 0)
                            {
                                CommandProcessor.InvokeSingleCommand(cpc, new CreateAssociationConnectorCommand(diagram, association));
                            }
                            relatedEntityTypesNotInDiagram.AddRange(entityTypesNotInDiagram);
                        }

                        if (createRelatedEntityTypeShapes)
                        {
                            foreach (var entityType in relatedEntityTypesNotInDiagram)
                            {
                                // we only want to bring entity-type directly related to the entity-type, so set createRelatedEntityTypeShapes flag to false.
                                CreateEntityTypeShapeAndConnectorsInDiagram(
                                    cpc, diagram, entityType as ConceptualEntityType, entityTypeShapeFillColor, false);
                            }
                        }
                    }
                };
            CommandProcessor.InvokeSingleCommand(cpc, createEntityTypeShapecommand);
        }

        #endregion
    }
}
