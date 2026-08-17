// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod]
        public void InferKeyColumns_returns_names_for_key_valid_candidates()
        {
            EdmProperty nonNullableIntProperty =
                EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            nonNullableIntProperty.Nullable = false;

            EdmProperty nonNullableStringProperty =
                EdmProperty.CreatePrimitive("Name", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));
            nonNullableStringProperty.Nullable = false;

            CreateStoreModelBuilder()
                .InferKeyProperties([nonNullableIntProperty, nonNullableStringProperty])
                .Should().BeEquivalentTo(new[] { "Id", "Name" });
        }

        [TestMethod]
        public void InferKeyColumns_does_not_return_names_for_nullable_columns()
        {
            EdmProperty nullableIntProperty =
                EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));
            nullableIntProperty.Nullable = true;

            CreateStoreModelBuilder()
                .InferKeyProperties([nullableIntProperty])
                .Should().BeEmpty();
        }

        [TestMethod]
        public void InferKeyColumns_does_not_return_names_for_columns_of_types_that_are_not_valid_keys_types()
        {
            EdmProperty geographyProperty =
                EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Geography));
            geographyProperty.Nullable = false;

            CreateStoreModelBuilder()
                .InferKeyProperties([geographyProperty])
                .Should().BeEmpty();
        }
    }
}
