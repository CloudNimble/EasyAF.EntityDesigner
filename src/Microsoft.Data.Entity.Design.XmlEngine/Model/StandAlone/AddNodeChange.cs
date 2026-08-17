// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Records the insertion of a node into an XLinq tree.
    /// </summary>
    /// <remarks>
    /// XLinq does not preserve enough information to reinsert a node at its original position once it has been
    /// detached, so the following sibling is captured here and used as the insertion anchor when the change is redone.
    /// </remarks>
    internal class AddNodeChange : SimpleXmlChange, IXmlAddNodeChange
    {

        #region Fields

        /// <summary>
        /// The sibling that followed the added node at the time of insertion.
        /// </summary>
        internal XObject nextNode;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sibling that followed the added node at the time of insertion.
        /// </summary>
        /// <remarks>
        /// A value of <see langword="null" /> means the node was appended as the last child of its parent.
        /// </remarks>
        public XObject NextNode
        {
            get => nextNode;
            set => nextNode = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNodeChange" /> class.
        /// </summary>
        /// <param name="node">The node that was added.</param>
        /// <param name="change">The kind of change that was observed.</param>
        public AddNodeChange(XObject node, XObjectChange change)
            : base(node, change)
        {
        }

        #endregion

    }

}
