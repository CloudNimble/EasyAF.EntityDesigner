// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Database;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Common.Diagnostics;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Data.Entity.Design.Edmx.UpdateFromDatabase
{
    internal class UpdatedModelSummary
    {
        // the artifact from which this was created
        private readonly EFArtifact _artifact;

        // a map of the C-side EntityTypes to their EntityTypeIdentity
        private readonly Dictionary<EntityType, EntityTypeIdentity> _cEntityTypeToEntityTypeIdentity =
            [];

        // a map with key the DatabaseObject (from the S-side EntitySet) to 
        // a value which is a HashSet of the underlying S-side Property objects
        private readonly Dictionary<DatabaseObject, HashSet<Property>> _databaseObjectColumns =
            [];

        // Summary info about associations in the model.
        private readonly AssociationSummary _associationSummary;

        internal UpdatedModelSummary(EFArtifact artifact)
        {
            _artifact = artifact;

            Debug.Assert(artifact != null, "Null artifact");

            if (artifact != null)
            {
                if (null != artifact.MappingModel()
                    && null != artifact.MappingModel().FirstEntityContainerMapping)
                {
                    RecordEntityTypeIdentities(
                        artifact.MappingModel().FirstEntityContainerMapping);

                    // build the association summary
                    _associationSummary = AssociationSummary.ConstructAssociationSummary(artifact);
                }

                if (null != artifact.StorageModel()
                    && null != artifact.StorageModel().FirstEntityContainer)
                {
                    if (artifact.StorageModel().FirstEntityContainer is StorageEntityContainer sec)
                    {
                        RecordStorageProperties(sec);
                    }
                }
            }
        }

        internal string TraceString()
        {
            StringBuilder sb = new StringBuilder("[" + typeof(UpdatedModelSummary).Name);
            sb.AppendLine(" artifactUri=" + (_artifact == null ? "null" : _artifact.Uri.ToString()));

            sb.Append(
                " " + EFToolsTraceUtils.FormatNamedDictionary(
                    "cEntityTypeToEntityTypeIdentity", _cEntityTypeToEntityTypeIdentity,
                    delegate(EntityType et) { return et.NormalizedNameExternal; },
                    delegate(EntityTypeIdentity etId) { return etId.TraceString(); },
                    true,
                    " "
                          ));

            sb.Append(
                " " + EFToolsTraceUtils.FormatNamedDictionary(
                    "databaseObjectColumns", _databaseObjectColumns,
                    delegate(DatabaseObject dbObj) { return dbObj.ToString(); },
                    delegate(HashSet<Property> hashOfProperties)
                        {
                            return EFToolsTraceUtils.FormatEnumerable(
                                hashOfProperties, delegate(Property prop) { return prop.NormalizedNameExternal; });
                        },
                    true,
                    " "
                          ));

            sb.AppendLine(" associationSummary=" + (_associationSummary == null ? "null" : _associationSummary.TraceString()));

            sb.Append("]");

            return sb.ToString();
        }

        internal EFArtifact Artifact
        {
            get { return _artifact; }
        }

        internal EntityTypeIdentity GetEntityTypeIdentityForEntityType(EntityType et)
        {
            _cEntityTypeToEntityTypeIdentity.TryGetValue(et, out EntityTypeIdentity results);
            return results;
        }

        internal HashSet<Property> GetPropertiesForDatabaseObject(DatabaseObject dbObj)
        {
            _databaseObjectColumns.TryGetValue(dbObj, out HashSet<Property> results);
            return results;
        }

        internal AssociationIdentity GetAssociationIdentityForAssociation(Association assoc)
        {
            var results = _associationSummary.GetAssociationIdentity(assoc);
            return results;
        }

        private void RecordEntityTypeIdentities(
            EntityContainerMapping entityContainerMapping)
        {
            // construct mapping from EntityType to EntityTypeIdentity which
            // is its identity
            UpdateModelFromDatabaseUtils.ConstructEntityMappings(
                entityContainerMapping, AddCEntityTypeToEntityTypeIdentityMapping);
        }

        private void RecordStorageProperties(StorageEntityContainer sec)
        {
            foreach (var es in sec.EntitySets())
            {
                if (es is StorageEntitySet ses)
                {
                    DatabaseObject dbObj = DatabaseObject.CreateFromEntitySet(ses);
                    var et = ses.EntityType.Target;
                    if (null == et)
                    {
                        Debug.Fail("Null EntityType");
                    }
                    else
                    {
                        foreach (var prop in et.Properties())
                        {
                            AddDbObjToPropertiesMapping(dbObj, prop);
                        }
                    }
                }
            }
        }

        private void AddCEntityTypeToEntityTypeIdentityMapping(
            EntityType key, DatabaseObject dbObj)
        {
            _cEntityTypeToEntityTypeIdentity.TryGetValue(key, out EntityTypeIdentity etId);
            if (null == etId)
            {
                etId = _cEntityTypeToEntityTypeIdentity[key] = new EntityTypeIdentity();
            }

            etId.AddTableOrView(dbObj);
        }

        private void AddDbObjToPropertiesMapping(DatabaseObject key, Property prop)
        {
            _databaseObjectColumns.TryGetValue(key, out HashSet<Property> propsSet);
            if (null == propsSet)
            {
                propsSet = _databaseObjectColumns[key] = [];
            }

            propsSet.Add(prop);
        }
    }
}
