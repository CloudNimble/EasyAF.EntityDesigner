// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class CopyComplexTypeCommand : CopyAnnotatableElementCommand
    {
        private readonly ComplexTypeClipboardFormat _clipboardComplexType;
        private ComplexType _createdComplexType;

        /// <summary>
        ///     Creates a copy of ComplexType from clipboard format
        /// </summary>
        /// <param name="clipboardEntity"></param>
        /// <returns></returns>
        internal CopyComplexTypeCommand(ComplexTypeClipboardFormat clipboardComplexType)
        {
            _clipboardComplexType = clipboardComplexType;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // create copy of the ComplexType
            CreateComplexTypeCommand cmd = new CreateComplexTypeCommand(_clipboardComplexType.Name, true);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);
            _createdComplexType = cmd.ComplexType;

            // copy child properties
            CopyPropertiesCommand cmd2 = new CopyPropertiesCommand(_clipboardComplexType.Properties, _createdComplexType);
            CommandProcessor.InvokeSingleCommand(cpc, cmd2);
            AddAnnotations(_clipboardComplexType, _createdComplexType);
        }

        internal ComplexType ComplexType
        {
            get { return _createdComplexType; }
        }
    }
}
