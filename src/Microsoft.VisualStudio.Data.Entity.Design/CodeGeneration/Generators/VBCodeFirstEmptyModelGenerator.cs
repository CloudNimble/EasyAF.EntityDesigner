// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Infrastructure;
using System.Globalization;
using WizardResources = Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties.Resources;

namespace Microsoft.Data.Entity.Design.CodeGeneration.Generators
{
    internal class VBCodeFirstEmptyModelGenerator : IContextGenerator
    {
        private const string VBCodeFileTemplate =
@"Imports System
Imports System.Data.Entity
Imports System.Linq

Public Class {0}
    Inherits DbContext

    {1}
    Public Sub New()
        MyBase.New(""name={2}"")
    End Sub

    {3}
    ' Public Overridable Property MyEntities() As DbSet(Of MyEntity)

End Class

'Public Class MyEntity
'    Public Property Id() As Int32
'    Public Property Name() As String
'End Class
";
        public string Generate(DbModel model, string codeNamespace, string contextClassName, string connectionStringName)
        {
            var ctorComment = 
                string.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.CodeFirstCodeFile_CtorComment_VB,
                    contextClassName,
                    codeNamespace);

            return
                string.Format(
                    CultureInfo.CurrentCulture, 
                    VBCodeFileTemplate,  
                    contextClassName,
                    ctorComment,
                    connectionStringName,
                    WizardResources.CodeFirstCodeFile_DbSetComment_VB);
        }
    }
}
