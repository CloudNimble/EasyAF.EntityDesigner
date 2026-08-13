using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.ModelWizard.engine;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class CodeFirstModelBuilderEngineTests
    {
        [TestMethod, Ignore("Different API Visiblity between official dll and locally built")]
        public void ProcessModel_validates_store_model()
        {
            //var storeModel = EdmModel.CreateStoreModel(
            //    new DbProviderInfo("System.Data.SqlClient", "2012"), 
            //    SqlProviderServices.Instance.GetProviderManifest("2012"));

            //storeModel.AddItem(EntityType.Create("E", "ns", DataSpace.SSpace, new string[0], new EdmMember[0], null));

            //var model = CreateDbModel(null, storeModel);
            
            //var errors = new List<EdmSchemaError>();
            //new CodeFirstModelBuilderEngineInvoker()
            //    .InvokeProcessModel(model, null, null, null, errors);

            //errors.Count.Should().Be(1);

            //Assert.Contains(
            //    string.Format(Strings.EdmModel_Validator_Semantic_KeyMissingOnEntityType("E")), 
            //    errors.Single().Message);
        }

        [TestMethod, Ignore("Different API Visiblity between official dll and locally built")]
        public void ProcessModel_validates_conceptual_model()
        {
            //var conceptualModel = EdmModel.CreateConceptualModel();
            //conceptualModel.AddItem(EntityType.Create("E", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null));

            //var model = CreateDbModel(conceptualModel, null);

            //var errors = new List<EdmSchemaError>();
            //new CodeFirstModelBuilderEngineInvoker()
            //    .InvokeProcessModel(model, null, null, null, errors);

            //errors.Count.Should().Be(1);

            //Assert.Contains(
            //    string.Format(Strings.EdmModel_Validator_Semantic_KeyMissingOnEntityType("E")),
            //    errors.Single().Message);
        }

        [TestMethod, Ignore("Different API Visiblity between official dll and locally built")]
        private static DbModel CreateDbModel(EdmModel conceptualModel, EdmModel storeModel)
        {
            //if (storeModel == null)
            //{
            //    storeModel = EdmModel.CreateStoreModel(
            //        new DbProviderInfo("System.Data.SqlClient", "2012"),
            //        SqlProviderServices.Instance.GetProviderManifest("2012"));                
            //}

            //if (conceptualModel == null)
            //{
            //    conceptualModel = EdmModel.CreateConceptualModel();
            //}

            //var databaseMapping = new DbDatabaseMapping
            //{
            //    Database = storeModel,
            //    Model = conceptualModel
            //};

            //databaseMapping.AddEntityContainerMapping(new EntityContainerMapping(databaseMapping.Model.Container));

            //return new DbModel(databaseMapping, new DbModelBuilder());
            return null;
        }

        private class CodeFirstModelBuilderEngineInvoker : CodeFirstModelBuilderEngine
        {
            public void InvokeProcessModel(DbModel model, string storeModelNamespace, ModelBuilderSettings settings, 
            ModelBuilderEngineHostContext hostContext, List<EdmSchemaError> errors)
            {
                ProcessModel(model, storeModelNamespace, settings, hostContext, errors);
            }
        }
    }
}
