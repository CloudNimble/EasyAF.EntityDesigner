// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Util
{

    /// <summary>
    ///     Loads XAML resources compiled into this assembly by their relative path.
    /// </summary>
    /// <remarks>
    ///     Wraps <see cref="Application.LoadComponent(Uri)" />, which needs a pack style URI naming the component
    ///     assembly. The assembly name is captured once so callers can pass an ordinary relative path instead.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var styles = FileResourceManager.GetResourceDictionary(@"UI\Views\Dialogs\DialogStyles.xaml");
    ///     </code>
    /// </example>
    internal class FileResourceManager
    {

        #region Fields

        private static FileResourceManager _instance;
        private readonly string _componentName;

        #endregion

        #region Properties

        /// <summary>
        ///     The shared instance, bound to the assembly this type is defined in.
        /// </summary>
        public static FileResourceManager Instance
        {
            get
            {
                _instance ??= new FileResourceManager(typeof(FileResourceManager).Assembly);
                return _instance;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a manager that loads resources from <paramref name="resourceAssembly" />.
        /// </summary>
        /// <param name="resourceAssembly">The assembly the resources are compiled into.</param>
        private FileResourceManager(Assembly resourceAssembly)
        {
            _componentName = resourceAssembly.ToString();
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Loads a XAML element by its path relative to the assembly root.
        /// </summary>
        /// <param name="name">Relative path of the XAML file, for example <c>UI\Views\Foo.xaml</c>.</param>
        /// <returns>The loaded element.</returns>
        public static FrameworkElement GetElement(string name)
        {
            return (FrameworkElement)Instance.LoadObject(name);
        }

        /// <summary>
        ///     Loads a XAML resource dictionary by its path relative to the assembly root.
        /// </summary>
        /// <param name="name">Relative path of the XAML file, for example <c>UI\Views\Styles.xaml</c>.</param>
        /// <returns>The loaded resource dictionary.</returns>
        public static ResourceDictionary GetResourceDictionary(string name)
        {
            return (ResourceDictionary)Instance.LoadObject(name);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Loads the component at <paramref name="name" /> as a pack URI against this assembly.
        /// </summary>
        /// <param name="name">Relative path of the XAML file.</param>
        /// <returns>The loaded object.</returns>
        /// <remarks>
        ///     The path is lowercased and its separators normalised because pack URIs are case sensitive and use
        ///     forward slashes, while callers naturally write Windows style paths.
        /// </remarks>
        private object LoadObject(string name)
        {
            name = name.ToLower(CultureInfo.InvariantCulture);
            name = name.Replace("\\", "/");

            Uri uri = new Uri(_componentName + ";component/" + name, UriKind.RelativeOrAbsolute);

            return Application.LoadComponent(uri);
        }

        #endregion

    }

}
