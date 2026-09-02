// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows.Automation.Peers;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Views.Explorer
{

    internal class SearchComboBoxAutomationPeer : ComboBoxAutomationPeer
    {
        internal SearchComboBoxAutomationPeer(SearchComboBox owner)
            : base(owner)
        {
            // do nothing 
        }

        protected override void SetFocusCore()
        {
            Owner.Focus();
        }
    }

}
