// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public class UniqueIdentifierServiceTests
    {
        [TestMethod]
        public static void RegisterUsedIdentifier_uses_the_specified_StringComparer_correctly()
        {
            const string identifierA = "Identifier";
            const string identifierB = "IdeNTiFieR";

            UniqueIdentifierService service = new UniqueIdentifierService(StringComparer.Ordinal);
            service.RegisterUsedIdentifier(identifierA);
            Action act = () => service.RegisterUsedIdentifier(identifierB);
            act.Should().NotThrow();

            service = new UniqueIdentifierService(StringComparer.OrdinalIgnoreCase);
            service.RegisterUsedIdentifier(identifierA);
            Action act2 = () => service.RegisterUsedIdentifier(identifierB);
            act2.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public static void AdjustIdentifier_with_Ordinal_comparer_and_identity_transform_returns_expected_result()
        {
            const string identifier = "Identifier";
            const string adjustedIdentifier1 = "Identifier1";
            const string adjustedIdentifier2 = "Identifier2";
            const string adjustedIdentifier3 = "Identifier3";

            UniqueIdentifierService service = new UniqueIdentifierService(StringComparer.Ordinal);
            service.RegisterUsedIdentifier(identifier);

            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier1);
            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier2);
            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier3);
        }

        [TestMethod]
        public static void AdjustIdentifier_with_OrdinalIgnoreCase_comparer_and_custom_transform_returns_expected_result()
        {
            const string identifier = "My.Identifier";
            const string usedIdentifier = "My_IdENtIfiEr";
            const string adjustedIdentifier1 = "My_Identifier1";
            const string adjustedIdentifier2 = "My_Identifier2";
            const string adjustedIdentifier3 = "My_Identifier3";

            Func<string, string> transform = s => s.Replace(".", "_");

            UniqueIdentifierService service = new UniqueIdentifierService(StringComparer.OrdinalIgnoreCase, transform);
            service.RegisterUsedIdentifier(usedIdentifier);

            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier1);
            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier2);
            service.AdjustIdentifier(identifier).Should().Be(adjustedIdentifier3);
        }

        [TestMethod]
        public static void TryGetAdjustedName_correctly_retrieves_the_adjusted_identifier_associated_with_an_object()
        {
            const string identifier = "Identifier";
            const string adjustedIdentifier1 = "Identifier1";
            const string adjustedIdentifier2 = "Identifier2";
            const string adjustedIdentifier3 = "Identifier3";

            var value1 = new object();
            var value2 = new object();
            var value3 = new object();

            UniqueIdentifierService service = new UniqueIdentifierService();
            service.RegisterUsedIdentifier(identifier);

            service.AdjustIdentifier(identifier, value1).Should().Be(adjustedIdentifier1);
            service.AdjustIdentifier(identifier, value2).Should().Be(adjustedIdentifier2);
            service.AdjustIdentifier(identifier, value3).Should().Be(adjustedIdentifier3);
            service.TryGetAdjustedName(value1, out string adjustedIdentifier).Should().BeTrue();
            adjustedIdentifier.Should().Be(adjustedIdentifier1);
            service.TryGetAdjustedName(value2, out adjustedIdentifier).Should().BeTrue();
            adjustedIdentifier.Should().Be(adjustedIdentifier2);
            service.TryGetAdjustedName(value3, out adjustedIdentifier).Should().BeTrue();
            adjustedIdentifier.Should().Be(adjustedIdentifier3);
        }
    }
}
