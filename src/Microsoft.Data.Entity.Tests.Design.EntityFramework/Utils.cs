// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.IO;
using System.Xml;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Moq;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    internal class Utils
    {
        private static DbProviderServices _sqlProviderServices;
        private static Type _sqlProviderServicesType;

        /// <summary>
        /// Gets SqlProviderServices via DependencyResolver.
        /// The DependencyResolver handles provider resolution using LegacyDbProviderServicesWrapper when necessary.
        /// </summary>
        public static DbProviderServices SqlProviderServicesInstance
        {
            get
            {
                _sqlProviderServices ??= DependencyResolver.GetService<DbProviderServices>("System.Data.SqlClient");
                return _sqlProviderServices;
            }
        }

        /// <summary>
        /// Gets the SqlProviderServices type dynamically.
        /// </summary>
        public static Type SqlProviderServicesType
        {
            get
            {
                if (_sqlProviderServicesType == null)
                {
                    _sqlProviderServicesType = Type.GetType(
                        "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer");
                }
                return _sqlProviderServicesType;
            }
        }

        public static StoreItemCollection CreateStoreItemCollection(string ssdl)
        {
            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(SqlProviderServicesInstance);


            return StoreItemCollection.Create(
                new[] { XmlReader.Create(new StringReader(ssdl)) },
                null,
                mockResolver.Object,
                out IList<EdmSchemaError> errors);
        }
    }
}
