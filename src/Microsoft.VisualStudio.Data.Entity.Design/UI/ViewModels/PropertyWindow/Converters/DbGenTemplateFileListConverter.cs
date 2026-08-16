// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard.Engine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class DbGenTemplateFileListConverter : ExtensibleFileListConverter
    {
        protected override string SubDirPath
        {
            get { return DatabaseGenerationEngine._dbGenFolderName; }
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> standardValues = new List<string>();
            standardValues.AddRange(DatabaseGenerationEngine.TemplateFileManager.VSFiles.Select(fi => MacroizeFilePath(fi.FullName)));
            standardValues.AddRange(DatabaseGenerationEngine.TemplateFileManager.UserFiles.Select(fi => MacroizeFilePath(fi.FullName)));
            return new StandardValuesCollection(standardValues);
        }
    }
}
