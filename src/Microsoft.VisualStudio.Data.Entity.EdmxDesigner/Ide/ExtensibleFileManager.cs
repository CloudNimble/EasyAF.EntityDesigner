// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide
{

    /// <summary>
    ///     Finds extensibility files of one kind, in both the per user and the Visual Studio install locations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Entity Framework Tools looks for extensions in two places: a per user directory under
    ///         <see cref="Environment.SpecialFolder.LocalApplicationData" />, and one under the Visual Studio
    ///         install. This walks a named subdirectory of each and returns the files matching an extension, user
    ///         files first so they take precedence.
    ///     </para>
    ///     <para>
    ///         Within each location files come back sorted by name descending, which is what lets a caller apply
    ///         them in a predictable order.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var manager = new ExtensibleFileManager("DBGen", ".tt");
    ///     foreach (var file in manager.AllFiles)
    ///     {
    ///         // user templates first, then those shipped with Visual Studio
    ///     }
    ///     </code>
    /// </example>
    internal class ExtensibleFileManager
    {

        #region Fields

        /// <summary>
        ///     Name of the MSBuild macro standing for the per user extensions directory.
        /// </summary>
        internal static readonly string EFTOOLS_USER_MACRONAME = "UserEFTools";

        /// <summary>
        ///     Name of the MSBuild macro standing for the Visual Studio extensions directory.
        /// </summary>
        internal static readonly string EFTOOLS_VS_MACRONAME = "VSEFTools";

        // TODO: Find a 'more standard' way to obtain the MEF Extensions dirs
        private const string UserExtDirPartFormat = @"Microsoft\{0}\10.0\Extensions\Microsoft\Entity Framework Tools";
        private const string VsExtDirPart = @"Extensions\Microsoft\Entity Framework Tools";

        private static DirectoryInfo _userEFToolsDir;
        private static string _userExtDirPart;
        private static DirectoryInfo _vsEFToolsDir;

        private readonly string _extension;
        private readonly string _subdirectoryName;

        #endregion

        #region Properties

        /// <summary>
        ///     The per user extensions directory. The directory may not exist.
        /// </summary>
        internal static DirectoryInfo UserEFToolsDir
        {
            get
            {
                if (_userEFToolsDir is null)
                {
                    var appDataDirPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var userExtDirPath = Path.Combine(appDataDirPath, UserExtDirPart);
                    _userEFToolsDir = new DirectoryInfo(userExtDirPath);
                }
                return _userEFToolsDir;
            }
        }

        /// <summary>
        ///     The MSBuild macro that expands to the per user extensions directory.
        /// </summary>
        internal static string UserEFToolsMacro
        {
            get { return String.Format(CultureInfo.InvariantCulture, "$({0})", EFTOOLS_USER_MACRONAME); }
        }

        /// <summary>
        ///     The Visual Studio extensions directory. The directory may not exist.
        /// </summary>
        internal static DirectoryInfo VSEFToolsDir
        {
            get
            {
                if (_vsEFToolsDir is null)
                {
                    var vsExtDirPath = Path.Combine(VsUtils.GetVisualStudioInstallDir(), VSExtDirPart);
                    _vsEFToolsDir = new DirectoryInfo(vsExtDirPath);
                }
                return _vsEFToolsDir;
            }
        }

        /// <summary>
        ///     The MSBuild macro that expands to the Visual Studio extensions directory.
        /// </summary>
        internal static string VSEFToolsMacro
        {
            get { return String.Format(CultureInfo.InvariantCulture, "$({0})", EFTOOLS_VS_MACRONAME); }
        }

        /// <summary>
        ///     Every matching file, user files first so they take precedence over those shipped with the product.
        /// </summary>
        internal IEnumerable<FileInfo> AllFiles
        {
            get
            {
                foreach (var fileinfo in UserFiles)
                {
                    yield return fileinfo;
                }
                foreach (var fileInfo in VSFiles)
                {
                    yield return fileInfo;
                }
            }
        }

        /// <summary>
        ///     Matching files from the per user extensions directory.
        /// </summary>
        internal IEnumerable<FileInfo> UserFiles
        {
            get
            {
                foreach (var fileInfo in GetSortedFilesByType(TypeOfFile.User))
                {
                    yield return fileInfo;
                }
            }
        }

        /// <summary>
        ///     Matching files from the Visual Studio extensions directory.
        /// </summary>
        internal IEnumerable<FileInfo> VSFiles
        {
            get
            {
                foreach (var fileInfo in GetSortedFilesByType(TypeOfFile.VS))
                {
                    yield return fileInfo;
                }
            }
        }

        /// <summary>
        ///     The per user portion of the extensions path, which varies with the Visual Studio application ID.
        /// </summary>
        private static string UserExtDirPart
        {
            get
            {
                if (String.IsNullOrEmpty(_userExtDirPart))
                {
                    _userExtDirPart = String.Format(
                        CultureInfo.InvariantCulture, UserExtDirPartFormat, VsUtils.GetVisualStudioApplicationID());
                }
                return _userExtDirPart;
            }
        }

        /// <summary>
        ///     The Visual Studio portion of the extensions path.
        /// </summary>
        private static string VSExtDirPart
        {
            get { return VsExtDirPart; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a manager for one kind of extensibility file.
        /// </summary>
        /// <param name="subdirectoryName">Subdirectory of each extensions directory to look in.</param>
        /// <param name="extension">File extension to match, including the leading dot.</param>
        internal ExtensibleFileManager(string subdirectoryName, string extension)
        {
            _subdirectoryName = subdirectoryName;
            _extension = extension;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Returns the files in <paramref name="allFiles" /> matching <paramref name="extension" />, by name
        ///     descending.
        /// </summary>
        /// <param name="allFiles">Candidate files.</param>
        /// <param name="extension">Extension to match, compared case insensitively.</param>
        /// <returns>The matching files in descending name order.</returns>
        private static IEnumerable<FileInfo> GetSortedFilesByExtension(IEnumerable<FileInfo> allFiles, string extension)
        {
            return from fi in allFiles
                   where Path.GetExtension(fi.FullName).Equals(extension, StringComparison.OrdinalIgnoreCase)
                   orderby Path.GetFileName(fi.FullName) descending
                   select fi;
        }

        /// <summary>
        ///     Returns this manager's matching files from one of the two extensions directories.
        /// </summary>
        /// <param name="typeOfFile">Which extensions directory to read.</param>
        /// <returns>
        ///     The matching files in descending name order, or nothing when the directory does not exist.
        /// </returns>
        private IEnumerable<FileInfo> GetSortedFilesByType(TypeOfFile typeOfFile)
        {
            var dirPath = String.Empty;
            if (typeOfFile == TypeOfFile.User)
            {
                dirPath = Path.Combine(UserEFToolsDir.FullName, _subdirectoryName);
            }
            else if (typeOfFile == TypeOfFile.VS)
            {
                dirPath = Path.Combine(VSEFToolsDir.FullName, _subdirectoryName);
            }

            Debug.Assert(!String.IsNullOrEmpty(dirPath), "We should have determined the dirPath for extensible files");
            if (!String.IsNullOrEmpty(dirPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if (dirInfo.Exists)
                {
                    foreach (var fileInfo in GetSortedFilesByExtension(dirInfo.GetFiles(), _extension))
                    {
                        yield return fileInfo;
                    }
                }
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     Which of the two extensions directories a file came from.
        /// </summary>
        internal enum TypeOfFile
        {
            /// <summary>
            ///     The per user extensions directory.
            /// </summary>
            User,

            /// <summary>
            ///     The Visual Studio install's extensions directory.
            /// </summary>
            VS
        }

        #endregion

    }

}
