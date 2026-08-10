// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    /// <summary>
    /// Helper class for creating DSL model elements in unit tests.
    /// Creates a minimal but complete model hierarchy to satisfy DSL rules.
    /// </summary>
    internal static class DslTestHelper
    {
        /// <summary>
        /// Creates a Store configured with the Entity Designer domain model.
        /// </summary>
        public static Store CreateStore()
        {
            return new Store(typeof(MicrosoftDataEntityDesignDomainModel));
        }

        /// <summary>
        /// Creates a minimal EntityDesignerViewModel for testing.
        /// This is required as the root of the model hierarchy.
        /// </summary>
        public static EntityDesignerViewModel CreateEntityDesignerViewModel(Store store)
        {
            EntityDesignerViewModel viewModel = null;
            using (var tx = store.TransactionManager.BeginTransaction("Create EntityDesignerViewModel", true))
            {
                viewModel = new EntityDesignerViewModel(store);
                tx.Commit();
            }
            return viewModel;
        }

        /// <summary>
        /// Creates an EntityType attached to the given EntityDesignerViewModel.
        /// </summary>
        public static EntityType CreateEntityType(Store store, EntityDesignerViewModel viewModel, string name)
        {
            EntityType entityType = null;
            using (var tx = store.TransactionManager.BeginTransaction("Create EntityType", true))
            {
                entityType = new EntityType(
                    store,
                    new PropertyAssignment(NameableItem.NameDomainPropertyId, name));
                viewModel.EntityTypes.Add(entityType);
                tx.Commit();
            }
            return entityType;
        }

        /// <summary>
        /// Creates a ScalarProperty with the specified name attached to the given EntityType.
        /// </summary>
        public static ScalarProperty CreateScalarProperty(Store store, EntityType entityType, string name, string type = "String", bool isKey = false)
        {
            ScalarProperty prop = null;
            using (var tx = store.TransactionManager.BeginTransaction("Create ScalarProperty", true))
            {
                prop = new ScalarProperty(
                    store,
                    new PropertyAssignment(NameableItem.NameDomainPropertyId, name),
                    new PropertyAssignment(Property.TypeDomainPropertyId, type),
                    new PropertyAssignment(ScalarProperty.EntityKeyDomainPropertyId, isKey));
                entityType.Properties.Add(prop);
                tx.Commit();
            }
            return prop;
        }

        /// <summary>
        /// Creates a ScalarProperty with a minimal model hierarchy for testing.
        /// Convenience overload that creates the entire hierarchy.
        /// </summary>
        public static ScalarProperty CreateScalarProperty(Store store, string name, string type = "String", bool isKey = false)
        {
            var viewModel = CreateEntityDesignerViewModel(store);
            var entityType = CreateEntityType(store, viewModel, "TestEntity");
            return CreateScalarProperty(store, entityType, name, type, isKey);
        }

        /// <summary>
        /// Creates a NavigationProperty with the specified name attached to the given EntityType.
        /// </summary>
        public static NavigationProperty CreateNavigationProperty(Store store, EntityType entityType, string name)
        {
            NavigationProperty prop = null;
            using (var tx = store.TransactionManager.BeginTransaction("Create NavigationProperty", true))
            {
                prop = new NavigationProperty(
                    store,
                    new PropertyAssignment(NameableItem.NameDomainPropertyId, name));
                entityType.NavigationProperties.Add(prop);
                tx.Commit();
            }
            return prop;
        }

        /// <summary>
        /// Creates a NavigationProperty with a minimal model hierarchy for testing.
        /// Convenience overload that creates the entire hierarchy.
        /// </summary>
        public static NavigationProperty CreateNavigationProperty(Store store, string name)
        {
            var viewModel = CreateEntityDesignerViewModel(store);
            var entityType = CreateEntityType(store, viewModel, "TestEntity");
            return CreateNavigationProperty(store, entityType, name);
        }

        /// <summary>
        /// Creates a ComplexProperty with the specified name attached to the given EntityType.
        /// </summary>
        public static ComplexProperty CreateComplexProperty(Store store, EntityType entityType, string name)
        {
            ComplexProperty prop = null;
            using (var tx = store.TransactionManager.BeginTransaction("Create ComplexProperty", true))
            {
                prop = new ComplexProperty(
                    store,
                    new PropertyAssignment(NameableItem.NameDomainPropertyId, name));
                entityType.Properties.Add(prop);
                tx.Commit();
            }
            return prop;
        }

        /// <summary>
        /// Creates a ComplexProperty with a minimal model hierarchy for testing.
        /// Convenience overload that creates the entire hierarchy.
        /// </summary>
        public static ComplexProperty CreateComplexProperty(Store store, string name)
        {
            var viewModel = CreateEntityDesignerViewModel(store);
            var entityType = CreateEntityType(store, viewModel, "TestEntity");
            return CreateComplexProperty(store, entityType, name);
        }
    }
}
