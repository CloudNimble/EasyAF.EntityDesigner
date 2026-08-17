// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx
{

    /// <summary>
    ///     Determines which Entity Framework features are available for a given schema (EDMX) version, so that callers
    ///     can show, hide, or disable the corresponding designer UI and model operations.
    /// </summary>
    /// <remarks>
    ///     Historically the designer supported schema V1 (EF 3.5), V1.1, V2 (EF 4), and V3 (EF 5/6), and every method
    ///     here compared the artifact's schema version against the version in which the feature was introduced. This
    ///     product now supports only <see cref="EntityFrameworkVersion.Version3" /> (EF6), which is a superset of all
    ///     earlier versions, so every feature is unconditionally
    ///     <see cref="FeatureState.VisibleAndEnabled" />. The methods and their version parameters are retained
    ///     deliberately: call sites across the designer are written against this gate, and keeping it centralized means
    ///     a future version split can be reinstated here rather than re-scattered through the UI. Each method documents
    ///     the schema version that originally introduced its feature so that intent is not lost.
    /// </remarks>
    internal static class EdmFeatureManager
    {

        #region Internal Methods

        /// <summary>
        ///     Returns the feature state for composable function imports (the <c>IsComposable</c> attribute on
        ///     <c>FunctionImport</c>, which allows a store function to be used inside a LINQ query).
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Composable function imports were introduced in schema V3 (EF 5); on V1/V2 artifacts this returned
        ///     <see cref="FeatureState.Invisible" />.
        /// </remarks>
        internal static FeatureState GetComposableFunctionImportFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for the <c>TypeAccess</c> attribute on <c>EntityContainer</c>, which controls
        ///     the visibility (public/internal) of the generated object context type.
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     The code-generation access annotations arrived with schema V2 (EF 4); V1 artifacts hid this property in
        ///     the property window.
        /// </remarks>
        internal static FeatureState GetEntityContainerTypeAccessFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for enum types, resolved from the artifact rather than from a bare version.
        /// </summary>
        /// <param name="artifact">The artifact whose schema version determines enum support. Must not be <see langword="null" />.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     This overload exists for call sites that hold an artifact but not its schema version; it is equivalent to
        ///     calling <see cref="GetEnumTypeFeatureState(Version)" /> with the artifact's schema version. Enum types
        ///     were introduced in schema V3 (EF 5).
        /// </remarks>
        internal static FeatureState GetEnumTypeFeatureState(EFArtifact artifact)
        {
            Debug.Assert(artifact is not null, "artifact is not null");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for enum types in the conceptual model (the <c>EnumType</c> element and enum
        ///     property types).
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Enum types were introduced in schema V3 (EF 5); on V1/V2 artifacts the designer hid all enum-related
        ///     commands and property editors.
        /// </remarks>
        internal static FeatureState GetEnumTypeFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for foreign keys exposed in the conceptual model (foreign key associations,
        ///     as opposed to independent associations).
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Foreign keys in the conceptual model (the <c>ReferentialConstraint</c> on a conceptual association) were
        ///     introduced in schema V2 (EF 4); V1 models supported independent associations only, so reverse engineering
        ///     and the "include foreign key columns" option were unavailable there.
        /// </remarks>
        internal static FeatureState GetForeignKeysInModelFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for the "Get Column Information" functionality in the function import
        ///     dialogs, which queries the store for the shape of a stored procedure's result set.
        /// </summary>
        /// <param name="artifact">The artifact whose schema version determines support. Must not be <see langword="null" />.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Retrieving result-set column information depends on the store schema definition shipped with schema V3
        ///     (EF 5) providers, so this button was hidden for earlier artifacts. It takes an artifact because the
        ///     dialog resolves the provider from the artifact rather than from a version alone.
        /// </remarks>
        internal static FeatureState GetFunctionImportColumnInformationFeatureState(EFArtifact artifact)
        {
            Debug.Assert(artifact is not null, "artifact is not null");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for function import result mapping (the <c>FunctionImportMapping</c> element
        ///     that maps stored procedure results onto entity or complex types).
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Explicit function import result mapping arrived with schema V2 (EF 4); V1 artifacts could declare
        ///     function imports but not map their results, so the mapping details window suppressed this feature.
        /// </remarks>
        internal static FeatureState GetFunctionImportMappingFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for function imports whose return type is a complex type.
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Complex type return values for function imports were introduced in schema V2 (EF 4); on V1 artifacts the
        ///     designer restricted the return type picker to entity types and scalars.
        /// </remarks>
        internal static FeatureState GetFunctionImportReturningComplexTypeFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3); kept for backward compatibility with old files
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for the <c>GenerateUpdateViews</c> attribute on <c>EntityContainerMapping</c>,
        ///     which suppresses update view generation for read-only mappings.
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     The attribute was introduced in the schema V3 (EF 5) mapping schema; earlier artifacts always generated
        ///     update views and the property was hidden.
        /// </remarks>
        internal static FeatureState GetGenerateUpdateViewsFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for the <c>LazyLoadingEnabled</c> attribute on <c>EntityContainer</c>, which
        ///     sets the default deferred-loading behaviour of the generated context.
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Lazy loading was introduced in schema V2 (EF 4); V1 artifacts had no such attribute, so the property was
        ///     hidden rather than merely disabled.
        /// </remarks>
        internal static FeatureState GetLazyLoadingFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        /// <summary>
        ///     Returns the feature state for the <c>UseStrongSpatialTypes</c> annotation, which controls whether
        ///     spatial (geography/geometry) properties are surfaced as strongly typed CLR members.
        /// </summary>
        /// <param name="schemaVersion">The schema version of the artifact being edited.</param>
        /// <returns>
        ///     <see cref="FeatureState.VisibleAndEnabled" />, because schema V3 (EF 5/6) is the only supported version.
        /// </returns>
        /// <remarks>
        ///     Spatial types were introduced in schema V3 (EF 5); on V1/V2 artifacts spatial primitives did not exist
        ///     and this annotation was invisible.
        /// </remarks>
        internal static FeatureState GetUseStrongSpatialTypesFeatureState(Version schemaVersion)
        {
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");

            // Always enabled for EF6 (Version3)
            return FeatureState.VisibleAndEnabled;
        }

        #endregion

    }

}
