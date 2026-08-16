// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// Base record of a single mutation observed on an XLinq tree while a <see cref="SimpleTransaction" /> is active.
    /// </summary>
    /// <remarks>
    /// The XLinq change notifications only report the affected node and the kind of change, so the surrounding
    /// context that is needed to undo the edit (most importantly the parent container, which is already detached
    /// by the time the "after" event fires) is captured here while it is still available.
    /// </remarks>
    internal class SimpleXmlChange : IXmlChange
    {

        #region Fields

        /// <summary>
        /// The kind of change that was observed.
        /// </summary>
        private readonly XObjectChange action;

        /// <summary>
        /// The node the change applies to.
        /// </summary>
        private readonly XObject node;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the kind of change that was observed.
        /// </summary>
        public XObjectChange Action => action;

        /// <summary>
        /// Gets the node the change applies to.
        /// </summary>
        public XObject Node => node;

        /// <summary>
        /// Gets or sets the container the node belonged to when the change was recorded.
        /// </summary>
        /// <remarks>
        /// This is recorded explicitly because a removed node no longer exposes its former parent, and an added
        /// node has not been attached yet when the "before" notification is raised.
        /// </remarks>
        public XContainer Parent { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleXmlChange" /> class.
        /// </summary>
        /// <param name="n">The node the change applies to.</param>
        /// <param name="a">The kind of change that was observed.</param>
        public SimpleXmlChange(XObject n, XObjectChange a)
        {
            node = n;
            action = a;
        }

        #endregion

    }

}
