// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{

    /// <summary>
    /// Translates Entity Framework schema versions into the XML namespaces used by the CSDL, MSL, SSDL, and EDMX
    /// sections of an EDMX document, and back again.
    /// </summary>
    /// <remarks>
    /// Every artifact inside an EDMX file is versioned by its XML namespace rather than by an explicit version
    /// attribute, so the namespace is the authoritative statement of which Entity Framework schema a document
    /// conforms to. This designer targets Entity Framework 6 exclusively, which corresponds to
    /// <see cref="EntityFrameworkVersion.Version3" /> and the "2009/11" family of namespaces. The older V1
    /// ("2006/04" and "2007/05") and V2 ("2008/09") namespaces are deliberately not registered, so a document that
    /// uses them will not resolve here.
    /// <para>
    /// The version-keyed dictionaries are populated once by the static constructor and are never mutated afterwards,
    /// which makes reads from the shared, process-wide instance safe from any thread. This type is consumed by the
    /// headless renderer as well as by the Visual Studio package, so it must not take a dependency on the shell.
    /// </para>
    /// </remarks>
    internal static class SchemaManager
    {

        #region Fields

        /// <summary>
        /// The XML namespace for Entity Framework annotations, such as <c>StoreGeneratedPattern</c> and
        /// <c>LazyLoadingEnabled</c>, that decorate CSDL elements.
        /// </summary>
        /// <remarks>
        /// Unlike the CSDL, MSL, SSDL, and EDMX namespaces, the annotation namespace was never re-versioned: the same
        /// "2009/02" value applies to every schema version, which is why it is a single constant rather than a
        /// version-keyed lookup.
        /// </remarks>
        public const string AnnotationNamespace = "http://schemas.microsoft.com/ado/2009/02/edm/annotation";

        /// <summary>
        /// The XML namespace for code-generation attributes, such as <c>TypeAccess</c> and <c>SetterAccess</c>, that
        /// control the visibility of generated members.
        /// </summary>
        /// <remarks>
        /// This namespace is version independent; the "2006/04" value is used by every schema version.
        /// </remarks>
        public const string CodeGenerationNamespace = "http://schemas.microsoft.com/ado/2006/04/codegeneration";

        /// <summary>
        /// The XML namespace applied to the SSDL elements that the store schema generator emits when reverse
        /// engineering a database, carrying provenance such as the originating table or view name.
        /// </summary>
        /// <remarks>
        /// This namespace is version independent; the "2007/12" value is used by every schema version.
        /// </remarks>
        public const string EntityStoreSchemaGeneratorNamespace =
            "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator";

        /// <summary>
        /// The XML namespace for provider manifest documents, which describe the primitive types and functions a
        /// specific ADO.NET provider supports.
        /// </summary>
        /// <remarks>
        /// This namespace is version independent; the "2006/04" value is used by every schema version.
        /// </remarks>
        public const string ProviderManifestNamespace = "http://schemas.microsoft.com/ado/2006/04/edm/providermanifest";

        /// <summary>
        /// The CSDL namespace names, in the same order as the entries of <see cref="CsdlNamespaces" />.
        /// </summary>
        /// <remarks>
        /// Cached at type initialization so that callers validating a document against every known CSDL namespace do
        /// not project the dictionary on each call.
        /// </remarks>
        private static readonly string[] CsdlNamespaceNames;

        /// <summary>
        /// Maps each supported schema version to the XML namespace of its conceptual model (CSDL) section. Contains a
        /// single entry: <see cref="EntityFrameworkVersion.Version3" /> maps to
        /// "http://schemas.microsoft.com/ado/2009/11/edm".
        /// </summary>
        private static readonly IDictionary<Version, XNamespace> CsdlNamespaces;

        /// <summary>
        /// The EDMX namespace names, in the same order as the entries of <see cref="EdmxNamespaces" />.
        /// </summary>
        /// <remarks>
        /// Cached at type initialization so that callers validating a document against every known EDMX namespace do
        /// not project the dictionary on each call.
        /// </remarks>
        private static readonly string[] EdmxNamespaceNames;

        /// <summary>
        /// Maps each supported schema version to the XML namespace of the EDMX wrapper document. Contains a single
        /// entry: <see cref="EntityFrameworkVersion.Version3" /> maps to
        /// "http://schemas.microsoft.com/ado/2009/11/edmx".
        /// </summary>
        private static readonly IDictionary<Version, XNamespace> EdmxNamespaces;

        /// <summary>
        /// The MSL namespace names, in the same order as the entries of <see cref="MslNamespaces" />.
        /// </summary>
        /// <remarks>
        /// Cached at type initialization so that callers validating a document against every known MSL namespace do
        /// not project the dictionary on each call.
        /// </remarks>
        private static readonly string[] MslNamespaceNames;

        /// <summary>
        /// Maps each supported schema version to the XML namespace of its mapping (MSL) section. Contains a single
        /// entry: <see cref="EntityFrameworkVersion.Version3" /> maps to
        /// "http://schemas.microsoft.com/ado/2009/11/mapping/cs".
        /// </summary>
        private static readonly IDictionary<Version, XNamespace> MslNamespaces;

        /// <summary>
        /// The inverse of the four version-keyed namespace dictionaries, mapping any known CSDL, MSL, SSDL, or EDMX
        /// namespace back to the schema version that introduced it.
        /// </summary>
        /// <remarks>
        /// Building the reverse index once at type initialization keeps <see cref="GetSchemaVersion" /> a hash lookup
        /// rather than a linear scan across four dictionaries. Because each namespace string is unique to exactly one
        /// version and section, the inversion is unambiguous and adding a duplicate key would throw during type
        /// initialization, surfacing a mapping mistake immediately.
        /// </remarks>
        private static readonly IDictionary<XNamespace, Version> NamespaceToVersionReverseLookUp;

        /// <summary>
        /// The SSDL namespace names, in the same order as the entries of <see cref="SsdlNamespaces" />.
        /// </summary>
        /// <remarks>
        /// Cached at type initialization so that callers validating a document against every known SSDL namespace do
        /// not project the dictionary on each call.
        /// </remarks>
        private static readonly string[] SsdlNamespaceNames;

        /// <summary>
        /// Maps each supported schema version to the XML namespace of its storage model (SSDL) section. Contains a
        /// single entry: <see cref="EntityFrameworkVersion.Version3" /> maps to
        /// "http://schemas.microsoft.com/ado/2009/11/edm/ssdl".
        /// </summary>
        private static readonly IDictionary<Version, XNamespace> SsdlNamespaces;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the version-to-namespace tables, their cached name arrays, and the reverse lookup index.
        /// </summary>
        /// <remarks>
        /// The tables are sized for exactly one entry because only <see cref="EntityFrameworkVersion.Version3" />
        /// (Entity Framework 6) is supported. Should support for additional versions ever be restored, each dictionary
        /// gains an entry and the array size grows accordingly; nothing else in this type needs to change.
        /// </remarks>
        static SchemaManager()
        {
            // Only Version3 (EF6) namespaces are supported
            const short arraySize = 1;

            CsdlNamespaces = new Dictionary<Version, XNamespace>(arraySize)
            {
                { EntityFrameworkVersion.Version3, XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm") }
            };
            CsdlNamespaceNames = CsdlNamespaces.Select(n => n.Value.NamespaceName).ToArray();

            MslNamespaces = new Dictionary<Version, XNamespace>(arraySize)
            {
                { EntityFrameworkVersion.Version3, XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/mapping/cs") }
            };
            MslNamespaceNames = MslNamespaces.Select(n => n.Value.NamespaceName).ToArray();

            SsdlNamespaces = new Dictionary<Version, XNamespace>(arraySize)
            {
                { EntityFrameworkVersion.Version3, XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm/ssdl") }
            };
            SsdlNamespaceNames = SsdlNamespaces.Select(n => n.Value.NamespaceName).ToArray();

            EdmxNamespaces = new Dictionary<Version, XNamespace>(arraySize)
            {
                { EntityFrameworkVersion.Version3, XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edmx") }
            };
            EdmxNamespaceNames = EdmxNamespaces.Select(n => n.Value.NamespaceName).ToArray();

            NamespaceToVersionReverseLookUp = new Dictionary<XNamespace, Version>();
            foreach (var kvp in CsdlNamespaces.Concat(MslNamespaces).Concat(SsdlNamespaces).Concat(EdmxNamespaces))
            {
                NamespaceToVersionReverseLookUp.Add(kvp.Value, kvp.Key);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets every XML namespace that can appear in an EDMX document of the specified schema version, including the
        /// version-independent provider manifest, code generation, and annotation namespaces.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// An array containing the EDMX, CSDL, SSDL, provider manifest, code generation, MSL, and annotation namespace
        /// names, in that order.
        /// </returns>
        /// <remarks>
        /// The returned order is not significant to callers, which use the array as a set when configuring validation
        /// or namespace filtering. The entity store schema generator namespace is intentionally excluded because it
        /// appears only in artifacts produced by reverse engineering, not in every EDMX document.
        /// </remarks>
        internal static string[] GetAllNamespacesForVersion(Version schemaVersion)
        {
            return new[]
                {
                    GetEDMXNamespaceName(schemaVersion),
                    GetCSDLNamespaceName(schemaVersion),
                    GetSSDLNamespaceName(schemaVersion),
                    GetProviderManifestNamespaceName(),
                    GetCodeGenerationNamespaceName(),
                    GetMSLNamespaceName(schemaVersion),
                    GetAnnotationNamespaceName()
                };
        }

        /// <summary>
        /// Gets the XML namespace name used by Entity Framework annotations.
        /// </summary>
        /// <returns>The value of <see cref="AnnotationNamespace" />, which is the same for every schema version.</returns>
        /// <remarks>
        /// Exposed as a method rather than used directly so that call sites read uniformly alongside the
        /// version-dependent lookups, and so a future version split can be absorbed here without touching callers.
        /// </remarks>
        internal static string GetAnnotationNamespaceName()
        {
            return AnnotationNamespace;
        }

        /// <summary>
        /// Gets the XML namespace name used by code-generation attributes.
        /// </summary>
        /// <returns>The value of <see cref="CodeGenerationNamespace" />, which is the same for every schema version.</returns>
        /// <remarks>
        /// Exposed as a method rather than used directly so that call sites read uniformly alongside the
        /// version-dependent lookups, and so a future version split can be absorbed here without touching callers.
        /// </remarks>
        internal static string GetCodeGenerationNamespaceName()
        {
            return CodeGenerationNamespace;
        }

        /// <summary>
        /// Gets the conceptual model (CSDL) XML namespace name for the specified schema version.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// "http://schemas.microsoft.com/ado/2009/11/edm" for <see cref="EntityFrameworkVersion.Version3" />.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> is not a supported schema version. In debug builds this is preceded by an
        /// assertion failure.
        /// </exception>
        internal static string GetCSDLNamespaceName(Version schemaVersion)
        {
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");

            return GetNamespaceName(schemaVersion, CsdlNamespaces);
        }

        /// <summary>
        /// Gets the conceptual model (CSDL) XML namespace names for every supported schema version.
        /// </summary>
        /// <returns>
        /// The cached array of CSDL namespace names, currently containing only the
        /// <see cref="EntityFrameworkVersion.Version3" /> namespace.
        /// </returns>
        /// <remarks>
        /// The cached array instance is returned directly rather than copied, so callers must treat it as read only;
        /// mutating an element would corrupt the mapping for the lifetime of the process.
        /// </remarks>
        internal static string[] GetCSDLNamespaceNames()
        {
            return CsdlNamespaceNames;
        }

        /// <summary>
        /// Creates an <see cref="XmlNamespaceManager" /> preloaded with the prefixes used by XPath queries against an
        /// EDMX document of the specified schema version.
        /// </summary>
        /// <param name="xmlNameTable">The name table the manager should use, typically the one owned by the document being queried.</param>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// A namespace manager binding the prefixes <c>edmx</c>, <c>csdl</c>, <c>essg</c>, <c>ssdl</c>, and <c>msl</c>
        /// to the corresponding namespaces for <paramref name="schemaVersion" />.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> is not a supported schema version. In debug builds this is preceded by an
        /// assertion failure.
        /// </exception>
        /// <remarks>
        /// Sharing the caller's name table matters for performance: XPath evaluation compares atomized strings by
        /// reference, so a manager built over a foreign name table forces slower value comparisons. The annotation,
        /// code generation, and provider manifest namespaces are not registered because no XPath query in the designer
        /// targets them.
        /// </remarks>
        internal static XmlNamespaceManager GetEdmxNamespaceManager(XmlNameTable xmlNameTable, Version schemaVersion)
        {
            Debug.Assert(xmlNameTable is not null, "xmlNameTable != null");
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlNameTable);
            nsMgr.AddNamespace("edmx", GetEDMXNamespaceName(schemaVersion));
            nsMgr.AddNamespace("csdl", GetCSDLNamespaceName(schemaVersion));
            nsMgr.AddNamespace("essg", GetEntityStoreSchemaGeneratorNamespaceName());
            nsMgr.AddNamespace("ssdl", GetSSDLNamespaceName(schemaVersion));
            nsMgr.AddNamespace("msl", GetMSLNamespaceName(schemaVersion));
            return nsMgr;
        }

        /// <summary>
        /// Gets the EDMX wrapper XML namespace name for the specified schema version.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// "http://schemas.microsoft.com/ado/2009/11/edmx" for <see cref="EntityFrameworkVersion.Version3" />.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> is not a supported schema version. In debug builds this is preceded by an
        /// assertion failure.
        /// </exception>
        /// <remarks>
        /// This is the namespace of the outermost <c>Edmx</c> element and is what identifies the version of a document
        /// as a whole, so it is the value to inspect first when probing an unknown file.
        /// </remarks>
        internal static string GetEDMXNamespaceName(Version schemaVersion)
        {
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");

            return GetNamespaceName(schemaVersion, EdmxNamespaces);
        }

        /// <summary>
        /// Gets the EDMX wrapper XML namespace names for every supported schema version.
        /// </summary>
        /// <returns>
        /// The cached array of EDMX namespace names, currently containing only the
        /// <see cref="EntityFrameworkVersion.Version3" /> namespace.
        /// </returns>
        /// <remarks>
        /// The cached array instance is returned directly rather than copied, so callers must treat it as read only;
        /// mutating an element would corrupt the mapping for the lifetime of the process.
        /// </remarks>
        internal static string[] GetEDMXNamespaceNames()
        {
            return EdmxNamespaceNames;
        }

        /// <summary>
        /// Gets the XML namespace name applied to SSDL elements emitted by the store schema generator.
        /// </summary>
        /// <returns>
        /// The value of <see cref="EntityStoreSchemaGeneratorNamespace" />, which is the same for every schema version.
        /// </returns>
        /// <remarks>
        /// Exposed as a method rather than used directly so that call sites read uniformly alongside the
        /// version-dependent lookups, and so a future version split can be absorbed here without touching callers.
        /// </remarks>
        internal static string GetEntityStoreSchemaGeneratorNamespaceName()
        {
            return EntityStoreSchemaGeneratorNamespace;
        }

        /// <summary>
        /// Gets the mapping (MSL) XML namespace name for the specified schema version.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// "http://schemas.microsoft.com/ado/2009/11/mapping/cs" for <see cref="EntityFrameworkVersion.Version3" />.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> is not a supported schema version. In debug builds this is preceded by an
        /// assertion failure.
        /// </exception>
        /// <remarks>
        /// The trailing "/cs" segment denotes conceptual-to-storage mapping and is part of the namespace, not an
        /// optional suffix; omitting it produces a namespace that no supported version recognizes.
        /// </remarks>
        internal static string GetMSLNamespaceName(Version schemaVersion)
        {
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");

            return GetNamespaceName(schemaVersion, MslNamespaces);
        }

        /// <summary>
        /// Gets the mapping (MSL) XML namespace names for every supported schema version.
        /// </summary>
        /// <returns>
        /// The cached array of MSL namespace names, currently containing only the
        /// <see cref="EntityFrameworkVersion.Version3" /> namespace.
        /// </returns>
        /// <remarks>
        /// The cached array instance is returned directly rather than copied, so callers must treat it as read only;
        /// mutating an element would corrupt the mapping for the lifetime of the process.
        /// </remarks>
        internal static string[] GetMSLNamespaceNames()
        {
            return MslNamespaceNames;
        }

        /// <summary>
        /// Gets the XML namespace name used by provider manifest documents.
        /// </summary>
        /// <returns>The value of <see cref="ProviderManifestNamespace" />, which is the same for every schema version.</returns>
        /// <remarks>
        /// Exposed as a method rather than used directly so that call sites read uniformly alongside the
        /// version-dependent lookups, and so a future version split can be absorbed here without touching callers.
        /// </remarks>
        internal static string GetProviderManifestNamespaceName()
        {
            return ProviderManifestNamespace;
        }

        /// <summary>
        /// Determines the Entity Framework schema version that corresponds to a CSDL, MSL, SSDL, or EDMX XML namespace.
        /// </summary>
        /// <param name="xNamespace">The namespace to resolve. May be <see langword="null" />.</param>
        /// <returns>
        /// The schema version associated with <paramref name="xNamespace" />, or
        /// <see cref="EntityFrameworkVersion.Version3" /> when the namespace is <see langword="null" /> or unrecognized.
        /// </returns>
        /// <remarks>
        /// This method never throws or reports failure: because Version3 is the only supported version, falling back to
        /// it lets callers continue to operate on documents that carry a legacy or malformed namespace instead of
        /// aborting the load. Callers that must distinguish a genuine V3 namespace from an unrecognized one cannot rely
        /// on the return value alone and should compare the namespace against
        /// <see cref="GetEDMXNamespaceNames" /> and its siblings first.
        /// </remarks>
        internal static Version GetSchemaVersion(XNamespace xNamespace)
        {
            // Return Version3 as fallback since it's the only supported version
            return xNamespace is not null && NamespaceToVersionReverseLookUp.TryGetValue(xNamespace, out Version schemaVersion)
                       ? schemaVersion
                       : EntityFrameworkVersion.Version3;
        }

        /// <summary>
        /// Gets the storage model (SSDL) XML namespace name for the specified schema version.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version. Must be <see cref="EntityFrameworkVersion.Version3" />.</param>
        /// <returns>
        /// "http://schemas.microsoft.com/ado/2009/11/edm/ssdl" for <see cref="EntityFrameworkVersion.Version3" />.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> is not a supported schema version. In debug builds this is preceded by an
        /// assertion failure.
        /// </exception>
        /// <remarks>
        /// The SSDL namespace shares the CSDL namespace's "2009/11/edm" prefix and differs only by the trailing
        /// "/ssdl" segment, so a prefix match is not sufficient to tell the two sections apart.
        /// </remarks>
        internal static string GetSSDLNamespaceName(Version schemaVersion)
        {
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");

            return GetNamespaceName(schemaVersion, SsdlNamespaces);
        }

        /// <summary>
        /// Gets the storage model (SSDL) XML namespace names for every supported schema version.
        /// </summary>
        /// <returns>
        /// The cached array of SSDL namespace names, currently containing only the
        /// <see cref="EntityFrameworkVersion.Version3" /> namespace.
        /// </returns>
        /// <remarks>
        /// The cached array instance is returned directly rather than copied, so callers must treat it as read only;
        /// mutating an element would corrupt the mapping for the lifetime of the process.
        /// </remarks>
        internal static string[] GetSSDLNamespaceNames()
        {
            return SsdlNamespaceNames;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Looks up the namespace name registered for a schema version in one of the version-keyed namespace tables.
        /// </summary>
        /// <param name="schemaVersion">The Entity Framework schema version to look up.</param>
        /// <param name="xNamespaces">The table to search, one of the CSDL, MSL, SSDL, or EDMX namespace dictionaries.</param>
        /// <returns>The namespace name registered for <paramref name="schemaVersion" /> in <paramref name="xNamespaces" />.</returns>
        /// <exception cref="KeyNotFoundException">
        /// <paramref name="schemaVersion" /> has no entry in <paramref name="xNamespaces" />.
        /// </exception>
        /// <remarks>
        /// A missing entry is treated as a programming error rather than a recoverable condition: every public entry
        /// point is documented as accepting only supported versions, so the assertions fail loudly during development
        /// and the indexer throws in release builds instead of silently substituting a namespace from the wrong
        /// version, which would produce an EDMX document that no runtime can load.
        /// </remarks>
        private static string GetNamespaceName(Version schemaVersion, IDictionary<Version, XNamespace> xNamespaces)
        {
            Debug.Assert(schemaVersion is not null, "schemaVersion != null");
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(schemaVersion), "Invalid schema version.");
            Debug.Assert(xNamespaces is not null, "xNamespaces != null");
            Debug.Assert(xNamespaces.ContainsKey(schemaVersion), "The requested namespace is not found");

            return xNamespaces[schemaVersion].NamespaceName;
        }

        #endregion

    }

}
