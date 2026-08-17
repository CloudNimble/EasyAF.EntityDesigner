// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Infrastructure;
using System.Globalization;
using WizardResources = Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Properties.Resources;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Generators
{
    internal class CSharpCodeFirstEmptyModelGenerator : IContextGenerator
    {
        private const string CSharpCodeFileTemplate = 
@"namespace {0}
{{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class {1} : DbContext
    {{
        {2}
        public {1}()
            : base(""name={3}"")
        {{
        }}

        {4}

        // public virtual DbSet<MyEntity> MyEntities {{ get; set; }}
    }}

    //public class MyEntity
    //{{
    //    public int Id {{ get; set; }}
    //    public string Name {{ get; set; }}
    //}}
}}";

        public string Generate(DbModel model, string codeNamespace, string contextClassName, string connectionStringName)
        {
            var ctorComment = 
                string.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.CodeFirstCodeFile_CtorComment_CS,
                    contextClassName,
                    codeNamespace);

            return
                string.Format(
                    CultureInfo.CurrentCulture, 
                    CSharpCodeFileTemplate, 
                    codeNamespace, 
                    contextClassName,
                    ctorComment,
                    connectionStringName,
                    WizardResources.CodeFirstCodeFile_DbSetComment_CS);
        }
    }
}
