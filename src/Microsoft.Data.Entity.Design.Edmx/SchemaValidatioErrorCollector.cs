// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Schema;

namespace Microsoft.Data.Entity.Design.Edmx
{
    /// <summary>
    ///     Collects the schema validation errors raised while validating a document against the EDMX XSD.
    /// </summary>
    /// <remarks>
    ///     This retains the message and source location of every error, not just a count. A document that fails XSD
    ///     validation is opened in the XML editor rather than the designer, so the user needs to be told which part of
    ///     the document the validator rejected.
    /// </remarks>
    internal class SchemaValidationErrorCollector
    {
        private readonly List<string> _errors = [];

        /// <summary>
        ///     The number of errors raised during validation.
        /// </summary>
        internal int ErrorCount
        {
            get { return _errors.Count; }
        }

        /// <summary>
        ///     The validation errors, each formatted with its source location where the validator supplied one.
        /// </summary>
        internal IList<string> Errors
        {
            get { return _errors; }
        }

        /// <summary>
        ///     Handles a validation event by recording its message and location.
        /// </summary>
        /// <param name="sender">The source of the validation event.</param>
        /// <param name="e">The validation event, carrying the message and the position it was raised at.</param>
        internal void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            if (e is null)
            {
                return;
            }

            // e.Exception carries the position; it is null for errors the validator could not locate.
            _errors.Add(
                e.Exception is null
                    ? e.Message
                    : string.Format(
                        CultureInfo.CurrentCulture, Resources.EscherValidation_Structural_XmlSchemaErrorLocation, e.Message,
                        e.Exception.LineNumber, e.Exception.LinePosition));
        }
    }
}
