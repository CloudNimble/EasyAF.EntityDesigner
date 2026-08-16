// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.VSXmlDesignerBase.Common;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Design.UI.Views.Dialogs;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.Refactoring
{
    internal class RefactorEFObject
    {
        internal static void RefactorRenameElement(EFObject objectToRefactor, string newName = null, bool showPreview = true)
        {
            if (objectToRefactor != null
                && objectToRefactor.Artifact != null)
            {
                if (objectToRefactor is EFNormalizableItem namedObject)
                {
                    // If the API call did not supply a new name for the object, bring up the dialog for the user to input a name
                    if (newName == null)
                    {
                        RefactorRenameDialog dialog = new RefactorRenameDialog(namedObject);
                        if (dialog.ShowModal() == true)
                        {
                            newName = dialog.NewName;

                            if (dialog.ShowPreview.HasValue)
                            {
                                showPreview = dialog.ShowPreview.Value;
                            }
                        }
                    }

                    if (newName != null)
                    {
                        RefactorRenameElementInDesignerOnly(namedObject, newName, showPreview);
                    }
                }
            }
        }

        private static void RefactorRenameElementInDesignerOnly(EFNormalizableItem namedObject, string newName, bool showPreview)
        {
            Debug.Assert(namedObject != null, "namedObject != null");
            Debug.Assert(newName != null, "namedObject != newName");

            EFRenameContributorInput input = new EFRenameContributorInput(namedObject, newName, namedObject.Name.Value);
            EFRefactoringOperation refactoringOperation = new EFRefactoringOperation(
                namedObject,
                newName,
                input,
                new ServiceProviderHelper(PackageManager.Package.GetService(typeof(IServiceProvider)) as IServiceProvider));

            refactoringOperation.HasPreviewWindow = showPreview;
            refactoringOperation.DoOperation();
        }
    }
}
