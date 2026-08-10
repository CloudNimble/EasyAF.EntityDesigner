// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows;

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    // All assembly level XAML resources are stored here
    internal partial class AssemblyResources : ResourceDictionary
    {
        private static AssemblyResources _default;

        internal AssemblyResources()
        {
            InitializeComponent();
        }

        // Static accessor to the assembly resource dictionary
        internal static AssemblyResources Default
        {
            get
            {
                _default ??= [];
                return _default;
            }
        }
    }
}
