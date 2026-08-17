// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Records a change to the value of an element, attribute, text node, comment or processing instruction.
    /// </summary>
    /// <remarks>
    /// Both the old and the new value are captured because the XLinq notification pair only exposes one of them
    /// at a time: the old value is only readable in the "before" event and the new value only in the "after" event.
    /// </remarks>
    internal class NodeValueChange : SimpleXmlChange, IXmlNodeValueChange
    {

        #region Fields

        /// <summary>
        /// The value the node was given by the change.
        /// </summary>
        internal String newValue;

        /// <summary>
        /// The value the node carried before the change.
        /// </summary>
        internal String oldValue;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value the node was given by the change.
        /// </summary>
        public String NewValue
        {
            get => newValue;
            set => newValue = value;
        }

        /// <summary>
        /// Gets or sets the value the node carried before the change.
        /// </summary>
        public String OldValue
        {
            get => oldValue;
            set => oldValue = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeValueChange" /> class.
        /// </summary>
        /// <param name="node">The node whose value changed.</param>
        /// <param name="change">The kind of change that was observed.</param>
        public NodeValueChange(XObject node, XObjectChange change)
            : base(node, change)
        {
        }

        #endregion

    }

}
