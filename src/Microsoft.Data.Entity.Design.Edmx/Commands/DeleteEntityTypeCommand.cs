// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Integrity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using XamlDesignerBaseResources = Microsoft.Data.Entity.Design.XmlEngine.XmlEngineResources;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class DeleteEntityTypeCommand : DeleteEFElementCommand
    {
        internal string DeletedEntityTypeName { get; private set; }
        internal IEnumerable<EntityQualifiedPropertyName> DeletedMappedStoragePropertyNames { get; private set; }

        public DeleteEntityTypeCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        /// <summary>
        ///     Deletes the passed in EntityType
        /// </summary>
        /// <param name="entityType"></param>
        internal DeleteEntityTypeCommand(EntityType entityType)
            : base(entityType)
        {
            CommandValidation.ValidateEntityType(entityType);

            SaveDeletedInformation();
        }

        private void SaveDeletedInformation()
        {
            DeletedEntityTypeName = EntityType.Name.Value;

            // Collect all of the mapped storage property names in case we need to 
            HashSet<EntityQualifiedPropertyName> deletedStoragePropertyNames = new HashSet<EntityQualifiedPropertyName>();
            foreach (var cProp in EntityType.Properties())
            {
                var scalarProperties = cProp.GetAntiDependenciesOfType<ScalarProperty>();
                foreach (var scalarProperty in scalarProperties)
                {
                    StorageProperty storageProperty = (StorageProperty)scalarProperty.ColumnName.Target;
                    if (storageProperty != null
                        && storageProperty.EntityType != null)
                    {
                        EntityQualifiedPropertyName entityToPropertyName = new EntityQualifiedPropertyName(
                            storageProperty.EntityType.Name.Value, storageProperty.Name.Value, false);
                        if (!deletedStoragePropertyNames.Contains(entityToPropertyName))
                        {
                            deletedStoragePropertyNames.Add(entityToPropertyName);
                        }
                    }
                }
            }
            DeletedMappedStoragePropertyNames = deletedStoragePropertyNames;
        }

        protected internal EntityType EntityType
        {
            get
            {
                EntityType elem = EFElement as EntityType;
                Debug.Assert(elem != null, "underlying element does not exist or is not an EntityType");
                if (elem == null)
                {
                    throw new InvalidModelItemException();
                }
                return elem;
            }
        }

        /// <summary>
        ///     We override this method because we need to do some extra things before
        ///     the normal PreInvoke gets called and our antiDeps are removed
        /// </summary>
        /// <param name="cpc"></param>
        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            // save off the deleted entity type name
            SaveDeletedInformation();

            // enforce our mapping rules for C-side entities
            if (EntityType.EntityModel.IsCSDL)
            {
                EnforceEntitySetMappingRules.AddRule(cpc, EntityType);
                // remove base type for all derived EntityTypes
                ConceptualEntityType cet = EntityType as ConceptualEntityType;
                Debug.Assert(cet != null, "EntityType is not a ConceptualEntityType");

                foreach (var derivedType in cet.ResolvableDirectDerivedTypes)
                {
                    DeleteInheritanceCommand cmd = new DeleteInheritanceCommand(derivedType);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
            base.PreInvoke(cpc);
        }

        /// <summary>
        ///     We override this method to do some specialized processing of AssociationEnd antiDeps
        /// </summary>
        /// <param name="cpc"></param>
        protected override void RemoveAntiDeps(CommandProcessorContext cpc)
        {
            // if this entity is used in an AssociationEnd, remove the entire Association
            // Note: have to convert to array first to prevent exceptions due to
            // editing the collection while iterating over it
            var associations = GetListOfAssociationsToBeDeleted(EntityType).ToArray();
            foreach (var association in associations)
            {
                var deleteAssociationCommand = association.GetDeleteCommand();
                DeleteInTransaction(cpc, deleteAssociationCommand);
            }

            if (EntityType.EntityModel.IsCSDL)
            {
                if (EntityType.RuntimeModelRoot() is ConceptualEntityModel cModel)
                {
                    // If there is a FunctionImport which returns the entity type, set the FunctionImport return type to null.
                    foreach (var fi in EntityType.GetAntiDependenciesOfType<FunctionImport>())
                    {
                        CommandProcessor.InvokeSingleCommand(
                            cpc, new ChangeFunctionImportCommand(
                                cModel.FirstEntityContainer as ConceptualEntityContainer,
                                fi, fi.Function, fi.DisplayName, fi.IsComposable.Value, true,
                                XamlDesignerBaseResources.NoneDisplayValueUsedForUX));
                    }
                }
            }

            // process the remaining antiDeps normally
            base.RemoveAntiDeps(cpc);
        }

        /// <summary>
        ///     Constructs a list of the Associations to be deleted given a to-be-deleted EntityType
        /// </summary>
        /// <param name="entityType">the EntityType</param>
        /// <returns>list of the Associations to be deleted</returns>
        internal static List<Association> GetListOfAssociationsToBeDeleted(EntityType entityType)
        {
            List<Association> assocsToDelete = new List<Association>();
            if (entityType == null)
            {
                Debug.Fail("argument entityType must not be null");
                return assocsToDelete;
            }

            // if this entity is used in an AssociationEnd, the entire Association should be deleted
            foreach (var associationEnd in entityType.GetAntiDependenciesOfType<AssociationEnd>())
            {
                Association association = associationEnd.Parent as Association;
                Debug.Assert(association != null, " null association for AssociationEnd" + associationEnd.ToPrettyString());
                if (association != null
                    && association.XObject != null)
                {
                    assocsToDelete.Add(association);
                }
            }

            return assocsToDelete;
        }
    }
}
