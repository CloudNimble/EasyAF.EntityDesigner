// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    internal class DbProviderServicesResolver : IDbDependencyResolver
    {
        private readonly Dictionary<string, Type> _providerServicesRegistrar = [];

        public void Register(Type type, string invariantName)
        {
            Debug.Assert(type != null, "type != null");
            Debug.Assert(IsDbProviderServicesType(type), "expected type derived from DbProviderServices");
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invariantName cannot be null or empty string");

            _providerServicesRegistrar[invariantName] = type;
        }

        private static bool IsDbProviderServicesType(Type type)
        {
            if (type == null) return false;
            if (typeof(DbProviderServices).IsAssignableFrom(type)) return true;

            var current = type;
            while (current != null)
            {
                if (current.FullName == "System.Data.Entity.Core.Common.DbProviderServices")
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        public void Unregister(string invariantName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invariantName cannot be null or empty string");
            _providerServicesRegistrar.Remove(invariantName);
        }

        public object GetService(Type type, object key)
        {
            if (type != typeof(DbProviderServices) || key is not string providerInvariantName)
                return null;

            if (_providerServicesRegistrar.TryGetValue(providerInvariantName, out var providerServicesType))
            {
                return CreateProviderInstance(providerServicesType);
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            var service = GetService(type, key);
            return service != null ? [service] : [];
        }

        private static DbProviderServices CreateProviderInstance(Type providerType)
        {
            Debug.Assert(providerType != null, "providerType != null");
            Debug.Assert(IsDbProviderServicesType(providerType), "expected type derived from DbProviderServices");

            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var instanceMember = providerType.GetProperty("Instance", bindingFlags)
                                 ?? (MemberInfo)providerType.GetField("Instance", bindingFlags);

            if (instanceMember == null)
                return null;

            var instanceValue = instanceMember switch
            {
                PropertyInfo prop => prop.GetValue(null, null),
                FieldInfo field => field.GetValue(null),
                _ => null
            };

            return instanceValue as DbProviderServices;
        }
    }
}
