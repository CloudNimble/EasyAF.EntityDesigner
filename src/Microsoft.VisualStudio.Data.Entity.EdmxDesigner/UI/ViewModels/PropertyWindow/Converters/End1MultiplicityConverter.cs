// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{

    internal class End1MultiplicityConverter : EndMultiplicityConverter
    {
        // <summary>
        //     Returns the first End of the given Association
        // </summary>
        protected override AssociationEnd GetEnd(Association association)
        {
            if (association.AssociationEnds().Count > 0)
            {
                return association.AssociationEnds()[0];
            }
            return null;
        }
    }

}