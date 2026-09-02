// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Replays or reverts a change to the value of an element, attribute, text node, comment or processing instruction.
    /// </summary>
    /// <remarks>
    /// The type name carries a historical spelling ("Commmand") that is preserved for compatibility with existing
    /// callers. Repeated value edits of the same node collapse into one command via <see cref="Merge(ModelCommand)" />, because
    /// emitting two edits against the same text range within a single transaction is not supported by the source modifier.
    /// </remarks>
    internal class NodeValueCommmand : XmlModelCommand
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeValueCommmand" /> class.
        /// </summary>
        /// <param name="change">The recorded value change this command replays.</param>
        public NodeValueCommmand(NodeValueChange change)
            : base(change)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Absorbs an earlier value change of the same node by adopting its original value.
        /// </summary>
        /// <param name="other">The earlier command that is a candidate for merging.</param>
        /// <returns><see langword="true" /> when <paramref name="other" /> changed the same node and was absorbed; otherwise <see langword="false" />.</returns>
        public override bool Merge(ModelCommand other)
        {
            if (other is NodeValueCommmand s
                && s != this
                && s.Change.Node == Change.Node)
            {
                ((NodeValueChange)Change).OldValue = ((NodeValueChange)s.Change).OldValue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reapplies the new value to the node.
        /// </summary>
        public override void Redo()
        {
            NodeValueChange valueChange = Change as NodeValueChange;
            SetValue(valueChange.Node, valueChange.NewValue);
        }

        /// <summary>
        /// Restores the value the node carried before the change.
        /// </summary>
        public override void Undo()
        {
            NodeValueChange valueChange = Change as NodeValueChange;
            SetValue(valueChange.Node, valueChange.OldValue);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies a value to the node, dispatching on its node type.
        /// </summary>
        /// <param name="node">The node whose value is set.</param>
        /// <param name="value">The value to apply.</param>
        /// <exception cref="NotImplementedException">Thrown when the node type does not carry a settable value.</exception>
        private static void SetValue(XObject node, string value)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    ((XElement)node).Value = value;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    ((XText)node).Value = value;
                    break;
                case XmlNodeType.ProcessingInstruction:
                    ((XProcessingInstruction)node).Data = value;
                    break;
                case XmlNodeType.Comment:
                    ((XComment)node).Value = value;
                    break;
                case XmlNodeType.Attribute:
                    ((XAttribute)node).Value = value;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

    }

}
