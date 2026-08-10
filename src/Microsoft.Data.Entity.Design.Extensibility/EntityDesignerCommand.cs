// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// A single command that an <see cref="IEntityDesignerCommandFactory" /> contributes to the context menu of the Entity Data
    /// Model Designer.
    /// </summary>
    /// <remarks>
    /// A command pairs the work to perform with a rule that decides whether it is shown and enabled for the current selection.
    /// The designer allocates a menu slot per distinct command name, which is why <see cref="Equals(object)" /> and
    /// <see cref="GetHashCode" /> compare on <see cref="Name" /> alone.
    /// <para>
    /// This type is internal to the designer, so only assemblies granted <c>InternalsVisibleTo</c> can supply commands; it is
    /// not part of the third-party extension surface.
    /// </para>
    /// </remarks>
    internal class EntityDesignerCommand
    {

        #region Delegates

        /// <summary>
        /// Delegate which is called when determining whether to show or enable a command for a particular selection.
        /// </summary>
        /// <param name="selectedXElement">The XML object corresponding to the selection.</param>
        /// <param name="docView">The document view corresponding to the selection.</param>
        /// <param name="singleViewModelSelection">The selection in the view model.</param>
        /// <param name="propertiesCompartment">If this selection is within an EntityType, this specifies the Properties compartment within that EntityType.</param>
        /// <param name="isSingleSelection">Specifies whether the selection is over multiple objects or not.</param>
        /// <returns>A Tuple where the first boolean corresponds to whether the command is shown, and the second corresponds to whether the command is enabled.</returns>
        internal delegate Tuple<bool, bool> CanExecuteFunction(
            XObject selectedXElement, object docView, object singleViewModelSelection, object propertiesCompartment, bool isSingleSelection);

        /// <summary>
        /// Delegate which carries out the work of a command once the user has invoked it.
        /// </summary>
        /// <param name="selectedXElement">The XML object corresponding to the selection.</param>
        /// <param name="docView">The document view corresponding to the selection.</param>
        /// <param name="singleViewModelSelection">The selection in the view model.</param>
        /// <param name="propertiesCompartment">If this selection is within an EntityType, this specifies the Properties compartment within that EntityType.</param>
        /// <param name="isSingleSelection">Specifies whether the selection is over multiple objects or not.</param>
        internal delegate void ExecuteAction(
            XObject selectedXElement, object docView, object singleViewModelSelection, object propertiesCompartment, bool isSingleSelection);

        #endregion

        #region Fields

        private readonly CanExecuteFunction _canExecute;
        private readonly ExecuteAction _execute;
        private string _name;

        #endregion

        #region Properties

        /// <summary>
        /// Specifies whether this command is a refactoring operation, in which case it may be placed separately in the resulting
        /// context menu.
        /// </summary>
        internal bool IsRefactoringCommand { get; set; }

        /// <summary>
        /// The name of the command, and the label text that will appear in the designer's context menu.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <remarks>
        /// Null is rejected because <see cref="GetHashCode" /> hashes this value, so a null name would make the command
        /// throw the moment it was placed in a hash-based collection.
        /// </remarks>
        internal string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an EntityDesignerCommand which can be executed from a context menu in the designer.
        /// </summary>
        /// <param name="name">The name of the command, and the label text that will appear in the designer's context menu</param>
        /// <param name="executeAction">A simple Action that will get passed in the EntityDesignerSelection and the selected XElement</param>
        /// <param name="canExecuteFunction">Delegate which accepts an EntityDesignerSelection and will return a Tuple where the first boolean corresponds to whether the command is shown, and the second corresponds to whether the command is enabled</param>
        /// <param name="isRefactoringCommand">Specifies whether this command is a refactoring operation, in which case it may be placed separately in the resulting context menu</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> or <paramref name="executeAction" /> is null.</exception>
        /// <remarks>
        /// Omitting <paramref name="canExecuteFunction" /> makes the command unconditionally visible and enabled.
        /// </remarks>
        internal EntityDesignerCommand(
            string name,
            ExecuteAction executeAction,
            CanExecuteFunction canExecuteFunction = null,
            bool isRefactoringCommand = false)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (executeAction is null)
            {
                throw new ArgumentNullException(nameof(executeAction));
            }

            Name = name;
            _execute = executeAction;
            _canExecute = canExecuteFunction;
            IsRefactoringCommand = isRefactoringCommand;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether another object represents the same designer command as this one.
        /// </summary>
        /// <param name="obj">The object to compare with this command.</param>
        /// <returns>True when <paramref name="obj" /> is an <see cref="EntityDesignerCommand" /> with the same <see cref="Name" />.</returns>
        /// <remarks>
        /// Commands are identified by name because that is what the designer keys its menu slots on.
        /// </remarks>
        public override bool Equals(object obj)
        {
            // TODO support layer discrimination as well
            return obj is EntityDesignerCommand otherCommand && Name == otherCommand.Name;
        }

        /// <summary>
        /// Returns a hash code consistent with <see cref="Equals(object)" />.
        /// </summary>
        /// <returns>The hash code of <see cref="Name" />.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Delegate which is called when determining whether to show or enable this command for a particular selection
        /// </summary>
        /// <param name="selectedElement">The XML object corresponding to the selection</param>
        /// <param name="docView">The document view corresponding to the selection</param>
        /// <param name="singleViewModelSelection">The selection in the view model</param>
        /// <param name="propertiesCompartment">If this selection is within an EntityType, this specifies the Properties compartment within that EntityType</param>
        /// <param name="isSingleSelection">Specifies whether the selection is over multiple objects or not</param>
        /// <returns>a Tuple where the first boolean corresponds to whether the command is shown, and the second corresponds to whether the command is enabled</returns>
        /// <remarks>
        /// A command created without a <see cref="CanExecuteFunction" /> is always shown and always enabled.
        /// </remarks>
        [DebuggerStepThrough]
        internal Tuple<bool, bool> CanExecute(
            XObject selectedElement, object docView, object singleViewModelSelection, object propertiesCompartment, bool isSingleSelection)
        {
            return _canExecute is null
                       ? new Tuple<bool, bool>(true, true)
                       : _canExecute(selectedElement, docView, singleViewModelSelection, propertiesCompartment, isSingleSelection);
        }

        /// <summary>
        /// The execution logic of the command
        /// </summary>
        /// <param name="selectedElement">The XElement corresponding to the selection in the Entity Designer</param>
        /// <param name="docView">The document view corresponding to this selection</param>
        /// <param name="singleViewModelSelection">The selection in the view model</param>
        /// <param name="propertiesCompartment">If this selection is within an EntityType, this specifies the Properties compartment within that EntityType</param>
        /// <param name="isSingleSelection">Specifies whether the selection is over multiple objects</param>
        internal void Execute(
            XObject selectedElement, object docView, object singleViewModelSelection, object propertiesCompartment, bool isSingleSelection)
        {
            _execute(selectedElement, docView, singleViewModelSelection, propertiesCompartment, isSingleSelection);
        }

        #endregion

    }

}
