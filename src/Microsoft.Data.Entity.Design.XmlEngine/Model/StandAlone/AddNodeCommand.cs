// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Replays or reverts the insertion of a node into an XLinq tree.
    /// </summary>
    /// <remarks>
    /// Redo re-inserts the node in front of the sibling that followed it originally, which is the only way to
    /// restore its exact position; attributes are simply appended because XLinq gives them no stable ordering.
    /// </remarks>
    internal class AddNodeCommand : XmlModelCommand
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNodeCommand" /> class.
        /// </summary>
        /// <param name="change">The recorded insertion this command replays.</param>
        public AddNodeCommand(AddNodeChange change)
            : base(change)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Always returns <see langword="false" />; insertions are never merged with other commands.
        /// </summary>
        /// <param name="other">The earlier command that is a candidate for merging.</param>
        /// <returns><see langword="false" />.</returns>
        public override bool Merge(ModelCommand other)
        {
            return false;
        }

        /// <summary>
        /// Re-inserts the node at the position it originally occupied.
        /// </summary>
        public override void Redo()
        {
            AddNodeChange c = (AddNodeChange)Change;
            var node = Change.Node;
            if (node is XNode xn)
            {
                if (c.NextNode is XNode nextSibling)
                {
                    nextSibling.AddBeforeSelf(xn);
                }
                else
                {
                    c.Parent.Add(xn);
                }
            }
            else
            {
                // Attributes cannot remember their relative position in XLinq!
                XAttribute a = (XAttribute)node;
                c.Parent.Add(a);
            }
        }

        /// <summary>
        /// Detaches the node that was inserted.
        /// </summary>
        public override void Undo()
        {
            AddNodeChange c = (AddNodeChange)Change;
            Debug.Assert(c.Parent is not null, "c.Parent is not null");
            var node = Change.Node;
            if (node is XNode xn)
            {
                xn.Remove();
            }
            else
            {
                XAttribute a = (XAttribute)node;
                a.Remove();
            }
        }

        #endregion

    }

}
