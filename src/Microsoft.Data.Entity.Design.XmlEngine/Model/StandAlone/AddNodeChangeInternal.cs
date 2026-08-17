// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    //[CLSCompliant(false)]
    /// <summary>
    /// An <see cref="AddNodeChange" /> that can be ordered relative to other add changes by document position.
    /// </summary>
    /// <remarks>
    /// Insertions have to be replayed against a text buffer in document order, otherwise earlier edits invalidate
    /// the positions computed for later ones. Document order alone is not sufficient, however: an edit that targets
    /// the end tag of an element sorts after every edit inside that element, which is why <see cref="endTag" /> takes
    /// part in the comparison.
    /// </remarks>
    internal class AddNodeChangeInternal : AddNodeChange, IComparable
    {

        #region Fields

        /// <summary>
        /// The node used as the basis for document order comparisons, or <see langword="null" /> when the change
        /// targets an attribute (attributes have no position in document order).
        /// </summary>
        internal XNode CompareToObject;

        /// <summary>
        /// Indicates that the change targets the end tag of <see cref="CompareToObject" /> rather than the node itself.
        /// </summary>
        internal bool endTag = false;

        //this is used on a change adding attributes. When multiple attributes are added in a single tx
        //this field should point to the last attribute that was present before the tx started
        /// <summary>
        /// The last attribute that already existed before the transaction started, used as the anchor when several
        /// attributes are added to the same element within one transaction.
        /// </summary>
        internal XAttribute lastStableAttribute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNodeChangeInternal" /> class.
        /// </summary>
        /// <param name="n">The node that was added.</param>
        /// <param name="action">The kind of change that was observed.</param>
        public AddNodeChangeInternal(XObject n, XObjectChange action)
            : base(n, action)
        {
            CompareToObject = n as XNode;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares this change with another change by the document position of the affected nodes.
        /// </summary>
        /// <param name="otherObj">The change to compare against.</param>
        /// <returns>
        /// Zero when both changes affect the same position, a negative value when this change precedes
        /// <paramref name="otherObj" />, and a positive value when it follows it. Also returns -1 when
        /// <paramref name="otherObj" /> is not an <see cref="AddNodeChangeInternal" /> of the same type.
        /// </returns>
        public int CompareTo(object otherObj)
        {
            var value = -1;
            if (otherObj is AddNodeChangeInternal other
                && other.GetType() == GetType())
            {
                if (CompareToObject is null)
                {
                    return (other.CompareToObject is null ? 0 : 1);
                }
                else if (other.CompareToObject is null)
                {
                    return -1;
                }
                else
                {
                    if (endTag && IsChildOf(CompareToObject, other.CompareToObject))
                    {
                        return 1; // then this end tag is after the other node.
                    }
                    if (other.endTag
                        && IsChildOf(other.CompareToObject, CompareToObject))
                    {
                        return -1; // then this node is before the other end tag .
                    }

                    // 0 if the nodes are equal; -1 if n1 is before n2; 1 if n1 is after n2.
                    return XNode.CompareDocumentOrder(CompareToObject, other.CompareToObject);
                }
            }

            return value;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Determines whether <paramref name="child" /> is a descendant of <paramref name="node" />.
        /// </summary>
        /// <param name="node">The candidate ancestor.</param>
        /// <param name="child">The node whose ancestry is walked.</param>
        /// <returns><see langword="true" /> when <paramref name="node" /> is found on the ancestor chain of <paramref name="child" />; otherwise <see langword="false" />.</returns>
        internal static bool IsChildOf(XNode node, XNode child)
        {
            var e = child.Parent;
            while (e is not null)
            {
                if (e == node)
                {
                    return true;
                }
                e = e.Parent;
            }
            return false;
        }

        #endregion

    }

}
