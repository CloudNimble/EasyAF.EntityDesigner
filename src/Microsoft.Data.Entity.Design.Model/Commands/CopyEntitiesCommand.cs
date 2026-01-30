// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Eventing;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    /// <summary>
    ///     Create copy of Entities (EntityType, Association, Inheritance).
    ///     If the entities are copied to a diagram, the behavior as follow:
    ///     --------------------------------------------------------------------------------------------------------------------------------------------------------------------
    ///     | Object in Model   | Object in Diagram     |           Expected behavior                                                                                          |
    ///     --------------------------------------------------------------------------------------------------------------------------------------------------------------------
    ///     | Yes               |      Yes              |   A new object copy is added to the Entity Model and the corresponding diagram item is created in the diagram model  |
    ///     | Yes               |      No               |   Only diagram item is added into diagram                                                                            |
    ///     | No                |      No               |   A new object copy is added to the Entity Model and the corresponding diagram item is created in the diagram model  |
    ///     --------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// </summary>
    internal class CopyEntitiesCommand : CopyAnnotatableElementCommand
    {
        private readonly EntitiesClipboardFormat _clipboardEntities;
        private readonly ModelSpace _modelSpace;
        private readonly Diagram _diagram;

        /// <summary>
        ///     Creates a copy of EntityTypes and relationships between them from clipboard format
        /// </summary>
        /// <param name="property"></param>
        internal CopyEntitiesCommand(EntitiesClipboardFormat clipboardEntities, ModelSpace modelSpace)
            : this(null, clipboardEntities, modelSpace)
        {
            _clipboardEntities = clipboardEntities;
            _modelSpace = modelSpace;
        }

        /// <summary>
        ///     The behavior is as follow:
        ///     Creates copy of EntityTypes and relationships between them from clipboard format in the Entity Model if:
        ///     - The entity-types and/or relationships do not exist in the model.
        ///     - The passed in diagram parameter is null.
        ///     OR
        ///     Create the associated diagram items in the Diagram Model if:
        ///     - The diagram is not null AND entity-types and/or relationships exist in the model AND the corresponding diagram items do not exist in the diagram.
        /// </summary>
        /// <param name="diagram"></param>
        /// <param name="clipboardEntities"></param>
        /// <param name="modelSpace"></param>
        internal CopyEntitiesCommand(Diagram diagram, EntitiesClipboardFormat clipboardEntities, ModelSpace modelSpace)
        {
            _diagram = diagram;
            _clipboardEntities = clipboardEntities;
            _modelSpace = modelSpace;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            if (_diagram != null)
            {
                // Add diagram id information in the transaction context.
                // This is to ensure the diagram objects are created correctly.
                if (cpc.EfiTransaction.GetContextValue<DiagramContextItem>(EfiTransactionOriginator.TransactionOriginatorDiagramId) == null)
                {
                    cpc.EfiTransaction.AddContextValue(
                        EfiTransactionOriginator.TransactionOriginatorDiagramId, new DiagramContextItem(_diagram.Id.Value));
                }
            }

            var service = cpc.EditingContext.GetEFArtifactService();
            var artifact = service.Artifact;

            // the model that we want to add the entity to
            var model = ModelHelper.GetEntityModel(artifact, _modelSpace);

            Dictionary<EntityTypeClipboardFormat, EntityType> entitiesMap = new Dictionary<EntityTypeClipboardFormat, EntityType>(_clipboardEntities.ClipboardEntities.Count);
            // create copies of EntityTypes
            foreach (var clipboardEntity in _clipboardEntities.ClipboardEntities)
            {
                CopyEntityCommand cmd = new CopyEntityCommand(_diagram, clipboardEntity, _modelSpace);
                CommandProcessor.InvokeSingleCommand(cpc, cmd);
                entitiesMap.Add(clipboardEntity, cmd.EntityType);
            }

            // create copies of associations
            foreach (var clipboardAssociation in _clipboardEntities.ClipboardAssociations)
            {
                // Check if the association is in the Artifact/model.
                if (_diagram != null)
                {
                    // Get the association by name.
                    if (artifact.ArtifactSet.LookupSymbol(clipboardAssociation.NormalizedName) is Association association)
                    {
                        List<EntityType> entityTypesInAssociation = association.AssociationEnds().Select(ae => ae.Type.Target).ToList();
                        // Check whether the associated entity-types are created in the previous step.
                        // When the user copy and paste an association and the associated entities in the same diagram, 
                        // we need to create a new copy of the association in the model. Without the check below, the code will determine that there is no need
                        // to create the copy since the association exist in the model.
                        if (entityTypesInAssociation.Except(entitiesMap.Values).Count() == 0)
                        {
                            // At this point we know that the association that is referred in clipboard exists in the current model.
                            // Next we will check whether this association is represented in diagram or not.
                            // if not, create a new association connector in the diagram.
                            if (association.GetAntiDependenciesOfType<AssociationConnector>()
                                    .Count(ac => ac.Diagram.Id == _diagram.Id.Value) == 0)
                            {
                                // AssociationConnector is created by creating EntityTypeShapes that associationEnd refer to.
                                foreach (var associationEnd in association.AssociationEnds())
                                {
                                    ConceptualEntityType entityType = associationEnd.Type.SafeTarget as ConceptualEntityType;
                                    Debug.Assert(
                                        entityType != null,
                                        "In: CopyEntitiesCommand's  InvokeInternal, associationEnd's Type property should be typeof ConceptualEntityType");
                                    if (entityType != null)
                                    {
                                        // CreateEntityTypeShapeAndConnectorsInDiagram method will check if the shape for the entity-type has been created; 
                                        // and it will not create one if the shape already exists in the diagram.
                                        // Also, VerifyDiagramModelIntegrityVisitor will assert if there are duplicate diagram shapes (shapes that point to the same model element)
                                        // every-time a command transaction is committed. So adding another check to do the same thing here is redundant.
                                        CreateEntityTypeShapeCommand.CreateEntityTypeShapeAndConnectorsInDiagram(
                                            cpc, _diagram, entityType, false);
                                    }
                                }
                            }
                            continue;
                        }
                    }
                }
                CopyAssociation(cpc, model, clipboardAssociation, entitiesMap);
            }

            // create copies of inheritances
            foreach (var inheritance in _clipboardEntities.ClipboardInheritances)
            {
                if (_diagram != null)
                {
                    if (artifact.ArtifactSet.LookupSymbol(inheritance.Key.NormalizedName) is EntityType derivedEntity
                        && artifact.ArtifactSet.LookupSymbol(inheritance.Value.NormalizedName) is EntityType baseEntity)
                    {
                        if (entitiesMap.Values.Contains(derivedEntity)
                            && entitiesMap.Values.Contains(baseEntity))
                        {
                            // check if the underlying entity-types are not in the diagram.
                            // InheritanceConnector are created by ensuring both EntityTypeShapes are created.
                            if (derivedEntity.GetAntiDependenciesOfType<EntityTypeShape>().Count(ets => ets.Diagram.Id == _diagram.Id.Value)
                                == 0
                                && baseEntity.GetAntiDependenciesOfType<EntityTypeShape>()
                                       .Count(ets2 => ets2.Diagram.Id == _diagram.Id.Value) == 0)
                            {
                                // CreateEntityTypeShapeAndConnectorsInDiagram method will check if the shape for the entity-type has been created; 
                                // and it will not create one if the shape already exists in the diagram.
                                // Also, VerifyDiagramModelIntegrityVisitor will assert if there are duplicate diagram shapes (shapes that point to the same model element)
                                // every-time a command transaction is committed. So adding another check to do the same thing here is redundant.
                                CreateEntityTypeShapeCommand.CreateEntityTypeShapeAndConnectorsInDiagram(
                                    cpc, _diagram, derivedEntity as ConceptualEntityType, false);
                                CreateEntityTypeShapeCommand.CreateEntityTypeShapeAndConnectorsInDiagram(
                                    cpc, _diagram, baseEntity as ConceptualEntityType, false);
                            }
                            continue;
                        }
                    }
                }
                CopyInheritance(cpc, inheritance, entitiesMap);
            }
        }

        private void CopyAssociation(
            CommandProcessorContext cpc, BaseEntityModel model, AssociationClipboardFormat clipboardAssociation
            , Dictionary<EntityTypeClipboardFormat, EntityType> entitiesMap)
        {
            var associationName = ModelHelper.GetUniqueName(typeof(Association), model, clipboardAssociation.AssociationName);
            var clipboardEntity1 = clipboardAssociation.ClipboardEntity1;
            var clipboardEntity2 = clipboardAssociation.ClipboardEntity2;

            ConceptualEntityType entity1 = entitiesMap[clipboardEntity1] as ConceptualEntityType;
            ConceptualEntityType entity2 = entitiesMap[clipboardEntity2] as ConceptualEntityType;

            Debug.Assert(entity1 != null, "entity1 is not a ConceptualEntityType");
            Debug.Assert(entity2 != null, "entity2 is not a ConceptualEntityType");

            var navigationPropertyEntity1 = clipboardEntity1.GetNavigationPropertyClipboard(
                clipboardAssociation.AssociationName, clipboardAssociation.AssociationEndRole1);
            var navigationPropertyEntity2 = clipboardEntity2.GetNavigationPropertyClipboard(
                clipboardAssociation.AssociationName, clipboardAssociation.AssociationEndRole2);

            var navigationPropertyName1 = navigationPropertyEntity1 != null
                                              ? ModelHelper.GetUniqueConceptualPropertyName(navigationPropertyEntity1.PropertyName, entity1)
                                              : null;
            string navigationPropertyName2 = null;
            if (entity1 == entity2)
            {
                // if this is a self-association then the NavProp for end2 needs a different name from end1
                navigationPropertyName2 =
                    ModelHelper.GetUniqueConceptualPropertyName(
                        (navigationPropertyEntity2 != null ? navigationPropertyEntity2.PropertyName : entity1.LocalName.Value), entity2,
                        [navigationPropertyName1]);
            }
            else
            {
                navigationPropertyName2 =
                    ModelHelper.GetUniqueConceptualPropertyName(
                        (navigationPropertyEntity2 != null ? navigationPropertyEntity2.PropertyName : entity1.LocalName.Value), entity2);
            }
            CreateConceptualAssociationCommand cmd = new CreateConceptualAssociationCommand(
                associationName, entity1, clipboardAssociation.Multiplicity1, navigationPropertyName1, entity2,
                clipboardAssociation.Multiplicity2, navigationPropertyName2, true, false);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);

            // copy nav prop facets & structured annotations
            if (navigationPropertyEntity1 != null)
            {
                var np1 = entity1.FindNavigationPropertyForEnd(cmd.End1);
                CommandProcessor.InvokeSingleCommand(
                    cpc,
                    new SetNavigationPropertyFacetsCommand(
                        np1, navigationPropertyEntity1.GetterAccessModifier, navigationPropertyEntity1.SetterAccessModifier));
                AddAnnotations(navigationPropertyEntity1, np1);
            }
            if (navigationPropertyEntity2 != null)
            {
                var np2 = entity2.FindNavigationPropertyForEnd(cmd.End2);
                CommandProcessor.InvokeSingleCommand(
                    cpc,
                    new SetNavigationPropertyFacetsCommand(
                        np2, navigationPropertyEntity2.GetterAccessModifier, navigationPropertyEntity2.SetterAccessModifier));
                AddAnnotations(navigationPropertyEntity2, np2);
            }

            if (clipboardAssociation.ReferentialConstraint != null)
            {
                EntityType principal;
                EntityType dependent;
                if (entity1 == FindEntityByClipboardName(clipboardAssociation.ReferentialConstraint.PrincipalEntityName, entitiesMap))
                {
                    principal = entity1;
                    dependent = entity2;
                }
                else
                {
                    Debug.Assert(
                        entity2 == FindEntityByClipboardName(clipboardAssociation.ReferentialConstraint.PrincipalEntityName, entitiesMap),
                        "could not find entity using clipboard name " + clipboardAssociation.ReferentialConstraint.PrincipalEntityName);

                    principal = entity2;
                    dependent = entity1;
                }
                IEnumerable<Property> principalPropertyList = null;
                IEnumerable<Property> dependentPropertyList = null;
                var associationEnds = cmd.CreatedAssociation.AssociationEnds();
                if (associationEnds.Count == 2
                    && (principalPropertyList =
                        ModelHelper.FindProperties(principal, clipboardAssociation.ReferentialConstraint.PrincipalProperties)) != null
                    && (dependentPropertyList =
                        ModelHelper.FindProperties(dependent, clipboardAssociation.ReferentialConstraint.DependentProperties)) != null)
                {
                    CreateReferentialConstraintCommand refCmd = new CreateReferentialConstraintCommand(
                        associationEnds[0], associationEnds[1], principalPropertyList, dependentPropertyList
                        );
                    CommandProcessor.InvokeSingleCommand(cpc, refCmd);
                    AddAnnotations(clipboardAssociation.ReferentialConstraint, refCmd.ReferentialConstraint);
                }
            }

            // add structured annotations to the association
            AddAnnotations(clipboardAssociation, cmd.CreatedAssociation);
        }

        private static void CopyInheritance(
            CommandProcessorContext cpc, KeyValuePair<EntityTypeClipboardFormat, EntityTypeClipboardFormat> inheritance,
            Dictionary<EntityTypeClipboardFormat, EntityType> entitiesMap)
        {
            ConceptualEntityType derivedEntity = entitiesMap[inheritance.Key] as ConceptualEntityType;
            ConceptualEntityType baseEntity = entitiesMap[inheritance.Value] as ConceptualEntityType;

            Debug.Assert(derivedEntity != null, "EntityType derivedEntity is not a ConceptualEntityType");
            Debug.Assert(baseEntity != null, "EntityType baseEntity is not a ConceptualEntityType");

            CreateInheritanceCommand cmd = new CreateInheritanceCommand(derivedEntity, baseEntity);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);
        }

        private EntityType FindEntityByClipboardName(string name, Dictionary<EntityTypeClipboardFormat, EntityType> entitiesMap)
        {
            //Entity can be renamed when pasted in the model, so lets find clipboard format entity and then created one.
            foreach (var clipboardEntity in _clipboardEntities.ClipboardEntities)
            {
                if (clipboardEntity.EntityName == name)
                {
                    return entitiesMap[clipboardEntity];
                }
            }
            return null;
        }
    }
}
