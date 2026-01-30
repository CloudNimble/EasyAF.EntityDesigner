// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class CreateOnDeleteActionCommand : Command
    {
        private readonly AssociationEnd _parentEnd;
        private readonly string _action;
        private OnDeleteAction _createdAction;

        /// <summary>
        ///     Creates new OnDeleteAction for AssociationEnd or changes existing one
        /// </summary>
        /// <param name="end"></param>
        /// <param name="action"></param>
        internal CreateOnDeleteActionCommand(AssociationEnd end, string action)
        {
            CommandValidation.ValidateAssociationEnd(end);
            ValidateString(action);

            _parentEnd = end;
            _action = action;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // if the OnDeleteAction already exists then only change the action value
            _createdAction = _parentEnd.OnDeleteAction;
            if (_createdAction == null)
            {
                _createdAction = new OnDeleteAction(_parentEnd, null);
                _parentEnd.OnDeleteAction = _createdAction;
            }
            _createdAction.Action.Value = _action;
            XmlModelHelper.NormalizeAndResolve(_createdAction);
        }

        internal OnDeleteAction OnDeleteAction
        {
            get { return _createdAction; }
        }
    }
}
