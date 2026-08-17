// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.DataTools.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VSDesigner.Data.Local;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Constants = EnvDTE.Constants;
// Before this file moved to the Microsoft.VisualStudio.* namespace, the unqualified name bound to the type in this
// file's own namespace. Now both candidates arrive via using directives, so the intended one is named explicitly.
using ModelChangeEventArgs = Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package.ModelChangeEventArgs;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package
{

    /// <summary>
    /// Allows interaction with App.Config and Web.Config. It stores a "project dictionary" where each bucket corresponds
    /// to a dictionary that associates entity container names with their corresponding connection strings, stored as
    /// <see cref="ConnectionString" /> objects. The dictionaries should mirror App.Config exactly.
    /// </summary>
    /// <remarks>
    /// The in-memory dictionary is the authority the designer validates against, so it is deliberately re-extracted from
    /// the .config file before most mutations: the user may have hand-edited the file behind the designer's back, and
    /// writing a stale hash back out would silently discard those edits.
    /// </remarks>
    internal class ConnectionManager : IDisposable
    {

        #region Fields

        /// <summary>
        /// The prefix used in an EntityClient metadata path to indicate that the CSDL/SSDL/MSL artifacts are embedded as
        /// resources in the output assembly rather than deployed as loose files.
        /// </summary>
        internal static readonly string EmbedAsResourcePrefix = "res://*";

        /// <summary>
        /// The SQL Database File data source that replaces
        /// <see cref="PreUpgradeSqlDatabaseFileConnectionStringDataSource" /> when a connection string is upgraded.
        /// </summary>
        internal static readonly string PostUpgradeSqlDatabaseFileConnectionStringDataSource = "Data Source=(LocalDB)\\v11.0";

        /// <summary>
        /// The legacy SQL Express data source that older SQL Database File connection strings used before LocalDB existed.
        /// </summary>
        internal static readonly string PreUpgradeSqlDatabaseFileConnectionStringDataSource = "data source=.\\sqlexpress";

        /// <summary>
        /// The cached length of <see cref="PreUpgradeSqlDatabaseFileConnectionStringDataSource" />, used to splice the
        /// replacement data source into a connection string without re-measuring the token on every substitution.
        /// </summary>
        internal static readonly int PreUpgradeSqlDatabaseFileConnectionStringDataSourceLength =
            PreUpgradeSqlDatabaseFileConnectionStringDataSource.Length;

        /// <summary>
        /// The provider fragment identifying a SQL Server Compact 3.5 provider connection string.
        /// </summary>
        internal static readonly string SqlCe35ConnectionStringProvider = "provider=System.Data.SqlServerCe.3.5";

        /// <summary>
        /// The provider fragment identifying a SQL Server Compact 4.0 provider connection string.
        /// </summary>
        internal static readonly string SqlCe40ConnectionStringProvider = "provider=System.Data.SqlServerCe.4.0";

        /// <summary>
        /// The ADO.NET invariant name of the SQL Server client provider.
        /// </summary>
        internal static readonly string SqlClientProviderName = "System.Data.SqlClient";

        /// <summary>
        /// The connection string keyword that requests a SQL Express user instance. It is stripped when a SQL Database
        /// File connection string is upgraded because LocalDB does not support user instances.
        /// </summary>
        internal static readonly string SqlDatabaseFileConnectionStringUserInstance = "user instance=true";

        /// <summary>
        /// The cached length of <see cref="SqlDatabaseFileConnectionStringUserInstance" />, used to locate the text that
        /// follows the keyword when removing it from a connection string.
        /// </summary>
        internal static readonly int SqlDatabaseFileConnectionStringUserInstanceLength =
            SqlDatabaseFileConnectionStringUserInstance.Length;

        /// <summary>
        /// The name of the XML attribute on a &lt;connectionStrings&gt;/&lt;add&gt; element that carries the connection string itself.
        /// </summary>
        internal static readonly string XmlAttrNameConnectionString = "connectionString";

        /// <summary>
        /// The name of the connection string keyword that enables Multiple Active Result Sets.
        /// </summary>
        internal static readonly string XmlAttrNameMultipleActiveResultSets = "MultipleActiveResultSets";

        /// <summary>
        /// The name of the XML attribute on a &lt;connectionStrings&gt;/&lt;add&gt; element that carries the connection string name.
        /// </summary>
        internal static readonly string XmlAttrNameName = "name";

        /// <summary>
        /// The name of the XML attribute on a &lt;connectionStrings&gt;/&lt;add&gt; element that carries the provider invariant name.
        /// </summary>
        internal static readonly string XmlAttrNameProviderName = "providerName";

        /// <summary>
        /// The XPath that selects every connection string entry in a .config file, regardless of provider.
        /// </summary>
        internal static readonly string XpathConnectionStringsAdd = "configuration/connectionStrings/add";

        /// <summary>
        /// Synchronizes every read and write of the project-to-connection-string hash. It is static because the hash is
        /// rebuilt in response to solution-wide Visual Studio events that can overlap with designer-initiated edits.
        /// </summary>
        private static readonly object _hashSyncRoot = new object();

        /// <summary>
        /// The ADO.NET provider invariant name written into the .config file for Entity Framework connection strings.
        /// </summary>
        private const string Provider = "System.Data.EntityClient";

        /// <summary>
        /// The provider connection string keyword used to report the calling application to the server. Added so server
        /// side statistics can attribute activity to Entity Framework.
        /// </summary>
        private const string ProviderConnectionStringPropertyNameApp = "App";

        /// <summary>
        /// The long form of <see cref="ProviderConnectionStringPropertyNameApp" />. Its presence suppresses the automatic
        /// injection of the App keyword so a user supplied application name is never overwritten.
        /// </summary>
        private const string ProviderConnectionStringPropertyNameApplicationName = "Application Name";

        /// <summary>
        /// The XPath that selects only the EntityClient connection string entries in a .config file, i.e. the subset this
        /// manager owns and is therefore allowed to rewrite.
        /// </summary>
        private const string XpathConnectionStringsAddEntity =
            "configuration/connectionStrings/add[@providerName='" + Provider + "']";

        /// <summary>
        /// Maps each project in the solution to the entity container names and connection strings declared in its .config
        /// file. Null until <see cref="InitializeConnectionStringsHash" /> populates it, and reset to null whenever the
        /// backing .config file is removed.
        /// </summary>
        private Dictionary<Project, Dictionary<string, ConnectionString>> _connStringsByProjectHash;

        /// <summary>
        /// The entity container name as it stood before the user edited it in the property browser. The rename cannot be
        /// applied to the .config file until the model is saved, so the old key is parked here to locate the connection
        /// string at that point.
        /// </summary>
        private string _staleEntityContainerName;

        /// <summary>
        /// The metadata artifact processing value as it stood before the user changed it. It is compared against the
        /// current value on save to decide whether the metadata portion of the connection string must be rewritten.
        /// </summary>
        private string _staleMetadataArtifactProcessing;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the project-to-connection-string hash, building it on first access.
        /// </summary>
        /// <remarks>
        /// Every consumer goes through this property rather than the backing field so that the lazy initialization can
        /// never be skipped, including after the hash has been discarded by a .config file removal.
        /// </remarks>
        private Dictionary<Project, Dictionary<string, ConnectionString>> ConnStringsByProjectHash
        {
            get
            {
                InitializeConnectionStringsHash();
                return _connStringsByProjectHash;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager" /> class and subscribes it to the Visual
        /// Studio model change events it needs in order to keep the .config file in sync.
        /// </summary>
        /// <remarks>
        /// Failure to subscribe is reported to the output window rather than thrown: the package must still load even if
        /// the event listener is unavailable, otherwise the designer becomes completely unusable.
        /// </remarks>
        internal ConnectionManager()
        {
            lock (_hashSyncRoot)
            {
                try
                {
                    RegisterModelListenerEvents();
                }
                catch (Exception e)
                {
                    var s = Resources.ConnectionManager_InitializeError;
                    s = String.Format(CultureInfo.CurrentCulture, s, e.Message);
                    Project project = null;
                    foreach (var p in VsUtils.GetAllProjectsInSolution(PackageManager.Package))
                    {
                        project = p;
                        break;
                    }

                    Debug.Assert(project is not null);
                    if (project is not null)
                    {
                        VsUtils.LogOutputWindowPaneMessage(project, s);
                    }
                }
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ConnectionManager" /> class.
        /// </summary>
        /// <remarks>
        /// Reaching the finalizer means the owner failed to dispose the manager and the event subscriptions were leaked,
        /// which the assert in <see cref="Dispose(bool)" /> flags during development.
        /// </remarks>
        ~ConnectionManager()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a connection string entry to the &lt;connectionStrings&gt; section of the supplied .config document,
        /// creating the section if it is missing.
        /// </summary>
        /// <param name="configXmlDoc">The .config document to modify.</param>
        /// <param name="connStringName">The name to give the new connection string entry.</param>
        /// <param name="connString">The connection string value.</param>
        /// <param name="providerName">The ADO.NET provider invariant name to record on the entry.</param>
        /// <exception cref="XmlException">
        /// Thrown when the document element is not an unqualified &lt;configuration&gt; element, in which case the file is
        /// not a .config file this manager can safely edit.
        /// </exception>
        public static void AddConnectionStringElement(XmlDocument configXmlDoc, string connStringName, string connString, string providerName)
        {
            var connStringsElement = GetConnectionStringsElement(configXmlDoc);
            if (connStringsElement is null)
            {
                // can happen if the document element is not "configuration"
                throw new XmlException(Resources.ConnectionManager_CorruptConfig);
            }

            AddConnectionStringElement(connStringsElement, connStringName, connString, providerName);
        }

        /// <summary>
        /// Builds a connection string that targets the machine's LocalDB instance using integrated security.
        /// </summary>
        /// <param name="initialCatalog">The name of the database to connect to.</param>
        /// <returns>
        /// A SQL Server connection string pointing at the default LocalDB instance.
        /// </returns>
        public static string CreateDefaultLocalDbConnectionString(string initialCatalog)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(initialCatalog), "invalid initial catalog name");

            return
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog={0};Integrated Security=True",
                    initialCatalog);
        }

        /// <summary>
        /// Releases the resources held by this instance and unsubscribes it from the Visual Studio model change events.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Constructs an EntityClient connection string from the supplied provider connection string and adds it to the
        /// hash, pushing the update straight through to the .config file.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="applicationType">The project system, which determines how metadata paths are expressed.</param>
        /// <param name="metadataFiles">The CSDL/SSDL/MSL artifact paths to reference from the connection string.</param>
        /// <param name="connectionStringName">The name to store the connection string under.</param>
        /// <param name="configFileConnectionStringValue">The provider connection string to wrap.</param>
        /// <param name="providerInvariantName">The ADO.NET provider invariant name of the underlying store.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="project" />, <paramref name="connectionStringName" /> or
        /// <paramref name="configFileConnectionStringValue" /> is null or empty.
        /// </exception>
        /// <remarks>
        /// Miscellaneous files "projects" have no .config file of their own, so the request is silently ignored for them.
        /// </remarks>
        internal void AddConnectionString(Project project, VisualStudioProjectSystem applicationType, ICollection<string> metadataFiles, string connectionStringName,
            string configFileConnectionStringValue, string providerInvariantName)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return;
            }

            if (String.IsNullOrEmpty(connectionStringName))
            {
                throw new ArgumentNullException("connectionStringName");
            }

            if (String.IsNullOrEmpty(configFileConnectionStringValue))
            {
                throw new ArgumentNullException("configFileConnectionStringValue");
            }

            var newConfigFileConnString = CreateEntityConnectionString(
                project,
                applicationType,
                metadataFiles,
                configFileConnectionStringValue,
                providerInvariantName);

            // add the connection string to the hash and update the .config file
            AddConnectionString(project, connectionStringName, newConfigFileConnString);
        }

        /// <summary>
        /// Wraps the given provider connection string in an EntityClient connection string.
        /// </summary>
        /// <param name="sqlConnectionString">The provider (for example SQL) connection string to wrap.</param>
        /// <param name="providerInvariantName">The ADO.NET provider invariant name of the underlying store.</param>
        /// <param name="metadataFiles">The CSDL/SSDL/MSL artifact paths to reference from the connection string.</param>
        /// <param name="project">The DTE project the connection string will belong to.</param>
        /// <param name="applicationType">The project system, which determines how metadata paths are expressed.</param>
        /// <returns>
        /// A <see cref="ConnectionString" /> containing the supplied provider connection string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sqlConnectionString" />, <paramref name="providerInvariantName" /> or
        /// <paramref name="project" /> is null or empty.
        /// </exception>
        internal static ConnectionString ConstructConnectionStringObject(
            string sqlConnectionString, string providerInvariantName,
            IEnumerable<string> metadataFiles, Project project, VisualStudioProjectSystem applicationType)
        {
            if (sqlConnectionString is null)
            {
                throw new ArgumentNullException("sqlConnectionString");
            }

            if (String.IsNullOrEmpty(providerInvariantName))
            {
                throw new ArgumentNullException("providerInvariantName");
            }

            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            // Wrap the given sql connection string in a map connection string
            EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder
            {
                Provider = providerInvariantName,
                ProviderConnectionString = sqlConnectionString,
                // we don't want to mess with the model when we are in the process of adding it, so just feed in the default value for metadata artifact processing
                Metadata = GetConnectionStringMetadata(
                    metadataFiles, project, applicationType, GetMetadataArtifactProcessingDefault())
            };

            return new ConnectionString(builder);
        }

        /// <summary>
        /// Returns an entity container name that is unique within the app/web.config for the given project, based on a
        /// proposed name.
        /// </summary>
        /// <param name="proposedEntityContainerName">The desired entity container name.</param>
        /// <param name="project">The DTE project whose .config file defines the names already in use.</param>
        /// <returns>
        /// The proposed name if it is unused, otherwise the proposed name with the lowest available numeric suffix appended.
        /// </returns>
        internal string ConstructUniqueEntityContainerName(string proposedEntityContainerName, Project project)
        {
            Debug.Assert(project is not null, "project in ConstructUniqueEntityContainerName()");
            Debug.Assert(
                proposedEntityContainerName is not null, "Null proposedEntityContainerName in ConstructUniqueEntityContainerName()");

            var entityContainerName = proposedEntityContainerName;
            var suffix = 1;

            InitializeConnectionStringsHash();
            if (ConnStringsByProjectHash is not null
                && ConnStringsByProjectHash.TryGetValue(project, out Dictionary<string, ConnectionString> connStringsInProject))
            {
                // keep incrementing the suffix until the existing connection string names
                // does not contain the result
                while (connStringsInProject.ContainsKey(entityContainerName))
                {
                    entityContainerName = proposedEntityContainerName + suffix;
                    ++suffix;
                }
            }

            return entityContainerName;
        }

        /// <summary>
        /// Creates the EntityClient connection string that should be written to the .config file for a model.
        /// </summary>
        /// <param name="project">The DTE project the connection string will belong to.</param>
        /// <param name="applicationType">The project system, which determines how metadata paths are expressed.</param>
        /// <param name="metadataFiles">The CSDL/SSDL/MSL artifact paths to reference from the connection string.</param>
        /// <param name="configFileConnectionStringValue">The provider connection string to wrap.</param>
        /// <param name="providerInvariantName">The ADO.NET provider invariant name of the underlying store.</param>
        /// <returns>
        /// The EntityClient <see cref="ConnectionString" /> to store in the .config file.
        /// </returns>
        /// <remarks>
        /// The result may omit credentials entirely when the user chose not to store sensitive information.
        /// </remarks>
        internal static ConnectionString CreateEntityConnectionString(
            Project project,
            VisualStudioProjectSystem applicationType,
            IEnumerable<string> metadataFiles,
            string configFileConnectionStringValue,
            string providerInvariantName)
        {
            // note that this connection string may not have credentials if the user chose to not store sensitive info
            return ConstructConnectionStringObject(
                InjectEFAttributesIntoConnectionString(configFileConnectionStringValue, providerInvariantName),
                providerInvariantName, metadataFiles, project, applicationType);
        }

        /// <summary>
        /// Reads the project's .config file and destructively replaces this manager's cached entry for that project.
        /// </summary>
        /// <param name="project">DTE Project that owns App.Config we want to look at.</param>
        /// <param name="createConfig">True to create the .config file when it does not yet exist; false to leave it absent.</param>
        /// <remarks>
        /// The .config file, not the hash, is the source of truth here, so the previous entry is discarded outright rather
        /// than merged. Miscellaneous files "projects" are skipped because they have no .config file.
        /// </remarks>
        internal void ExtractConnStringsIntoHash(Project project, bool createConfig)
        {
            if (VsUtils.IsMiscellaneousProject(project))
            {
                return;
            }

            ConfigFileUtils configFileUtils = new ConfigFileUtils(project, PackageManager.Package);
            if (createConfig)
            {
                configFileUtils.GetOrCreateConfigFile();
            }

            var configXmlDoc = configFileUtils.LoadConfig();
            if (configXmlDoc is not null)
            {
                var xmlNodeList = configXmlDoc.SelectNodes(XpathConnectionStringsAddEntity);

                Dictionary<string, ConnectionString> stringHash = new Dictionary<string, ConnectionString>();
                foreach (XmlNode node in xmlNodeList)
                {
                    ConnectionString connStringObj = new ConnectionString(node.Attributes.GetNamedItem(XmlAttrNameConnectionString).Value);
                    stringHash.Add(node.Attributes.GetNamedItem(XmlAttrNameName).Value, connStringObj);
                }

                // from msdn: UniqueName: This [property] returns a temporary, unique string value that you can use to
                // differentiate one project from another.
                ConnStringsByProjectHash[project] = stringHash;
            }
        }

        /// <summary>
        /// Looks up the connection string stored under the given entity container name for a project.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name the connection string is keyed by.</param>
        /// <returns>
        /// The matching <see cref="ConnectionString" />, or null when the project or the name is unknown.
        /// </returns>
        internal static ConnectionString GetConnectionStringObject(Project project, string entityContainerName)
        {
            return GetConnectionStringObject(project, entityContainerName, PackageManager.Package.ConnectionManager);
        }

        /// <summary>
        /// Looks up the connection string stored under the given entity container name for a project, using the supplied
        /// connection manager instance.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name the connection string is keyed by.</param>
        /// <param name="connectionManager">The manager holding the hash to search.</param>
        /// <returns>
        /// The matching <see cref="ConnectionString" />, or null when the project or the name is unknown.
        /// </returns>
        /// <remarks>
        /// The overload taking an explicit manager exists so callers, including tests, are not forced through the package
        /// singleton.
        /// </remarks>
        internal static ConnectionString GetConnectionStringObject(
            Project project, string entityContainerName, ConnectionManager connectionManager)
        {
            Dictionary<string, ConnectionString> connStringsInProject = null;
            ConnectionString connectionStringObj = null;
            if (project is not null
                && connectionManager is not null)
            {
                if (!connectionManager.ConnStringsByProjectHash.TryGetValue(project, out connStringsInProject))
                {
                    return null;
                }
                connStringsInProject.TryGetValue(entityContainerName, out connectionStringObj);
            }
            return connectionStringObj;
        }

        /// <summary>
        /// Reads every connection string declared in a .config file, keyed by name.
        /// </summary>
        /// <param name="configFileUtils">Provides access to the project's .config file.</param>
        /// <returns>
        /// A dictionary of connection string names to values, empty when the .config file does not exist.
        /// </returns>
        /// <remarks>
        /// All providers are returned, not just EntityClient, because Code First models register connection strings under
        /// their own provider names and those names still have to be treated as taken.
        /// </remarks>
        internal static Dictionary<string, string> GetExistingConnectionStrings(ConfigFileUtils configFileUtils)
        {
            var configXml = configFileUtils.LoadConfig();

            Dictionary<string, string> existingConnectionStrings = new Dictionary<string, string>();
            if (configXml is null)
            {
                // can be null if config does not exist in which case there are no connection strings
                return existingConnectionStrings;
            }

            // note we return all the connection string names to support CodeFirst scenarios
            foreach (var addElement in
                configXml.SelectNodes(XpathConnectionStringsAdd).OfType<XmlElement>())
            {
                var name = addElement.GetAttribute(XmlAttrNameName);
                if (!string.IsNullOrEmpty(name))
                {
                    existingConnectionStrings.Add(name, addElement.GetAttribute(XmlAttrNameConnectionString));
                }
            }

            return existingConnectionStrings;
        }

        /// <summary>
        /// Gets the metadata artifact processing value assigned to newly created models.
        /// </summary>
        /// <returns>
        /// The default metadata artifact processing value.
        /// </returns>
        internal static string GetMetadataArtifactProcessingDefault()
        {
            // for now all projects have "Embed in Output Assembly" as their default
            return ConnectionDesignerInfo.MAP_EmbedInOutputAssembly;
        }

        /// <summary>
        /// Computes the CSDL/SSDL/MSL file names that correspond to an .edmx artifact.
        /// </summary>
        /// <param name="project">The DTE project that contains the artifact.</param>
        /// <param name="filename">The path of the .edmx artifact, possibly URI escaped.</param>
        /// <param name="serviceProvider">The service provider used to resolve project services.</param>
        /// <returns>
        /// The metadata file paths, relative to the project root.
        /// </returns>
        internal static string[] GetMetadataFileNamesFromArtifactFileName(
            Project project, string filename, IServiceProvider serviceProvider)
        {
            return GetMetadataFileNamesFromArtifactFileName(project, filename, serviceProvider, VsUtils.GetProjectItemForDocument);
        }

        /// <summary>
        /// Computes the CSDL/SSDL/MSL file names that correspond to an .edmx artifact, resolving the artifact's project
        /// item through the supplied delegate.
        /// </summary>
        /// <param name="project">The DTE project that contains the artifact.</param>
        /// <param name="filename">The path of the .edmx artifact, possibly URI escaped.</param>
        /// <param name="serviceProvider">The service provider used to resolve project services.</param>
        /// <param name="getProjectItemForDocument">Resolves a document path to its <see cref="ProjectItem" />.</param>
        /// <returns>
        /// The metadata file paths, relative to the project root.
        /// </returns>
        /// <remarks>
        /// The lookup delegate is a parameter so tests can supply the project item without a live Visual Studio shell.
        /// </remarks>
        internal static string[] GetMetadataFileNamesFromArtifactFileName(
            Project project, string filename, IServiceProvider serviceProvider,
            Func<string, IServiceProvider, ProjectItem> getProjectItemForDocument)
        {
            var unescapedArtifactPath = Uri.UnescapeDataString(filename);
            FileInfo edmxFileInfo = new FileInfo(unescapedArtifactPath);
            var modelName = Path.GetFileNameWithoutExtension(edmxFileInfo.FullName);
            var projectRootDirInfo = VsUtils.GetProjectRoot(project, serviceProvider);
            string relativeFolderPath;

            var projectItem = getProjectItemForDocument(edmxFileInfo.FullName, serviceProvider);
            // when generating model from the database project the item will be null
            // since the actual model is generated in the very last step
            if (projectItem is not null)
            {
                // since the given file can be a link, create the directory path by combining parent directories names
                // folowing code will create correct relativeFolderPath regardles whether projectItem is a link or not
                // Example - for following project hierarchy:
                // ProjectRoot
                // |__Folder1
                //    |__Folder2
                //       |__Model.edmx
                // code below will produce "Folder2" path in the first step and "Folder1\Folder2" path in the second (and last) step
                relativeFolderPath = "";
                ProjectItem parentItem = projectItem.Collection.Parent as ProjectItem;
                while (parentItem is not null)
                {
                    relativeFolderPath = Path.Combine(parentItem.Name, relativeFolderPath);
                    parentItem = parentItem.Collection.Parent as ProjectItem;
                }
            }
            else
            {
                relativeFolderPath = EdmUtils.GetRelativePath(edmxFileInfo.Directory, projectRootDirInfo);
            }

            var folderPath = Path.Combine(projectRootDirInfo.FullName, relativeFolderPath);
            return EdmUtils.GetRelativeMetadataPaths(folderPath, project, modelName, EdmUtils.CsdlSsdlMslExtensions, serviceProvider);
        }

        /// <summary>
        /// Extracts the metadata artifact processing designer property from an artifact.
        /// </summary>
        /// <param name="artifact">The artifact whose designer section is inspected.</param>
        /// <returns>
        /// The metadata artifact processing property, or null when the artifact has no connection designer section.
        /// </returns>
        internal static DesignerProperty GetMetadataPropertyFromArtifact(EFArtifact artifact)
        {
            var designerRoot = artifact.DesignerInfo();
            DesignerProperty mapProperty = null;
            if (designerRoot is not null)
            {
                if (designerRoot.TryGetDesignerInfo(ConnectionDesignerInfo.ElementName, out DesignerInfo designerInfo))
                {
                    ConnectionDesignerInfo connectionDesignerInfo = designerInfo as ConnectionDesignerInfo;
                    Debug.Assert(
                        connectionDesignerInfo is not null,
                        "We should have associated the ConnectionDesignerInfo with " + ConnectionDesignerInfo.ElementName);

                    if (connectionDesignerInfo is not null)
                    {
                        mapProperty = connectionDesignerInfo.MetadataArtifactProcessingProperty;
                    }
                }
            }
            return mapProperty;
        }

        /// <summary>
        /// Computes a connection string name that is not already present in the project's .config file.
        /// </summary>
        /// <param name="configFileUtils">Provides access to the project's .config file.</param>
        /// <param name="baseConnectionStringName">The desired connection string name.</param>
        /// <returns>
        /// The base name if it is unused, otherwise the base name with the lowest available numeric suffix appended.
        /// </returns>
        // computes a unique connection string name based on the input base name
        internal static string GetUniqueConnectionStringName(ConfigFileUtils configFileUtils, string baseConnectionStringName)
        {
            var connectionStringNames = GetExistingConnectionStrings(configFileUtils).Keys;

            var i = 1;
            var uniqueConnectionStringName = baseConnectionStringName;
            while (connectionStringNames.Contains(uniqueConnectionStringName))
            {
                uniqueConnectionStringName = baseConnectionStringName + i++;
            }

            return uniqueConnectionStringName;
        }

        /// <summary>
        /// Determines whether a connection string is already stored for the given entity container name.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name to look for.</param>
        /// <returns>
        /// True when a connection string exists under that name; otherwise false.
        /// </returns>
        internal bool HasConnectionString(Project project, string entityContainerName)
        {
            if (project is null
                || String.IsNullOrEmpty(entityContainerName))
            {
                return false;
            }

            if (!ConnStringsByProjectHash.ContainsKey(project))
            {
                return false;
            }
            return ConnStringsByProjectHash[project].ContainsKey(entityContainerName);
        }

        /// <summary>
        /// Determines whether the connection string described by an &lt;add&gt; element is already stored, both by name
        /// and by value.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="node">The &lt;connectionStrings&gt;/&lt;add&gt; element to compare against the hash.</param>
        /// <returns>
        /// True when an entry with the same name and an equivalent connection string exists; otherwise false.
        /// </returns>
        /// <remarks>
        /// Both the name and the value must match so a hand-edited .config file, where the name survived but the value
        /// changed, is correctly reported as out of sync.
        /// </remarks>
        internal bool HasConnectionString(Project project, XmlNode node)
        {
            if (project is null
                || node is null)
            {
                return false;
            }

            if (!ConnStringsByProjectHash.ContainsKey(project))
            {
                return false;
            }
            var connectionStringAttr = node.Attributes.GetNamedItem(XmlAttrNameConnectionString);
            var connectionNameAttr = node.Attributes.GetNamedItem(XmlAttrNameName);
            if (connectionStringAttr is not null
                && connectionNameAttr is not null)
            {
                ConnectionString connStringObj = new ConnectionString(connectionStringAttr.Value);
                return (ConnStringsByProjectHash[project].ContainsKey(connectionNameAttr.Value)
                        && ConnStringsByProjectHash[project][connectionNameAttr.Value].Equals(connStringObj));
            }
            return false;
        }

        /// <summary>
        /// Injects the MARS/AppFramework attributes into the provider connection string
        /// without pinging the connection to see if the database supports SQL 90 or newer. This
        /// does not require a design-time connection.
        /// </summary>
        /// <param name="sourceConnectionString">The provider connection string to augment.</param>
        /// <param name="providerInvariantName">The ADO.NET provider invariant name of the underlying store.</param>
        /// <returns>
        /// The augmented connection string, or the original string when the provider is not SQL Server or the string
        /// cannot be parsed.
        /// </returns>
        /// <remarks>
        /// Existing keywords are never overwritten: an unparseable string is returned untouched, and the App keyword is
        /// skipped when the caller already supplied App or Application Name.
        /// </remarks>
        internal static string InjectEFAttributesIntoConnectionString(string sourceConnectionString, string providerInvariantName)
        {
            // if the provider connection string's provider property is "System.Data.SqlClient" or
            // "Microsoft.Data.SqlClient" then add the MARS attribute (value is true if SQL Server
            // version >= 9, false otherwise). Also add the App attribute (with fixed value
            // EntityFramework) - which is useful for statistics on server.
            if (!ProviderNames.IsSqlServerProvider(providerInvariantName))
            {
                return sourceConnectionString;
            }

            DbConnectionStringBuilder configFileConnectionBuilder = new DbConnectionStringBuilder();

            try
            {
                configFileConnectionBuilder.ConnectionString = sourceConnectionString;
            }
            catch (ArgumentException)
            {
                return sourceConnectionString;
            }

            // add MARS property if it does not already exist
            if (!configFileConnectionBuilder.TryGetValue(XmlAttrNameMultipleActiveResultSets, out object marsValue))
            {
                configFileConnectionBuilder[XmlAttrNameMultipleActiveResultSets] = true.ToString();
            }

            // add App attribute if neither App nor Application Name property is already set
            if (!configFileConnectionBuilder.ContainsKey(ProviderConnectionStringPropertyNameApp)
                && !configFileConnectionBuilder.ContainsKey(ProviderConnectionStringPropertyNameApplicationName))
            {
                // note: fixed value so no localization;
                configFileConnectionBuilder[ProviderConnectionStringPropertyNameApp] = "EntityFramework";
            }

            return configFileConnectionBuilder.ConnectionString;
        }

        /// <summary>
        /// After renaming a file, we need to update the metadata portion of the connection string
        /// to reflect the new name of the edmx file.
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the renamed file and the artifact it maps to.</param>
        /// <returns>
        /// An HRESULT: S_OK when handled, E_NOTIMPL for non-.edmx files, and E_INVALIDARG when the project or artifact
        /// could not be resolved.
        /// </returns>
        internal int OnAfterRenameFile(object sender, ModelChangeEventArgs args)
        {
            // ignore files that are not edmx
            if (!Path.GetExtension(args.OldFileName).Equals(EntityDesignArtifact.ExtensionEdmx, StringComparison.CurrentCulture))
            {
                return VSConstants.E_NOTIMPL;
            }

            if (args.ProjectObj is null)
            {
                Debug.Fail(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.ConnectionManager_UpdateError, "Metadata portion of connection string",
                        "No project was found"));
                return VSConstants.E_INVALIDARG;
            }

            // if we are renaming the extension to a non-edmx extension, then the artifact will be null
            if (args.Artifact is null)
            {
                if (Path.GetExtension(args.NewFileName).Equals(EntityDesignArtifact.ExtensionEdmx, StringComparison.CurrentCulture))
                {
                    Debug.Fail("we are renaming the file to one with an edmx extension, why weren't we able to find the artifact?");
                }
                return VSConstants.E_INVALIDARG;
            }

            if (args.Artifact.ConceptualModel() is not null
                && args.Artifact.ConceptualModel().FirstEntityContainer is not null
                && HasConnectionString(args.ProjectObj, args.Artifact.ConceptualModel().FirstEntityContainer.LocalName.Value))
            {
                var metadataFileNames = GetMetadataFileNamesFromArtifactFileName(args.ProjectObj, args.Artifact.Uri.LocalPath, PackageManager.Package);
                var mapProperty = GetMetadataPropertyFromArtifact(args.Artifact);
                string mapPropertyValue;
                if (mapProperty is not null)
                {
                    mapPropertyValue = mapProperty.ValueAttr.Value;
                }
                else
                {
                    mapPropertyValue = ConnectionDesignerInfo.MAP_CopyToOutputDirectory;
                }

                var applicationType = VsUtils.GetApplicationType(PackageManager.Package, args.ProjectObj);
                var newMetaData = GetConnectionStringMetadata(metadataFileNames, args.ProjectObj, applicationType, mapPropertyValue);

                UpdateMetadataName(args.ProjectObj, args.Artifact.ConceptualModel().FirstEntityContainer.LocalName.Value, newMetaData);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Translate a connection string from design-time to runtime
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the connection string converter.</param>
        /// <param name="project">The DTE project the connection string belongs to.</param>
        /// <param name="runtimeInvariantName">The runtime provider invariant name.</param>
        /// <param name="designTimeConnectionString">The design-time connection string to translate.</param>
        /// <returns>
        /// The runtime form of the connection string.
        /// </returns>
        internal static string TranslateConnectionStringFromDesignTime(IServiceProvider serviceProvider, Project project, string runtimeInvariantName, string designTimeConnectionString)
        {
            return TranslateConnectionString(serviceProvider, project, runtimeInvariantName, designTimeConnectionString, fromDesignTime: true);
        }

        /// <summary>
        /// Translate a connection string from runtime to design-time
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the connection string converter.</param>
        /// <param name="project">The DTE project the connection string belongs to.</param>
        /// <param name="designTimeInvariantName">The design-time provider invariant name.</param>
        /// <param name="runtimeConnectionString">The runtime connection string to translate.</param>
        /// <returns>
        /// The design-time form of the connection string.
        /// </returns>
        internal static string TranslateConnectionStringFromRunTime(IServiceProvider serviceProvider, Project project, string designTimeInvariantName, string runtimeConnectionString)
        {
            return TranslateConnectionString(serviceProvider, project, designTimeInvariantName, runtimeConnectionString, fromDesignTime: false);
        }

        /// <summary>
        /// Translate an invariant name from design-time to runtime
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the provider mapper.</param>
        /// <param name="invariantName">The design-time provider invariant name.</param>
        /// <param name="connectionString">The connection string the provider is used with.</param>
        /// <returns>
        /// The runtime invariant name, or the supplied name when no mapping exists.
        /// </returns>
        internal static string TranslateInvariantNameFromDesignTime(IServiceProvider serviceProvider, string invariantName, string connectionString)
        {
            return TranslateInvariantName(serviceProvider, invariantName, connectionString, fromDesignTime: true);
        }

        /// <summary>
        /// Translate an invariant name from runtime to design-time
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the provider mapper.</param>
        /// <param name="invariantName">The runtime provider invariant name.</param>
        /// <param name="connectionString">The connection string the provider is used with.</param>
        /// <returns>
        /// The design-time invariant name, or the supplied name when no mapping exists.
        /// </returns>
        internal static string TranslateInvariantNameFromRunTime(IServiceProvider serviceProvider, string invariantName, string connectionString)
        {
            return TranslateInvariantName(serviceProvider, invariantName, connectionString, fromDesignTime: false);
        }

        /// <summary>
        /// Replaces the connection string stored under an entity container name, then rewrites the .config file.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name the connection string is keyed by.</param>
        /// <param name="newConnectionString">The replacement EntityClient connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="project" /> is null.</exception>
        /// <remarks>
        /// A connection string that fails to parse is not fatal: an empty builder is stored so the invalid text is
        /// dropped rather than propagated into the .config file.
        /// </remarks>
        internal void UpdateConnectionString(Project project, string entityContainerName, string newConnectionString)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return;
            }

            lock (_hashSyncRoot)
            {
                ExtractConnStringsIntoHash(project, true);

                // there definitely has to be a connection string keyed by the old entity container name.
                if (ConnStringsByProjectHash.ContainsKey(project)
                    && ConnStringsByProjectHash[project].TryGetValue(entityContainerName, out ConnectionString existingConnectionString))
                {
                    EntityConnectionStringBuilder ecsb = new EntityConnectionStringBuilder();
                    try
                    {
                        ecsb = new EntityConnectionStringBuilder(newConnectionString);
                    }
                    catch (ArgumentException)
                    {
                        Debug.WriteLine("Encountered argument exception while parsing the entity connection string");
                    }

                    ConnStringsByProjectHash[project][entityContainerName].Builder = ecsb;

                    InsertConnStringsFromHash(project);
                }
            }
        }

        /// <summary>
        /// Replaces every EntityClient connection string in a .config document with the contents of the supplied hash.
        /// </summary>
        /// <param name="configXmlDoc">The .config document to rewrite.</param>
        /// <param name="entityConnectionStrings">The connection strings to write, keyed by entity container name.</param>
        /// <exception cref="XmlException">
        /// Thrown when the document has no usable &lt;connectionStrings&gt; section, meaning the .config file is corrupt.
        /// </exception>
        /// <remarks>
        /// Whitespace siblings are removed alongside each deleted entry so repeated rewrites do not accumulate blank
        /// lines in the .config file. Only EntityClient entries are touched, leaving other providers untouched.
        /// </remarks>
        // internal for testing
        internal static void UpdateEntityConnectionStringsInConfig(XmlDocument configXmlDoc, Dictionary<string, ConnectionString> entityConnectionStrings)
        {
            Debug.Assert(configXmlDoc is not null, "configXmlDoc is null");
            Debug.Assert(entityConnectionStrings is not null, "entityConnectionStrings is null");

            // delete all previous System.Data.Entity connection strings that are in the hash
            var xmlNodeList = configXmlDoc.SelectNodes(XpathConnectionStringsAddEntity);
            foreach (XmlNode node in xmlNodeList)
            {
                var prevSibling = node.PreviousSibling;
                var nextSibling = node.NextSibling;
                node.ParentNode.RemoveChild(node);
                if (prevSibling is not null
                    && prevSibling.NodeType == XmlNodeType.Whitespace)
                {
                    prevSibling.ParentNode.RemoveChild(prevSibling);
                }
                if (nextSibling is not null
                    && nextSibling.NodeType == XmlNodeType.Whitespace)
                {
                    nextSibling.ParentNode.RemoveChild(nextSibling);
                }
            }

            var connStringsElement = GetConnectionStringsElement(configXmlDoc);
            if (connStringsElement is null)
            {
                throw new XmlException(Resources.ConnectionManager_CorruptConfig);
            }

            foreach (var nameToConnString in entityConnectionStrings)
            {
                AddConnectionStringElement(connStringsElement, nameToConnString.Key, nameToConnString.Value.Text, Provider);
            }
        }

        /// <summary>
        /// Change the entity container name in the hash, then rewrite the .config file.
        /// </summary>
        /// <param name="project">DTE Project that owns the .config file</param>
        /// <param name="oldName">The entity container name the connection string is currently keyed by.</param>
        /// <param name="newName">The entity container name to key the connection string by from now on.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="project" />, <paramref name="oldName" /> or <paramref name="newName" /> is null or empty.
        /// </exception>
        /// <remarks>
        /// Any pre-existing entry under the new name is removed first, because a hand-written .config file may already
        /// contain an unused connection string that happens to collide with the new name.
        /// </remarks>
        internal void UpdateEntityContainerName(Project project, string oldName, string newName)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return;
            }

            if (String.IsNullOrEmpty(oldName))
            {
                throw new ArgumentNullException("oldName");
            }

            if (String.IsNullOrEmpty(newName))
            {
                throw new ArgumentNullException("newName");
            }

            lock (_hashSyncRoot)
            {
                ExtractConnStringsIntoHash(project, true);

                // there definitely has to be a connection string keyed by the old entity container name.
                if (ConnStringsByProjectHash.ContainsKey(project)
                    && ConnStringsByProjectHash[project].TryGetValue(oldName, out ConnectionString tempString))
                {
                    ConnStringsByProjectHash[project].Remove(oldName);
                    // if the user opens up a .config with connection strings that aren't being used, one of them
                    // could contain the new entity container name, so we make sure we remove that before adding.
                    if (ConnStringsByProjectHash[project].ContainsKey(newName))
                    {
                        ConnStringsByProjectHash[project].Remove(newName);
                    }
                    ConnStringsByProjectHash[project].Add(newName, tempString);

                    InsertConnStringsFromHash(project);
                }
            }
        }

        /// <summary>
        /// Given an old metadata name and a new one, find the connection string keyed by the old metadata name in the hash,
        /// update its metadata name, then rewrite the .config file.
        /// </summary>
        /// <param name="project">DTE Project that owns the .config file</param>
        /// <param name="entityContainerName">The entity container name the connection string is keyed by.</param>
        /// <param name="newMetadata">The replacement metadata portion of the connection string.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="project" />, <paramref name="entityContainerName" /> or
        /// <paramref name="newMetadata" /> is null or empty.
        /// </exception>
        /// <remarks>
        /// A missing connection string is reported to the output window instead of throwing, because the user may
        /// legitimately have deleted the entry from the .config file by hand.
        /// </remarks>
        internal void UpdateMetadataName(Project project, string entityContainerName, string newMetadata)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return;
            }

            if (String.IsNullOrEmpty(entityContainerName))
            {
                throw new ArgumentNullException("entityContainerName");
            }

            if (String.IsNullOrEmpty(newMetadata))
            {
                throw new ArgumentNullException("newMetadata");
            }

            lock (_hashSyncRoot)
            {
                ExtractConnStringsIntoHash(project, true);
                var localConnStringToChange = GetConnectionStringObject(project, entityContainerName);

                if (localConnStringToChange is not null)
                {
                    localConnStringToChange.Builder.Metadata = newMetadata;
                    InsertConnStringsFromHash(project);
                }
                else
                {
                    var s = String.Format(CultureInfo.CurrentCulture, Resources.ConnectionManager_NoConnectionString, entityContainerName);
                    VsUtils.LogOutputWindowPaneMessage(project, s);
                }
            }
        }

        /// <summary>
        /// Stores a connection string under an entity container name, replacing any existing entry.
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name to key the connection string by.</param>
        /// <param name="entityConnectionString">The EntityClient connection string to store.</param>
        internal void UpdateOrAddConnectionString(Project project, string entityContainerName, string entityConnectionString)
        {
            if (HasConnectionString(project, entityContainerName))
            {
                UpdateConnectionString(project, entityContainerName, entityConnectionString);
            }
            else
            {
                ConnectionString connectionStringObj = new ConnectionString(entityConnectionString);
                AddConnectionString(project, entityContainerName, connectionStringObj);
            }
        }

