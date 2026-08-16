// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// An <see cref="XmlModel" /> over an XLinq document that a <see cref="VanillaXmlModelProvider" /> loaded itself.
    /// </summary>
    /// <remarks>
    /// The document is annotated with text ranges by <see cref="AnnotatedTreeBuilder" /> while it is parsed, which is
    /// what lets this model report source positions without a text buffer or a running editor behind it.
    /// </remarks>
    public class SimpleXmlModel : XmlModel
    {

        #region Fields

        /// <summary>
        /// The parsed, text-range annotated document.
        /// </summary>
        private readonly XDocument _doc;

        /// <summary>
        /// The location the document was loaded from; updated when the underlying file is renamed.
        /// </summary>
        private Uri _uri;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parsed document backing this model.
        /// </summary>
        public override XDocument Document => _doc;

        /// <summary>
        /// Gets the absolute URI of the document.
        /// </summary>
        public override string Name => _uri.AbsoluteUri;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleXmlModel" /> class.
        /// </summary>
        /// <param name="uri">The location the document was loaded from.</param>
        /// <param name="doc">The parsed, text-range annotated document.</param>
        internal SimpleXmlModel(Uri uri, XDocument doc)
        {
            _uri = uri;
            _doc = doc;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the document may be edited.
        /// </summary>
        /// <returns>Always <see langword="true" />.</returns>
        /// <remarks>
        /// There is no source control or checkout gate outside of Visual Studio, so a standalone model is always editable.
        /// </remarks>
        public override bool CanEditXmlModel()
        {
            return true;
        }

        /// <summary>
        /// Gets the source position of an object in the document.
        /// </summary>
        /// <param name="xobject">The object whose source position is requested.</param>
        /// <returns>The span the object occupies, or an empty span when no text range was recorded for it or any of its ancestors.</returns>
        /// <remarks>
        /// Line and column numbers are converted from the one-based values recorded by the parser to the zero-based
        /// values Visual Studio expects. The end column is deliberately not decremented because the XML editor treats
        /// the closing column as the first column after the "&gt;" bracket. When an object carries no text range of
        /// its own the parent's range is returned, which keeps navigation working for synthesized nodes.
        /// </remarks>
        public override TextSpan GetTextSpan(XObject xobject)
        {
            TextSpan ts = new TextSpan();

            if (xobject is XElement el)
            {
                var etr = el.GetTextRange();
                if (etr is not null)
                {
                    // subtract 1 from these, since VS uses zero-based column & line numbers
                    ts.iStartLine = etr.OpenStartLine - 1;
                    ts.iStartIndex = etr.OpenStartColumn - 1;
                    ts.iEndLine = etr.CloseEndLine - 1;
                    // don't subtrace 1 from here, since the XML editor treats the "closing" column is the first
                    // column after the ">" bracket
                    ts.iEndIndex = etr.CloseEndColumn;
                }
            }
            else
            {
                var atr = xobject.GetTextRange();
                if (atr is not null)
                {
                    // subtract 1 from these, since VS uses zero-based column & line numbers
                    ts.iStartLine = atr.OpenStartLine - 1;
                    ts.iStartIndex = atr.OpenStartColumn - 1;
                    ts.iEndLine = atr.CloseEndLine - 1;
                    // don't subtrace 1 from here, since the XML editor treats the "closing" column is the first
                    // column after the ">" bracket
                    ts.iEndIndex = atr.CloseEndColumn;
                }
                else
                {
                    if (xobject.Parent is not null
                        && xobject.Parent != xobject)
                    {
                        // fallback case.  If we couldn't get a text range for this element, use the one for the parent.
                        return GetTextSpan(xobject.Parent);
                    }
                }
            }

            return ts;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Updates the URI reported by <see cref="Name" /> after the underlying file has been renamed.
        /// </summary>
        /// <param name="uri">The new location of the document.</param>
        internal void SetName(Uri uri)
        {
            _uri = uri;
        }

        #endregion

    }

}
