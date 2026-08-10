// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Microsoft.Data.Entity.Design.UI.Util
{
    internal class FileResourceManager
    {
        private static FileResourceManager _instance;
        private readonly string _componentName;

        private FileResourceManager(Assembly resourceAssembly)
        {
            _componentName = resourceAssembly.ToString();
        }

        public static FileResourceManager Instance
        {
            get
            {
                _instance ??= new FileResourceManager(typeof(FileResourceManager).Assembly);
                return _instance;
            }
        }

        public static ResourceDictionary GetResourceDictionary(string name)
        {
            return (ResourceDictionary)Instance.LoadObject(name);
        }

        public static FrameworkElement GetElement(string name)
        {
            return (FrameworkElement)Instance.LoadObject(name);
        }

        private object LoadObject(string name)
        {
            name = name.ToLower(CultureInfo.InvariantCulture);
            name = name.Replace("\\", "/");

            Uri uri = new Uri(_componentName + ";component/" + name, UriKind.RelativeOrAbsolute);

            return Application.LoadComponent(uri);
        }
    }
}
