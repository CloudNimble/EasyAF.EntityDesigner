// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reflection;

namespace Microsoft.Data.Entity.Design.VersioningFacade.Metadata
{
    /// <summary>
    /// Helper class for accessing internal EntityFramework metadata methods via reflection.
    /// </summary>
    internal static class MetadataItemHelper
    {
        public const string SchemaInvalidMetadataPropertyName = "EdmSchemaInvalid";
        public const string SchemaErrorsMetadataPropertyName = "EdmSchemaErrors";

        private static readonly Lazy<MethodInfo> IsInvalidMethod = new Lazy<MethodInfo>(() =>
        {
            var helperType = typeof(MetadataItem).Assembly.GetType(
                "System.Data.Entity.Core.Metadata.Edm.MetadataItemHelper", throwOnError: false);
            return helperType?.GetMethod("IsInvalid", BindingFlags.Public | BindingFlags.Static);
        });

        private static readonly Lazy<MethodInfo> HasSchemaErrorsMethod = new Lazy<MethodInfo>(() =>
        {
            var helperType = typeof(MetadataItem).Assembly.GetType(
                "System.Data.Entity.Core.Metadata.Edm.MetadataItemHelper", throwOnError: false);
            return helperType?.GetMethod("HasSchemaErrors", BindingFlags.Public | BindingFlags.Static);
        });

        private static readonly Lazy<MethodInfo> GetSchemaErrorsMethod = new Lazy<MethodInfo>(() =>
        {
            var helperType = typeof(MetadataItem).Assembly.GetType(
                "System.Data.Entity.Core.Metadata.Edm.MetadataItemHelper", throwOnError: false);
            return helperType?.GetMethod("GetSchemaErrors", BindingFlags.Public | BindingFlags.Static);
        });

        /// <summary>
        /// Checks if a metadata item has been marked as invalid.
        /// </summary>
        public static bool IsInvalid(MetadataItem item)
        {
            if (item == null) return false;

            // Try reflection first (for EF6 internal API)
            if (IsInvalidMethod.Value != null)
            {
                try
                {
                    return (bool)IsInvalidMethod.Value.Invoke(null, new object[] { item });
                }
                catch (TargetInvocationException)
                {
                    // Fall through to property check
                }
            }

            // Fallback: check for EdmSchemaInvalid property
            var invalidProperty = item.MetadataProperties
                .FirstOrDefault(p => p.Name == SchemaInvalidMetadataPropertyName);
            return invalidProperty != null && invalidProperty.Value is bool b && b;
        }

        /// <summary>
        /// Checks if a metadata item has schema errors.
        /// </summary>
        public static bool HasSchemaErrors(MetadataItem item)
        {
            if (item == null) return false;

            // Try reflection first
            if (HasSchemaErrorsMethod.Value != null)
            {
                try
                {
                    return (bool)HasSchemaErrorsMethod.Value.Invoke(null, new object[] { item });
                }
                catch (TargetInvocationException)
                {
                    // Fall through to property check
                }
            }

            // Fallback: check for EdmSchemaErrors property
            var errorsProperty = item.MetadataProperties
                .FirstOrDefault(p => p.Name == SchemaErrorsMetadataPropertyName);
            return errorsProperty?.Value is IEnumerable<string> errors && errors.Any();
        }

        /// <summary>
        /// Gets schema errors from a metadata item.
        /// </summary>
        public static IEnumerable<EdmSchemaError> GetSchemaErrors(MetadataItem item)
        {
            if (item == null) return Enumerable.Empty<EdmSchemaError>();

            // Try reflection first
            if (GetSchemaErrorsMethod.Value != null)
            {
                try
                {
                    var result = GetSchemaErrorsMethod.Value.Invoke(null, new object[] { item });
                    if (result is IEnumerable<EdmSchemaError> errors)
                        return errors;
                }
                catch (TargetInvocationException)
                {
                    // Fall through to property check
                }
            }

            // Fallback: check for EdmSchemaErrors property
            var errorsProperty = item.MetadataProperties
                .FirstOrDefault(p => p.Name == SchemaErrorsMetadataPropertyName);
            return errorsProperty?.Value as IEnumerable<EdmSchemaError> ?? Enumerable.Empty<EdmSchemaError>();
        }
    }
}