#if (VS11)
        /// <summary>
        /// Rewrites any SQL Server Compact 3.5 provider references in a .config document to use 4.0 instead.
        /// </summary>
        /// <param name="configXmlDoc">The .config document to inspect and update in place.</param>
        /// <returns>
        /// True when at least one connection string was changed; otherwise false.
        /// </returns>
        // Update the .config file if the nodes have a SQL CE 3.5 provider to use 4.0 instead
        internal static bool UpdateSqlCeProviderInConnectionStrings(XmlDocument configXmlDoc)
        {
            Debug.Assert(configXmlDoc is not null, "configXml is null");

            var docUpdated = false;
            // update all nodes that have SQL CE 3.5 provider
            var xmlNodeList = configXmlDoc.SelectNodes(XpathConnectionStringsAdd);
            foreach (XmlNode node in xmlNodeList)
            {
                var e = node as XmlElement;
                if (e is not null)
                {
                    var connectionString = e.GetAttribute(XmlAttrNameConnectionString);
                    if (connectionString is not null
                        && connectionString.Contains(SqlCe35ConnectionStringProvider))
                    {
                        var newConnString = connectionString.Replace(SqlCe35ConnectionStringProvider, SqlCe40ConnectionStringProvider);
                        e.SetAttribute(XmlAttrNameConnectionString, newConnString);
                        docUpdated = true;
                    }
                }
            }

            return docUpdated;
        }
