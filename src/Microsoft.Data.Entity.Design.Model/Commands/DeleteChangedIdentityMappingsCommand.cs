// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Database;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.Model.UpdateFromDatabase;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class DeleteChangedIdentityMappingsCommand : Command
    {
        private readonly ExistingModelSummary _preExistingModel;

        internal DeleteChangedIdentityMappingsCommand(ExistingModelSummary preExistingModel)
        {
            Debug.Assert(null != preExistingModel, "received null preExistingModel");
            _preExistingModel = preExistingModel;
        }

        // from the updated model with already updated S-side information determine
        // which S-side objects have been added which have different identities
        // but identical names to pre-existing S-side objects (only happens if you
        // add and delete something with the same name but in a different schema)
        // and delete the mappings for those S-side objects so that the C-side objects
        // do not end up incorrectly mapped to the new S-side object
        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            var service = cpc.EditingContext.GetEFArtifactService();
            var artifact = service.Artifact;
            Debug.Assert(artifact != null, "Null Artifact in DeleteChangedIdentityMappingsCommand.InvokeInternal()");
            if (null == artifact)
            {
                return;
            }

            // Find the S-side EntitySets (in the current artifact) which 
            // have different identities compared to those in the existing model
            // but the same names and delete their mappings
            FindNewStorageEntitySetsWithSameName(artifact, out HashSet<StorageEntitySet> storageEntitySetsWithSameNameButDifferentIdentity);
            DeleteMappingsForEntitySets(cpc, storageEntitySetsWithSameNameButDifferentIdentity);

            // Find the S-side Functions (in the current artifact) which 
            // have different identities compared to those in the existing model
            // but the same names and delete their mappings
            FindNewStorageFunctionsWithSameName(artifact, out HashSet<Function> storageFunctionsWithSameNameButDifferentIdentity);
            DeleteMappingsForFunctions(cpc, storageFunctionsWithSameNameButDifferentIdentity);
        }

        private void FindNewStorageEntitySetsWithSameName(
            EFArtifact artifact, out HashSet<StorageEntitySet> storageEntitySetsWithSameNameButDifferentIdentity)
        {
            storageEntitySetsWithSameNameButDifferentIdentity = [];

            if (null != artifact
                && null != artifact.StorageModel()
                && null != artifact.StorageModel().FirstEntityContainer)
            {
                // set up Dictionary of EntitySetName to EntitySet for new (DB-based) artifact
                Dictionary<string, EntitySet> newEntitySetMap = new Dictionary<string, EntitySet>();
                foreach (var newEntitySet in artifact.StorageModel().FirstEntityContainer.EntitySets())
                {
                    newEntitySetMap.Add(newEntitySet.LocalName.Value, newEntitySet);
                }

                // now compare all names - if we find a local name match compare identities
                // if they have the same name but different identities then add to returned HashSet
                foreach (var existingEntitySet in _preExistingModel.AllTablesAndViewsDictionary)
                {
                    var existingEntitySetLocalName = existingEntitySet.Value;

                    if (newEntitySetMap.TryGetValue(existingEntitySetLocalName, out EntitySet newEntitySet))
                    {
                        if (newEntitySet is StorageEntitySet newStorageEntitySet)
                        {
                            // we have a StorageEntitySet in the DB-based artifact which is the
                            // same as one in the pre-existing artifact. Now compare identities.
                            DatabaseObject newEntitySetIdentity = DatabaseObject.CreateFromEntitySet(newStorageEntitySet);
                            var existingEntitySetIdentity = existingEntitySet.Key;
                            if (!newEntitySetIdentity.Equals(existingEntitySetIdentity))
                            {
                                storageEntitySetsWithSameNameButDifferentIdentity.Add(newStorageEntitySet);
                            }
                        }
                    }
                }
            }
        }

        private void FindNewStorageFunctionsWithSameName(
            EFArtifact artifact, out HashSet<Function> storageFunctionsWithSameNameButDifferentIdentity)
        {
            storageFunctionsWithSameNameButDifferentIdentity = [];

            if (null != artifact
                && null != artifact.StorageModel())
            {
                // set up Dictionary of EntitySetName to EntitySet for new (DB-based) artifact
                Dictionary<string, Function> newFunctionMap = new Dictionary<string, Function>();
                foreach (var newFunction in artifact.StorageModel().Functions())
                {
                    newFunctionMap.Add(newFunction.LocalName.Value, newFunction);
                }

                // now compare all names - if we find a local name match compare identities
                // if they have the same name but different identities then add to returned HashSet
                foreach (var existingFunction in _preExistingModel.AllFunctionsDictionary)
                {
                    var existingFunctionLocalName = existingFunction.Value;

                    if (newFunctionMap.TryGetValue(existingFunctionLocalName, out Function newFunction))
                    {
                        if (null != newFunction)
                        {
                            // we have a Function in the DB-based artifact which is the
                            // same as one in the pre-existing artifact. Now compare identities.
                            DatabaseObject newFunctionIdentity = DatabaseObject.CreateFromFunction(newFunction);
                            var existingFunctionIdentity = existingFunction.Key;
                            if (!newFunctionIdentity.Equals(existingFunctionIdentity))
                            {
                                storageFunctionsWithSameNameButDifferentIdentity.Add(newFunction);
                            }
                        }
                    }
                }
            }
        }

        private static void DeleteMappingsForEntitySets(CommandProcessorContext cpc, HashSet<StorageEntitySet> storageEntitySets)
        {
            if (null != storageEntitySets)
            {
                foreach (var ses in storageEntitySets)
                {
                    foreach (var mappingFragment in ses.GetAntiDependenciesOfType<MappingFragment>())
                    {
                        DeleteEFElementCommand.DeleteInTransaction(cpc, mappingFragment);
                    }

                    foreach (var asm in ses.GetAntiDependenciesOfType<AssociationSetMapping>())
                    {
                        DeleteEFElementCommand.DeleteInTransaction(cpc, asm);
                    }
                }
            }
        }

        private static void DeleteMappingsForFunctions(CommandProcessorContext cpc, HashSet<Function> functions)
        {
            if (null != functions)
            {
                foreach (var f in functions)
                {
                    foreach (var fim in f.GetAntiDependenciesOfType<FunctionImportMapping>())
                    {
                        DeleteEFElementCommand.DeleteInTransaction(cpc, fim);
                    }
                }
            }
        }
    }
}
