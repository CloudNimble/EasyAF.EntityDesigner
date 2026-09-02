// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Views.Explorer
{

    internal class SearchTickAdornerAutomationPeer : FrameworkElementAutomationPeer
    {
        public SearchTickAdornerAutomationPeer(SearchTickAdorner owner)
            : base(owner)
        {
            // do nothing
        }

        protected override string GetClassNameCore()
        {
            return "SearchTickAdorner";
        }

        protected override Point GetClickablePointCore()
        {
            var adornerBounds = GetBoundingRectangleCore();
            return new Point(adornerBounds.Left + adornerBounds.Width / 2, adornerBounds.Top + adornerBounds.Height / 2);
        }

        protected override Rect GetBoundingRectangleCore()
        {
            var baseRect = base.GetBoundingRectangleCore();
            var adornerBounds = ((SearchTickAdorner)Owner).Bounds;
            return new Rect(baseRect.X + adornerBounds.X, baseRect.Y + adornerBounds.Y, adornerBounds.Width, adornerBounds.Height);
        }

        protected override string GetItemStatusCore()
        {
            StringBuilder itemStatus = new StringBuilder();
            SearchTickAdorner searchTickAdorner = (SearchTickAdorner)Owner;

            foreach (var seXsdInfo in searchTickAdorner.ExplorerElements)
            {
                itemStatus.Append("[Name]");
                itemStatus.Append(seXsdInfo.Name);
            }

            return itemStatus.ToString();
        }
    }

}
