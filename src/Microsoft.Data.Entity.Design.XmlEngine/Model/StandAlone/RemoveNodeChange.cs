// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Records the removal of a node from an XLinq tree.
    /// </summary>
    /// <remarks>
    /// The following sibling is captured before the removal happens so the node can be spliced back into exactly
    /// the same position when the change is undone.
    /// </remarks>
    internal class RemoveNodeChange : SimpleXmlChange, IXmlRemoveNodeChange
    {

        #region Fields

        /// <summary>
        /// The sibling that followed the removed node before it was detached.
        /// </summary>
        internal XObject nextNode;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sibling that followed the removed node before it was detached.
        /// </summary>
        /// <remarks>
        /// A value of <see langword="null" /> means the node was the last child of its parent.
        /// </remarks>
        public XObject NextNode
        {
            get => nextNode;
            set => nextNode = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveNodeChange" /> class.
        /// </summary>
        /// <param name="node">The node that was removed.</param>
        /// <param name="change">The kind of change that was observed.</param>
        public RemoveNodeChange(XObject node, XObjectChange change)
            : base(node, change)
        {
        }

        #endregion

    }

}
