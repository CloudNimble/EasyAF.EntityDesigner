// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WizardResources = Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties.Resources;

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    internal class RuntimeConfigViewModel
    {
        private readonly string _helpUrl;
        private readonly string _message;
        private readonly RuntimeConfigState _state;
        private readonly ICollection<EntityFrameworkVersionOption> _entityFrameworkVersions = [];

        public RuntimeConfigViewModel(
            Version targetNetFrameworkVersion,
            Version installedEntityFrameworkVersion,
            bool isModernProviderAvailable,
            bool isCodeFirst)
        {
            // Simplified for modern development - only EF6+ is supported
            if (installedEntityFrameworkVersion != null && installedEntityFrameworkVersion >= RuntimeVersion.Version6)
            {
                if (!isModernProviderAvailable)
                {
                    _entityFrameworkVersions.Add(
                        new EntityFrameworkVersionOption(installedEntityFrameworkVersion)
                            {
                                Disabled = true,
                                IsDefault = true
                            });

                    _state = RuntimeConfigState.Error;
                    _message = WizardResources.RuntimeConfig_SixInstalledButNoProvider;
                    _helpUrl = WizardResources.RuntimeConfig_LearnProvidersUrl;
                }
                else
                {
                    _entityFrameworkVersions.Add(new EntityFrameworkVersionOption(installedEntityFrameworkVersion) { IsDefault = true });
                    _state = RuntimeConfigState.Skip;
                }
            }
            else
            {
                // No EF6 installed yet
                if (isModernProviderAvailable)
                {
                    _entityFrameworkVersions.Add(new EntityFrameworkVersionOption(RuntimeVersion.Latest) { IsDefault = true });

                    if (isCodeFirst)
                    {
                        _state = RuntimeConfigState.Skip;
                    }
                }
                else
                {
                    _state = RuntimeConfigState.Error;
                    _entityFrameworkVersions.Add(new EntityFrameworkVersionOption(RuntimeVersion.Latest)
                    {
                        Disabled = true,
                        IsDefault = true
                    });
                    _message = WizardResources.RuntimeConfig_NoProvider;
                    _helpUrl = WizardResources.RuntimeConfig_LearnProvidersUrl;
                }
            }

            Debug.Assert(_entityFrameworkVersions.Count != 0, "_entityFrameworkVersions is empty.");
            Debug.Assert(_entityFrameworkVersions.Any(v => v.IsDefault), "No element of _entityFrameworkVersions is the default.");
        }

        public RuntimeConfigState State
        {
            get { return _state; }
        }

        public string Message
        {
            get { return _message; }
        }

        public string HelpUrl
        {
            get { return _helpUrl; }
        }

        public IEnumerable<EntityFrameworkVersionOption> EntityFrameworkVersions
        {
            get { return _entityFrameworkVersions; }
        }
    }
}