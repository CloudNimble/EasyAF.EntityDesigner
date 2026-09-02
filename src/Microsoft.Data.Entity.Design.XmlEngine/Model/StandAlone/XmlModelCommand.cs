// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Base class for a <see cref="ModelCommand" /> that carries the <see cref="SimpleXmlChange" /> it was built from.
    /// </summary>
    /// <remarks>
    /// The originating change is retained so the transaction can report its edits to clients through
    /// <see cref="IXmlChange" /> without having to keep a second, parallel list.
    /// </remarks>
    internal abstract class XmlModelCommand : ModelCommand
    {

        #region Fields

        /// <summary>
        /// The change this command was created from.
        /// </summary>
        private readonly SimpleXmlChange _change;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the change this command was created from.
        /// </summary>
        public SimpleXmlChange Change => _change;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlModelCommand" /> class.
        /// </summary>
        /// <param name="change">The change this command was created from.</param>
        public XmlModelCommand(SimpleXmlChange change)
            : base()
        {
            this._change = change;
        }

        #endregion

    }

}
