// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Encapsulates custom error information for Visual Studio extensions that extend the functionality of the Entity Data Model
    /// Designer.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Errors.Add(new ExtensionError("The 'MyFlag' annotation is not a boolean.", 4200, ExtensionErrorSeverity.Warning, line, column));
    /// </code>
    /// </example>
    /// <remarks>
    /// Add instances of this class to the <c>Errors</c> collection of a <see cref="ModelTransformExtensionContext" /> or
    /// <see cref="ModelConversionExtensionContext" /> to tell the user about a problem the extension found. The designer copies
    /// them into the Visual Studio Error List; double-clicking an entry that carries a line and column navigates there. Messages
    /// are shown verbatim, so extensions are responsible for localizing them.
    /// </remarks>
    [Serializable]
    public sealed class ExtensionError
    {

        #region Fields

        private int _column = -1;
        private int _errorCode;
        private int _line = -1;
        private readonly string _message;
        private ExtensionErrorSeverity _severity = ExtensionErrorSeverity.Warning;

        #endregion

        #region Properties

        /// <summary>
        /// The column of the input or output document in which the error occurred.
        /// </summary>
        /// <value>The column, or -1 when the error was not tied to a position.</value>
        /// <remarks>
        /// The value is passed straight through to the Visual Studio text span used to navigate to the error, so it is
        /// interpreted with the same origin the editor uses for column offsets.
        /// </remarks>
        public int Column
        {
            get => _column;
        }

        /// <summary>
        /// The error code associated with the error.
        /// </summary>
        /// <value>A non-negative code chosen by the extension.</value>
        /// <remarks>
        /// The designer does not interpret this value; it is shown to the user and is there so an extension can give its
        /// diagnostics stable, documentable identifiers.
        /// </remarks>
        public int ErrorCode
        {
            get => _errorCode;
        }

        /// <summary>
        /// The line of the input or output document in which the error occurred.
        /// </summary>
        /// <value>The line, or -1 when the error was not tied to a position.</value>
        /// <remarks>
        /// The value is passed straight through to the Visual Studio text span used to navigate to the error, so it is
        /// interpreted with the same origin the editor uses for line numbers.
        /// </remarks>
        public int Line
        {
            get => _line;
        }

        /// <summary>
        /// The message that describes the error.
        /// </summary>
        /// <value>The human-readable, already-localized description shown in the Error List.</value>
        public string Message
        {
            get => _message;
        }

        /// <summary>
        /// The severity of the error.
        /// </summary>
        /// <value>The category the entry appears under in the Error List.</value>
        /// <remarks>
        /// Unlike the other members this one is settable, so an extension can downgrade or promote an error it has already
        /// created before the designer reads the collection.
        /// </remarks>
        public ExtensionErrorSeverity Severity
        {
            get => _severity;
            set => _severity = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of <see cref="ExtensionError" />.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code associated with the error.</param>
        /// <param name="severity">The severity of the error.</param>
        /// <exception cref="ArgumentException"><paramref name="message" /> is null, empty or only whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="errorCode" /> is negative.</exception>
        /// <remarks>
        /// Use this overload when the problem cannot be attributed to a particular place in the document;
        /// <see cref="Line" /> and <see cref="Column" /> are both left at -1.
        /// </remarks>
        public ExtensionError(string message, int errorCode, ExtensionErrorSeverity severity)
        {
            _message = ValidateMessage(message);

            Initialize(errorCode, severity, -1, -1);
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="ExtensionError" />.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code associated with the error.</param>
        /// <param name="severity">The severity of the error.</param>
        /// <param name="line">The line of the input or output document in which the error occurred.</param>
        /// <param name="column">The column of the input or output document in which the error occurred.</param>
        /// <exception cref="ArgumentException"><paramref name="message" /> is null, empty or only whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line" />, <paramref name="column" /> or <paramref name="errorCode" /> is negative.</exception>
        /// <remarks>
        /// Use this overload to make the entry navigable: the Error List takes the user to this position in the document when
        /// the entry is double-clicked.
        /// </remarks>
        public ExtensionError(string message, int errorCode, ExtensionErrorSeverity severity, int line, int column)
        {
            _message = ValidateMessage(message);

            if (line < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(line));
            }

            if (column < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            Initialize(errorCode, severity, line, column);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies the validated state that is common to every constructor overload.
        /// </summary>
        /// <param name="errorCode">The error code associated with the error.</param>
        /// <param name="severity">The severity of the error.</param>
        /// <param name="line">The line of the document in which the error occurred, or -1.</param>
        /// <param name="column">The column of the document in which the error occurred, or -1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="errorCode" /> is negative.</exception>
        private void Initialize(int errorCode, ExtensionErrorSeverity severity, int line, int column)
        {
            if (errorCode < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(errorCode));
            }

            _errorCode = errorCode;
            _severity = severity;
            _line = line;
            _column = column;
        }

        /// <summary>
        /// Checks that an error message is usable before it reaches the Error List.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <returns>The validated message.</returns>
        /// <exception cref="ArgumentException"><paramref name="message" /> is null, empty or only whitespace.</exception>
        /// <remarks>
        /// The message is shown verbatim, so a blank one produces an Error List entry the user cannot act on and cannot
        /// trace back to the extension that raised it.
        /// </remarks>
        private static string ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("An extension error requires a message.", nameof(message));
            }

            return message;
        }

        #endregion

    }

}
