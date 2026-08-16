// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// Records the rename of an element or the retarget of a processing instruction.
    /// </summary>
    /// <remarks>
    /// Both the old and the new name are captured because the XLinq notification pair only exposes one of them
    /// at a time: the old name is only readable in the "before" event and the new name only in the "after" event.
    /// </remarks>
    internal class NodeNameChange : SimpleXmlChange, IXmlNodeNameChange
    {

        #region Fields

        /// <summary>
        /// The name the node was given by the change.
        /// </summary>
        internal XName newName;

        /// <summary>
        /// The name the node carried before the change.
        /// </summary>
        internal XName oldName;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name the node was given by the change.
        /// </summary>
        public XName NewName
        {
            get => newName;
            internal set => newName = value;
        }

        /// <summary>
        /// Gets the name the node carried before the change.
        /// </summary>
        public XName OldName
        {
            get => oldName;
            internal set => oldName = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNameChange" /> class.
        /// </summary>
        /// <param name="node">The node that was renamed.</param>
        /// <param name="change">The kind of change that was observed.</param>
        public NodeNameChange(XObject node, XObjectChange change)
            : base(node, change)
        {
        }

        #endregion

    }

}
