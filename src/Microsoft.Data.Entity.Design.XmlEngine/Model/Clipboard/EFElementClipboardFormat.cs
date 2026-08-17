// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Model
{
    [Serializable]
    internal abstract class EFElementClipboardFormat
    {
        internal EFElementClipboardFormat(EFElement efElement)
        {
            NormalizedName = null;
            if (efElement is EFNormalizableItem normalizableItem)
            {
                NormalizedName = normalizableItem.NormalizedName;
            }
        }

        internal Symbol NormalizedName { get; private set; }
    }
}
