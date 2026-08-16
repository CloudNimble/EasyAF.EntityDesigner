// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// Replays or reverts a rename of an element or a retarget of a processing instruction.
    /// </summary>
    /// <remarks>
    /// Repeated renames of the same node collapse into one command via <see cref="Merge(ModelCommand)" />, because emitting two
    /// edits against the same tag within a single transaction is not supported by the source modifier.
    /// </remarks>
    internal class NodeNameCommand : XmlModelCommand
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNameCommand" /> class.
        /// </summary>
        /// <param name="change">The recorded rename this command replays.</param>
        public NodeNameCommand(NodeNameChange change)
            : base(change)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Absorbs an earlier rename of the same node by adopting its original name.
        /// </summary>
        /// <param name="other">The earlier command that is a candidate for merging.</param>
        /// <returns><see langword="true" /> when <paramref name="other" /> renamed the same node and was absorbed; otherwise <see langword="false" />.</returns>
        public override bool Merge(ModelCommand other)
        {
            if (other is NodeNameCommand s
                && s != this
                && s.Change.Node == Change.Node)
            {
                ((NodeNameChange)Change).OldName = ((NodeNameChange)s.Change).OldName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reapplies the new name to the node.
        /// </summary>
        public override void Redo()
        {
            NodeNameChange nameChange = Change as NodeNameChange;
            SetName(nameChange.Node, nameChange.NewName);
        }

        /// <summary>
        /// Restores the name the node carried before the change.
        /// </summary>
        public override void Undo()
        {
            NodeNameChange nameChange = Change as NodeNameChange;
            SetName(nameChange.Node, nameChange.OldName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies a name to the node, dispatching on its node type.
        /// </summary>
        /// <param name="node">The node to rename.</param>
        /// <param name="name">The name to apply.</param>
        /// <exception cref="NotImplementedException">Thrown when the node type does not support renaming.</exception>
        /// <remarks>
        /// Attribute names cannot be changed in XLinq, so an attribute reaching this method indicates a defect in
        /// the change tracking rather than a supported scenario.
        /// </remarks>
        private static void SetName(XObject node, XName name)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    ((XElement)node).Name = name;
                    break;
                case XmlNodeType.Attribute:
                    Debug.Assert(false, "This should never happen because attribute names cannot be changed");
                    break;
                case XmlNodeType.ProcessingInstruction:
                    ((XProcessingInstruction)node).Target = name.LocalName;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

    }

}
