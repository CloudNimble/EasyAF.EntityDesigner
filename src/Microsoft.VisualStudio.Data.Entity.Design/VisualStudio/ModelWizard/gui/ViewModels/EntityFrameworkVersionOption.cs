// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    internal class EntityFrameworkVersionOption
    {
        public EntityFrameworkVersionOption(Version entityFrameworkVersion, Version targetNetFrameworkVersion = null)
        {
            Debug.Assert(entityFrameworkVersion != null, "entityFrameworkVersion is null.");

            Name = RuntimeVersion.GetName(entityFrameworkVersion, targetNetFrameworkVersion);
            Version = entityFrameworkVersion;
        }

        public string Name { get; set; }
        public Version Version { get; set; }
        public bool Disabled { get; set; }
        public bool IsDefault { get; set; }
    }
}