#endif

        /// <summary>
        /// Rewrites any legacy SQL Express based SQL Database File data sources in a .config document so they target
        /// LocalDB instead.
        /// </summary>
        /// <param name="configXmlDoc">The .config document to inspect and update in place.</param>
        /// <returns>
        /// True when at least one connection string was changed; otherwise false.
        /// </returns>
        /// <remarks>
        /// Only connection strings containing AttachDbFileName are considered, since a SQL Database File connection is
        /// the only case where the SQL Express data source can be safely swapped for LocalDB. The User Instance keyword
        /// is dropped at the same time because LocalDB does not support it.
        /// </remarks>
        // Update the .config file if the nodes have an old style SQL Database File Data Source to have the new style instead
        internal static bool UpdateSqlDatabaseFileDataSourceInConnectionStrings(XmlDocument configXmlDoc)
        {
            Debug.Assert(configXmlDoc is not null, "configXml is null");

            var docUpdated = false;

            // update SQL Database File connections
            var xmlNodeList = configXmlDoc.SelectNodes(XpathConnectionStringsAdd);
            foreach (XmlNode node in xmlNodeList)
            {
                if (node is XmlElement e)
                {
                    var connectionString = e.GetAttribute(XmlAttrNameConnectionString);
                    if (connectionString is not null)
                    {
                        // a SQL Database File connection must contain AttachDbFileName
                        var offset = connectionString.IndexOf(
                            LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME, StringComparison.OrdinalIgnoreCase);
                        if (offset > -1)
                        {
                            var connStringUpdated = false;

                            // check whether connection string contains "Data Source=.\SQLEXPRESS" (case-insensitive)
                            // if so replace with LocalDB Data Source
                            offset = connectionString.IndexOf(
                                PreUpgradeSqlDatabaseFileConnectionStringDataSource, StringComparison.OrdinalIgnoreCase);
                            if (offset > -1)
                            {
                                connectionString = connectionString.Substring(0, offset) +
                                                    PostUpgradeSqlDatabaseFileConnectionStringDataSource +
                                                    connectionString.Substring(
                                                        offset + PreUpgradeSqlDatabaseFileConnectionStringDataSourceLength);
                                connStringUpdated = true;
                            }

                            // check whether connection string contains "User Instance=True" (case-insensitive)
                            // if so remove
                            offset = connectionString.IndexOf(
                                SqlDatabaseFileConnectionStringUserInstance, StringComparison.OrdinalIgnoreCase);
                            if (offset > -1)
                            {
                                // if User Instance=True was followed by a semi-colon then remove that too
                                var afterUserInstance =
                                    connectionString.Substring(offset + SqlDatabaseFileConnectionStringUserInstanceLength);
                                if (afterUserInstance.StartsWith(";", StringComparison.Ordinal))
                                {
                                    afterUserInstance = afterUserInstance.Substring(1);
                                }
                                connectionString = connectionString.Substring(0, offset) + afterUserInstance;
                                connStringUpdated = true;
                            }

                            // update XML document
                            if (connStringUpdated)
                            {
                                e.SetAttribute(XmlAttrNameConnectionString, connectionString);
                                docUpdated = true;
                            }
                        }
                    }
                }
            }

            return docUpdated;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Add a connection string object to the hash and push the updates directly to the .config file
        /// </summary>
        /// <param name="project">The DTE project that owns the .config file.</param>
        /// <param name="entityContainerName">The entity container name to key the connection string by.</param>
        /// <param name="connStringObj">The connection string to store.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="project" /> is null.</exception>
        /// <remarks>
        /// The hash is re-extracted from disk first so a connection string the user deleted by hand is not resurrected,
        /// and any existing entry under the same name is removed before adding (bug 556587).
        /// </remarks>
        private void AddConnectionString(Project project, string entityContainerName, ConnectionString connStringObj)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            lock (_hashSyncRoot)
            {
                ExtractConnStringsIntoHash(project, true);
                if (!ConnStringsByProjectHash.ContainsKey(project))
                {
                    ConnStringsByProjectHash[project] = [];
                }

                // bug 556587: we need to delete the connection string from the hash if it is stale
                if (ConnStringsByProjectHash[project].ContainsKey(entityContainerName))
                {
                    ConnStringsByProjectHash[project].Remove(entityContainerName);
                }
                ConnStringsByProjectHash[project].Add(entityContainerName, connStringObj);
                InsertConnStringsFromHash(project);
            }
        }

        /// <summary>
        /// Appends a new &lt;add&gt; element describing a connection string to an existing &lt;connectionStrings&gt; element.
        /// </summary>
        /// <param name="connStringsElement">The &lt;connectionStrings&gt; element to append to.</param>
        /// <param name="connStringName">The name to give the new connection string entry.</param>
        /// <param name="connString">The connection string value.</param>
        /// <param name="providerName">The ADO.NET provider invariant name to record on the entry.</param>
        private static void AddConnectionStringElement(XmlNode connStringsElement, string connStringName, string connString, string providerName)
        {
            var addNode = connStringsElement.OwnerDocument.CreateElement("add");

            addNode.SetAttribute(XmlAttrNameName, connStringName);
            addNode.SetAttribute(XmlAttrNameConnectionString, connString);
            addNode.SetAttribute(XmlAttrNameProviderName, providerName);

            connStringsElement.AppendChild(addNode);
        }

        /// <summary>
        /// Determines whether a DDEX provider is registered for the given invariant name.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the DDEX provider manager.</param>
        /// <param name="invariantName">The ADO.NET provider invariant name to look for.</param>
        /// <returns>
        /// True when a DDEX provider with that invariant name is installed; otherwise false.
        /// </returns>
        /// <remarks>
        /// Used to turn an opaque connection string translation failure into a message that names the missing provider.
        /// </remarks>
        private static bool DDEXProviderInstalled(IServiceProvider serviceProvider, string invariantName)
        {
            Debug.Assert(serviceProvider is not null, "serviceProvider must not be null");
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "Invalid invariant name");

            IVsDataProviderManager dataProviderManager = (IVsDataProviderManager)serviceProvider.GetService(typeof(IVsDataProviderManager));
            Debug.Assert(dataProviderManager is not null, "Could not find IVsDataProviderManager");

            return
                dataProviderManager.Providers.Values.Any(
                    p => invariantName.Equals((string)p.GetProperty("InvariantName"), StringComparison.Ordinal));
        }

        /// <summary>
        /// Releases the resources held by this instance.
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose()" />; false when called from the finalizer.</param>
        /// <remarks>
        /// The event subscriptions are only released on an explicit dispose; reaching this method from the finalizer means
        /// the manager outlived its owner without being torn down, which the assert surfaces during development.
        /// </remarks>
        private void Dispose(bool disposing)
        {
            Debug.Assert(disposing, "Connection Manager is finalized before disposing");
            if (disposing)
            {
                UnregisterModelListenerEvents();
                _connStringsByProjectHash = null;
            }
        }

        /// <summary>
        /// Helper function to construct the metadata, depending on what type of application and output path
        /// </summary>
        /// <param name="metadataFiles">The CSDL/SSDL/MSL artifact paths, which may be empty or null.</param>
        /// <param name="project">The DTE project the connection string belongs to.</param>
        /// <param name="applicationType">The project system, which determines how metadata paths are expressed.</param>
        /// <param name="metadataProcessingType">Whether artifacts are embedded in the assembly or copied to the output directory.</param>
        /// <returns>
        /// The metadata portion of an EntityClient connection string.
        /// </returns>
        /// <remarks>
        /// Path shape varies per project system: web sites can only load embedded resources, web applications need virtual
        /// paths rooted at the output directory, and everything else uses paths relative to the assembly.
        /// </remarks>
        private static string GetConnectionStringMetadata(
            IEnumerable<string> metadataFiles, Project project, VisualStudioProjectSystem applicationType, string metadataProcessingType)
        {
            var outputPath = GetOutputPath(project, applicationType);

            // fix up outputPath
            if (outputPath is null)
            {
                outputPath = String.Empty;
            }
            else if (!outputPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += "\\";
            }
            outputPath = outputPath.Replace("\\", "/");

            // construct metadata portion of connection string
            if (metadataFiles is null
                || !metadataFiles.Any())
            {
                if (metadataProcessingType == ConnectionDesignerInfo.MAP_EmbedInOutputAssembly
                    || applicationType == VisualStudioProjectSystem.Website)
                {
                    return EmbedAsResourcePrefix;
                }
                if (applicationType == VisualStudioProjectSystem.WebApplication)
                {
                    // web-app's need to have the outputPath (usually "bin") appended
                    return "~/" + outputPath;
                }
                else
                {
                    return ".";
                }
            }
            else
            {
                StringBuilder md = new StringBuilder();
                var i = 0;
                var metadataFileCount = metadataFiles.Count();
                foreach (var f in metadataFiles)
                {
                    // if this is a web app, then change relative path to virtual path
                    if (applicationType == VisualStudioProjectSystem.WebApplication
                        && metadataProcessingType == ConnectionDesignerInfo.MAP_CopyToOutputDirectory)
                    {
                        md.Append(f.Replace(".\\", "~/" + outputPath));
                    }
                    else if (applicationType == VisualStudioProjectSystem.Website)
                    {
                        md.Append(EmbedAsResourcePrefix);
                        md.Append("/");

                        if (f[0] == '.'
                            && f[1] == Path.DirectorySeparatorChar)
                        {
                            var folderAndFile = f.Substring(2);
                            md.Append(folderAndFile.Replace(Path.DirectorySeparatorChar, '.'));
                        }
                        else
                        {
                            Debug.Fail("Unexpected start characters in metadata file");
                            return EmbedAsResourcePrefix;
                        }
                    }
                    else
                    {
                        if (metadataProcessingType == ConnectionDesignerInfo.MAP_EmbedInOutputAssembly)
                        {
                            md.Append(EmbedAsResourcePrefix);
                            md.Append("/");
                            md.Append(f.Replace(Path.DirectorySeparatorChar, '.').TrimStart('.'));
                        }
                        else if (metadataProcessingType == ConnectionDesignerInfo.MAP_CopyToOutputDirectory)
                        {
                            md.Append(f);
                        }
                    }

                    if (i++ < metadataFileCount - 1)
                    {
                        // Character used by framework to separate paths to artifacts in the Entity Connection String
                        md.Append("|");
                    }
                }

                return md.ToString();
            }
        }

        /// <summary>
        /// Gets the &lt;connectionStrings&gt; element of a .config document, creating it when it is absent.
        /// </summary>
        /// <param name="configXml">The .config document to inspect.</param>
        /// <returns>
        /// The &lt;connectionStrings&gt; element, or null when the document is not a recognizable .config file.
        /// </returns>
        /// <remarks>
        /// A namespaced or non-&lt;configuration&gt; document element means this is not a .NET configuration file, so null
        /// is returned rather than blindly appending a section to an unrelated XML document.
        /// </remarks>
        private static XmlElement GetConnectionStringsElement(XmlDocument configXml)
        {
            if (!"configuration".Equals(configXml.DocumentElement.Name, StringComparison.Ordinal)
                || !string.IsNullOrEmpty(configXml.DocumentElement.NamespaceURI))
            {
                return null;
            }

            XmlElement connStringsElement = (XmlElement)configXml.DocumentElement.SelectSingleNode("connectionStrings");
            if (connStringsElement is null)
            {
                connStringsElement = configXml.CreateElement("connectionStrings");
                configXml.DocumentElement.AppendChild(connStringsElement);
            }

            return connStringsElement;
        }

        /// <summary>
        /// Gets the build output path of a project when that path affects how metadata paths are written.
        /// </summary>
        /// <param name="project">The DTE project to query.</param>
        /// <param name="applicationType">The project system.</param>
        /// <returns>
        /// The active configuration's output path for web applications; otherwise an empty string.
        /// </returns>
        /// <remarks>
        /// Only web applications need the output path, because their metadata references are virtual paths rooted at the
        /// deployed bin directory.
        /// </remarks>
        private static string GetOutputPath(Project project, VisualStudioProjectSystem applicationType)
        {
            return (VisualStudioProjectSystem.WebApplication == applicationType)
                ? project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value as string
                : string.Empty;
        }

        /// <summary>
        /// Builds the project-to-connection-string hash from every project in the solution, if it has not been built yet.
        /// </summary>
        /// <remarks>
        /// The manager only starts listening for project events once the package has loaded, so any projects opened before
        /// that point have to be swept up here. Non-critical failures are logged rather than thrown so the designer stays
        /// usable when a single project cannot be parsed.
        /// </remarks>
        private void InitializeConnectionStringsHash()
        {
            if (_connStringsByProjectHash is null)
            {
                lock (_hashSyncRoot)
                {
                    try
                    {
                        // since we started listening only after the package loaded, parse the first project's config.
                        _connStringsByProjectHash = [];

                        // we might have opened up a solution with multiple projects, so iterate through them, building
                        // our dictionary
                        foreach (var eachProject in VsUtils.GetAllProjectsInSolution(PackageManager.Package))
                        {
                            ExtractConnStringsIntoHash(eachProject, false);
                        }
                    }
                    catch (Exception e)
                    {
                        var s = Resources.ConnectionManager_InitializeError;
                        s = String.Format(CultureInfo.CurrentCulture, s, e.Message);
                        Project project = null;
                        foreach (var p in VsUtils.GetAllProjectsInSolution(PackageManager.Package))
                        {
                            project = p;
                            break;
                        }

                        Debug.Assert(project is not null);
                        if (project is not null)
                        {
                            VsUtils.LogOutputWindowPaneMessage(project, s);
                        }

                        if (CriticalException.IsCriticalException(e))
                        {
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Takes our local hash and destructively updates the .config file.
        /// </summary>
        /// <param name="project">DTE Project that owns the .config file we want to look at.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="project" /> is null.</exception>
        /// <remarks>
        /// An empty hash is deliberately not written back: doing so would strip every EntityClient connection string from
        /// the .config file when the manager simply has nothing cached for the project.
        /// </remarks>
        private void InsertConnStringsFromHash(Project project)
        {
            if (project is null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return;
            }

            InitializeConnectionStringsHash();


            ConfigFileUtils configFileUtils = new ConfigFileUtils(project, PackageManager.Package);
            configFileUtils.GetOrCreateConfigFile();
            var configXmlDoc = configFileUtils.LoadConfig();

            if (!ConnStringsByProjectHash.TryGetValue(project, out Dictionary<string, ConnectionString> hash))
            {
                var s = String.Format(CultureInfo.CurrentCulture, Resources.ConnectionManager_GetConfigError);
                VsUtils.LogOutputWindowPaneMessage(project, s);
                return;
            }

            if (hash.Any())
            {
                UpdateEntityConnectionStringsInConfig(configXmlDoc, hash);
                configFileUtils.SaveConfig(configXmlDoc);
            }
        }

        /// <summary>
        /// If the user changes the entity container name property we just take note of the old entity container name,
        /// storing it safely in the connection manager so when we are ready to commit the change we will know what connection string
        /// to change based on the old name.
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the entity container name change.</param>
        /// <returns>
        /// S_OK.
        /// </returns>
        /// <remarks>
        /// Only the first change is recorded. If the name is edited several times before saving, the original name is the
        /// one that still keys the connection string in the .config file.
        /// </remarks>
        private int OnAfterEntityContainerNameChange(object sender, ModelChangeEventArgs args)
        {
            _staleEntityContainerName ??= args.OldEntityContainerName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// If the user changes the artifact metadata processing value, we store the old value.
        /// We are going to compare the value with the current value to determine whether we need to commit the value.
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the metadata artifact processing change.</param>
        /// <returns>
        /// S_OK.
        /// </returns>
        private int OnAfterMetadataArtifactProcessingChange(object sender, ModelChangeEventArgs args)
        {
            if (String.IsNullOrEmpty(_staleMetadataArtifactProcessing))
            {
                _staleMetadataArtifactProcessing = args.OldMetadataArtifactProcessing;
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// After opening a project, we want to see if there is a .config file, parse it, and
        /// add the connection strings to our hash.
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the opened project.</param>
        /// <returns>
        /// An HRESULT: S_OK when handled, E_INVALIDARG for the miscellaneous files project, and E_FAIL when the .config
        /// file could not be parsed.
        /// </returns>
        /// <remarks>
        /// Project systems that do not implement the interfaces walked along this path throw
        /// <see cref="NotImplementedException" />; those projects simply have no connection strings to track, so the
        /// exception is swallowed.
        /// </remarks>
        private int OnAfterOpenProject(object sender, ModelChangeEventArgs args)
        {
            if (args.ProjectObj is null)
            {
                Debug.Fail(Resources.ConnectionManager_InitializeError);
                return VSConstants.E_FAIL;
            }

            if (args.ProjectObj.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return VSConstants.E_INVALIDARG;
            }

            lock (_hashSyncRoot)
            {
                try
                {
                    ExtractConnStringsIntoHash(args.ProjectObj, false);
                }
                catch (NotImplementedException)
                {
                    // if a project doesn't implement any features we try to access along this code path, then ignore it
                }
                catch (Exception e)
                {
                    VsUtils.LogOutputWindowPaneMessage(args.ProjectObj, e.Message);
                    return VSConstants.E_FAIL;
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// After removing a *.config file, the connection manager should clear the internal connection string hash table
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the removed file and its project.</param>
        /// <returns>
        /// An HRESULT: S_OK when handled, E_INVALIDARG when the project or file name is missing, and E_FAIL when the hash
        /// could not be cleared.
        /// </returns>
        /// <remarks>
        /// The hash is set to null rather than merely emptied so the next access rebuilds it from whatever configuration
        /// files remain on disk.
        /// </remarks>
        private int OnAfterRemoveFile(object sender, ModelChangeEventArgs args)
        {
            if (args.ProjectObj is null)
            {
                Debug.Fail("Could not find the project object to attempt to clear the connection manager's hashtable if necessary");
                return VSConstants.E_INVALIDARG;
            }

            if (String.IsNullOrEmpty(args.OldFileName))
            {
                Debug.Fail(
                    "We are trying to figure out if we should clear the connection manager's hashtable if the file is a *.config, but where is the filename?");
                return VSConstants.E_INVALIDARG;
            }

            if ((VsUtils.GetApplicationType(PackageManager.Package, args.ProjectObj) == VisualStudioProjectSystem.WindowsApplication &&
                 Path.GetFileName(args.OldFileName).Equals(VsUtils.AppConfigFileName, StringComparison.CurrentCultureIgnoreCase))
                || (VsUtils.GetApplicationType(PackageManager.Package, args.ProjectObj) != VisualStudioProjectSystem.WindowsApplication &&
                    Path.GetFileName(args.OldFileName).Equals(VsUtils.WebConfigFileName, StringComparison.CurrentCultureIgnoreCase)))
            {
                lock (_hashSyncRoot)
                {
                    try
                    {
                        _connStringsByProjectHash.Clear();
                        _connStringsByProjectHash = null;
                    }
                    catch (Exception)
                    {
                        return VSConstants.E_FAIL;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// When a user edits the .config file directly, we want to pull those changes into our local hash so
        /// any further changes will be validated against it (if the user edited the entity container name we
        /// wouldn't be able to find it until the user changes it back in the .config file)
        /// </summary>
        /// <param name="sender">The event listener that raised the notification.</param>
        /// <param name="args">Describes the saved document and, for .edmx files, the artifact it maps to.</param>
        /// <returns>
        /// An HRESULT: S_OK when handled, E_INVALIDARG when there is no owning project, and E_FAIL when the .config file
        /// could not be re-parsed.
        /// </returns>
        /// <remarks>
        /// Saving the model is the point at which deferred edits are committed: a pending entity container rename is
        /// applied first so the connection string can be located, and only then is the metadata artifact processing value
        /// compared and written. The property browser is refreshed afterwards because the read-only connection string it
        /// displays is otherwise stale when app.config is saved without focus.
        /// </remarks>
        private int OnAfterSaveFile(object sender, ModelChangeEventArgs args)
        {
            if (args.ProjectObj is null)
            {
                // don't assert here because a solution could get passed in
                return VSConstants.E_INVALIDARG;
            }

            if (args.ProjectObj.UniqueName.Equals(Constants.vsMiscFilesProjectUniqueName, StringComparison.Ordinal))
            {
                return VSConstants.E_INVALIDARG;
            }

            var hr = VSConstants.S_OK;

            // we're only given a cookie into the RDT so we have to query it to get the filename
            var docTable = PackageManager.Package.GetRunningDocumentTable();
            string fileName;
            var docData = IntPtr.Zero;

            try
            {
                hr = docTable.GetDocumentInfo(
                    args.DocCookie, out uint rdtFlags, out uint readLocks, out uint editLocks, out fileName, out IVsHierarchy hierarchy, out uint itemId, out docData);
            }
            finally
            {
                if (docData != IntPtr.Zero)
                {
                    Marshal.Release(docData);
                }
            }

            if (fileName is not null)
            {
                var connStringsUpdated = false;
                // update the local hash table if app.config/web.config was updated manually. This is where we recognize if a user tried to fix up the config.
                if (Path.GetFileName(fileName).Equals(VsUtils.AppConfigFileName, StringComparison.CurrentCultureIgnoreCase)
                    || Path.GetFileName(fileName).Equals(VsUtils.WebConfigFileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    lock (_hashSyncRoot)
                    {
                        try
                        {
                            ExtractConnStringsIntoHash(args.ProjectObj, true);
                            connStringsUpdated = true;
                        }
                        catch (Exception e)
                        {
                            VsUtils.LogOutputWindowPaneMessage(args.ProjectObj, e.Message);
                            hr = VSConstants.E_FAIL;
                        }
                    }
                }
                else if (VSHelpers.GetDocData(PackageManager.Package, fileName) is IEntityDesignDocData)
                {
                    Debug.Assert(args.Artifact is not null, "Artifact must be passed in in order to save the edmx file!");

                    if (args.Artifact is not null
                        && args.Artifact.ConceptualModel() is not null
                        && args.Artifact.ConceptualModel().FirstEntityContainer is not null
                        && args.Artifact.ConceptualModel().FirstEntityContainer.LocalName is not null)
                    {
                        ExtractConnStringsIntoHash(args.ProjectObj, true);
                        connStringsUpdated = true;

                        var entityContainerName = args.Artifact.ConceptualModel().FirstEntityContainer.LocalName.Value;

                        // first check if an entity container name was updated in the model and we haven't saved it
                        if (!String.IsNullOrEmpty(_staleEntityContainerName))
                        {
                            UpdateEntityContainerName(args.ProjectObj, _staleEntityContainerName, entityContainerName);
                            _staleEntityContainerName = null;
                        }

                        // at this point we have taken into account all of the user's actions to fix up the connection between the model and the
                        // config file. Now we update the metadata artifact processing if it was updated
                        if (HasConnectionString(args.ProjectObj, entityContainerName)
                            && !String.IsNullOrEmpty(_staleMetadataArtifactProcessing))
                        {
                            var mapProperty = GetMetadataPropertyFromArtifact(args.Artifact);
                            Debug.Assert(mapProperty is not null, "Metadata artifact processing property in the model cannot be null");
                            if (mapProperty is not null)
                            {
                                var metadataFileNames = GetMetadataFileNamesFromArtifactFileName(
                                    args.ProjectObj, args.Artifact.Uri.LocalPath, PackageManager.Package);

                                var currentMetadataArtifactProcessingValue = mapProperty.ValueAttr.Value;
                                // Compare the new and value of MetadataArtifactProcessingValue, if they are different update the config file.
                                if (String.Compare(
                                    currentMetadataArtifactProcessingValue, _staleMetadataArtifactProcessing,
                                    StringComparison.OrdinalIgnoreCase) != 0)
                                {
                                    var applicationType = VsUtils.GetApplicationType(PackageManager.Package, args.ProjectObj);
                                    var metadata = GetConnectionStringMetadata(metadataFileNames, args.ProjectObj,
                                        applicationType, currentMetadataArtifactProcessingValue);
                                    var connectionString = GetConnectionStringObject(args.ProjectObj, entityContainerName, this);
                                    connectionString.Builder.Metadata = metadata;
                                    InsertConnStringsFromHash(args.ProjectObj);
                                }
                            }
                            _staleMetadataArtifactProcessing = null;
                        }
                    }

                    // refresh the property browser if we have updated the connection strings above. This is for the situation where app.config is saved but the user
                    // does not have focus on it. This way, the read-only connection string in the property browser will be updated immediately.
                    if (connStringsUpdated)
                    {
                        IVsUIShell uiShell = PackageManager.Package.GetService(typeof(IVsUIShell)) as IVsUIShell;
                        uiShell?.RefreshPropertyBrowser(0);
                    }
                }
            }

            return hr;
        }

        /// <summary>
        /// Subscribe to VS events via ModelChangeEventListener's delegates
        /// </summary>
        /// <remarks>
        /// Adding a file is handled by the same handler as opening a project, because in both cases the correct response
        /// is to re-read that project's .config file.
        /// </remarks>
        private void RegisterModelListenerEvents()
        {
            var listener = PackageManager.Package.ModelChangeEventListener;
            if (listener is not null)
            {
                listener.AfterOpenProject += OnAfterOpenProject;
                listener.AfterAddFile += OnAfterOpenProject;
                listener.AfterRemoveFile += OnAfterRemoveFile;
                listener.AfterRenameFile += OnAfterRenameFile;
                listener.AfterEntityContainerNameChange += OnAfterEntityContainerNameChange;
                listener.AfterMetadataArtifactProcessingChange += OnAfterMetadataArtifactProcessingChange;
                listener.AfterSaveFile += OnAfterSaveFile;
            }
        }

        /// <summary>
        /// Translate a connection string from design-time to runtime or vice versa.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the connection string converter.</param>
        /// <param name="project">The DTE project the connection string belongs to.</param>
        /// <param name="invariantName">The provider invariant name matching the direction of translation.</param>
        /// <param name="connectionString">The connection string to translate.</param>
        /// <param name="fromDesignTime">True to translate design-time to runtime; false for the reverse.</param>
        /// <returns>
        /// The translated connection string, or the original when no converter service is available.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the converter rejects the connection string. The message names the missing DDEX provider when that
        /// is the cause.
        /// </exception>
        /// <remarks>
        /// <see cref="ConnectionStringConverterServiceException" /> carries no message of its own, so it is rethrown as an
        /// <see cref="ArgumentException" /> that can actually be shown to the user. The debug-only asserts catch callers
        /// passing a string in the wrong form, which is otherwise silent because the two forms differ only in whitespace.
        /// </remarks>
        private static string TranslateConnectionString(IServiceProvider serviceProvider, Project project, string invariantName, string connectionString, bool fromDesignTime)
        {
            Debug.Assert(serviceProvider is not null, "serviceProvider must not be null");
            Debug.Assert(project is not null, "project must not be null");
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invalid invariantName");

            if (string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            IConnectionStringConverterService converter = (IConnectionStringConverterService)serviceProvider.GetService(typeof(IConnectionStringConverterService));
            if (converter is null)
            {
                return connectionString;
            }
#if DEBUG
            string lowerConnectionString = connectionString.ToLowerInvariant();
            // Runtime has spaces, designtime does not
            if (!fromDesignTime)
            {
                Debug.Assert(!lowerConnectionString.Contains("activedirectoryintegrated")
                    && !lowerConnectionString.Contains("activedirectoryinteractive")
                    && !lowerConnectionString.Contains("activedirectorypassword"), "Passed in a runtime string where a design time string is needed");
            }
            else
            {
                Debug.Assert(!lowerConnectionString.Contains("active directory integrated")
                    && !lowerConnectionString.Contains("active directory interactive")
                    && !lowerConnectionString.Contains("active directory password"), "Passed in a designtime string where a runtime string is needed");
            }
#endif
            try
            {
                return fromDesignTime
                    ? converter.ToRunTime(project, connectionString, invariantName)
                    : converter.ToDesignTime(
                        project, connectionString, TranslateInvariantNameFromRunTime(serviceProvider, invariantName, connectionString));
            }
            catch (ConnectionStringConverterServiceException)
            {
                var ddexNotInstalledMsg =
                    !DDEXProviderInstalled(serviceProvider, invariantName) ?
                    string.Format(CultureInfo.CurrentCulture, Resources.DDEXNotInstalled, invariantName) :
                    string.Empty;

                // ConnectionStringConverterServiceException has no Message - convert to a more descriptive exception
                var errMsg = fromDesignTime
                                 ? string.Format(
                                     CultureInfo.CurrentCulture,
                                     Resources.CannotTranslateDesignTimeConnectionString,
                                     ddexNotInstalledMsg,
                                     connectionString)
                                 : string.Format(
                                     CultureInfo.CurrentCulture,
                                     Resources.CannotTranslateRuntimeConnectionString,
                                     ddexNotInstalledMsg,
                                     connectionString);
                throw new ArgumentException(errMsg);
            }
        }

        /// <summary>
        /// Translate an invariant name from design-time to runtime or vice versa
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the provider mapper.</param>
        /// <param name="invariantName">The provider invariant name matching the direction of translation.</param>
        /// <param name="connectionString">The connection string the provider is used with.</param>
        /// <param name="fromDesignTime">True to translate design-time to runtime; false for the reverse.</param>
        /// <returns>
        /// The translated invariant name, falling back to <paramref name="invariantName" /> when the mapper is absent or
        /// returns nothing.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="connectionString" /> or <paramref name="invariantName" /> is null.
        /// </exception>
        private static string TranslateInvariantName(IServiceProvider serviceProvider, string invariantName, string connectionString, bool fromDesignTime)
        {
            if (connectionString is null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (invariantName is null)
            {
                throw new ArgumentNullException("invariantName");
            }
            var translatedInvariantName = invariantName;

            IDTAdoDotNetProviderMapper providerMapper = serviceProvider.GetService(typeof(IDTAdoDotNetProviderMapper)) as IDTAdoDotNetProviderMapper;

            if (providerMapper is IDTAdoDotNetProviderMapper2 providerMapper2)
            {
                if (fromDesignTime)
                {
                    translatedInvariantName = providerMapper2.MapInvariantToRuntimeInvariantName(invariantName, connectionString, false);
                }
                else
                {
                    translatedInvariantName = providerMapper2.MapRuntimeInvariantToInvariantName(invariantName, connectionString, false);
                }
            }

            if (string.IsNullOrEmpty(translatedInvariantName))
            {
                translatedInvariantName = invariantName;
            }
            return translatedInvariantName;
        }

        /// <summary>
        /// Retrieves the Guid for a particular provider
        /// </summary>
        /// <param name="invariantName">The invariant name for the provider</param>
        /// <param name="connectionString">The connection string being used</param>
        /// <returns>The associated Guid</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="connectionString" /> or <paramref name="invariantName" /> is null.
        /// </exception>
        private static Guid TranslateProviderGuid(string invariantName, string connectionString)
        {
            if (connectionString is null)
            {
                throw new ArgumentNullException("connectionString");
            }
            if (invariantName is null)
            {
                throw new ArgumentNullException("invariantName");
            }

            var providerGuid = Guid.Empty;
            if (PackageManager.Package.GetService(typeof(IDTAdoDotNetProviderMapper)) is IDTAdoDotNetProviderMapper providerMapper)
            {
                providerGuid = providerMapper.MapInvariantNameToGuid(invariantName, connectionString, false /*fEncryptedString*/);
            }

            return providerGuid;
        }

        /// <summary>
        /// Unsubscribe to VS events via ModelChangeEventListener's delegates
        /// </summary>
        private void UnregisterModelListenerEvents()
        {
            var listener = PackageManager.Package.ModelChangeEventListener;
            if (listener is not null)
            {
                listener.AfterOpenProject -= OnAfterOpenProject;
                listener.AfterAddFile -= OnAfterOpenProject;
                listener.AfterRemoveFile -= OnAfterRemoveFile;
                listener.AfterRenameFile -= OnAfterRenameFile;
                listener.AfterEntityContainerNameChange -= OnAfterEntityContainerNameChange;
                listener.AfterMetadataArtifactProcessingChange -= OnAfterMetadataArtifactProcessingChange;
                listener.AfterSaveFile -= OnAfterSaveFile;
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// A wrapper around <see cref="EntityConnectionStringBuilder" /> so we can add our own properties
        /// or different builders at a later point in time.
        /// </summary>
        internal class ConnectionString
        {

            #region Fields

            /// <summary>
            /// The builder that holds the parsed EntityClient connection string this instance wraps.
            /// </summary>
            private EntityConnectionStringBuilder _builder;

            #endregion

            #region Properties

            /// <summary>
            /// Gets or sets the builder that holds the parsed EntityClient connection string.
            /// </summary>
            internal EntityConnectionStringBuilder Builder
            {
                get { return _builder; }
                set { _builder = value; }
            }

            /// <summary>
            /// Gets the design-time form of the underlying provider's invariant name.
            /// </summary>
            /// <remarks>
            /// The .config file stores runtime invariant names, but the designer's data connection UI works in design-time
            /// names, so a translation is required on every read.
            /// </remarks>
            internal string DesignTimeProviderInvariantName
            {
                get { return TranslateInvariantName(PackageManager.Package, _builder.Provider, _builder.ProviderConnectionString, false); }
            }

            /// <summary>
            /// Gets the DDEX provider Guid that corresponds to the underlying provider.
            /// </summary>
            internal Guid Provider
            {
                get { return TranslateProviderGuid(_builder.Provider, _builder.ProviderConnectionString); }
            }

            /// <summary>
            /// Gets the provider connection string nested inside the EntityClient connection string, or an empty string
            /// when no builder is present.
            /// </summary>
            internal string ProviderConnectionStringText
            {
                get { return _builder is not null ? _builder.ProviderConnectionString : String.Empty; }
            }

            /// <summary>
            /// Gets or sets the full EntityClient connection string text.
            /// </summary>
            internal string Text
            {
                get { return _builder?.ConnectionString; }
                set { _builder.ConnectionString = value; }
            }

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ConnectionString" /> class from connection string text.
            /// </summary>
            /// <param name="connStringText">The EntityClient connection string to parse.</param>
            internal ConnectionString(string connStringText)
                : this(new EntityConnectionStringBuilder(connStringText))
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ConnectionString" /> class around an existing builder.
            /// </summary>
            /// <param name="builder">The builder holding the parsed EntityClient connection string.</param>
            internal ConnectionString(EntityConnectionStringBuilder builder)
            {
                _builder = builder;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Determines whether another object is a <see cref="ConnectionString" /> carrying equivalent text.
            /// </summary>
            /// <param name="obj">The object to compare with this instance.</param>
            /// <returns>
            /// True when <paramref name="obj" /> is a <see cref="ConnectionString" /> whose text matches this one,
            /// ignoring case; otherwise false.
            /// </returns>
            /// <remarks>
            /// The comparison is case-insensitive because connection string keywords and values are not case-sensitive,
            /// so two differently cased strings still describe the same connection.
            /// </remarks>
            public override bool Equals(object obj)
            {
                if (obj is not ConnectionString connString)
                {
                    return false;
                }
                return (connString.Text.Equals(Text, StringComparison.CurrentCultureIgnoreCase));
            }

            /// <summary>
            /// Returns a hash code derived from the connection string text.
            /// </summary>
            /// <returns>
            /// The hash code of the connection string text, or the base implementation's hash code when no text is available.
            /// </returns>
            public override int GetHashCode()
            {
                if (_builder is not null
                    && _builder.ConnectionString is not null)
                {
                    return _builder.ConnectionString.GetHashCode();
                }
                return base.GetHashCode();
            }

            #endregion

            #region Internal Methods

            /// <summary>
            /// Gets the design-time form of the nested provider connection string.
            /// </summary>
            /// <param name="project">The DTE project the connection string belongs to.</param>
            /// <returns>
            /// The provider connection string translated into its design-time form.
            /// </returns>
            /// <remarks>
            /// Runtime and design-time connection strings differ, for example in how Active Directory authentication modes
            /// are spelled, so the stored runtime string cannot be handed to design-time UI directly.
            /// </remarks>
            internal string GetDesignTimeProviderConnectionString(Project project)
            {
                return TranslateConnectionStringFromRunTime(
                    PackageManager.Package, project, _builder.Provider, _builder.ProviderConnectionString);
            }

            #endregion

        }

        #endregion

    }

}
