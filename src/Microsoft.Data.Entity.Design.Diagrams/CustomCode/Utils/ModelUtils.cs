// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EDMModelHelper = Microsoft.Data.Entity.Design.Edmx.ModelHelper;
using Microsoft.VisualStudio.Modeling;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.Diagrams.CustomSerializer;

namespace Microsoft.Data.Entity.Design.Diagrams.Utils
{
    internal static class ModelUtils
    {
        /// <summary>
        ///     Returns true if the store is serializing, false otherwise
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        internal static bool IsSerializing(Store store)
        {
            var serializing = false;

            if ((store != null)
                && (store.TransactionManager != null)
                && (store.TransactionManager.CurrentTransaction != null))
            {
                serializing = store.TransactionManager.CurrentTransaction.IsSerializing;
            }

            return serializing;
        }

        /// <summary>
        ///     Returns the name of Current Active Tx on that Store, otherwise return null
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        internal static Transaction GetCurrentTx(Store store)
        {
            Transaction tx = null;

            if ((store != null)
                && (store.TransactionManager != null))
            {
                tx = store.TransactionManager.CurrentTransaction;
            }

            return tx;
        }

        internal static bool IsUniqueName(ModelElement elementToCheck, string proposedName, EditingContext context)
        {
            if (string.IsNullOrEmpty(proposedName)
                || elementToCheck == null)
            {
                return false;
            }

            ModelToDesignerModelXRef xref = ModelToDesignerModelXRef.GetModelToDesignerModelXRef(context);
            var modelItem = xref.GetExisting(elementToCheck);
            if (modelItem != null)
            {
                return EDMModelHelper.IsUniqueNameForExistingItem(modelItem, proposedName, true, out string msg);
            }

            return true;
        }
    }
}
