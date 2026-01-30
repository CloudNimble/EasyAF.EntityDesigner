// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Model.Designer;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    /// <summary>
    ///     This command will change a DesignerProperty under a DesignerInfoPropertySet inside a generic
    ///     DesignerInfo, for example, "Options", "Connection", etc. We are guaranteed to have a
    ///     DesignerInfo at least, but not guaranteed to have a DesignerInfoPropertySet or a DesignerProperty.
    ///     Therefore, this will create the DesignerInfoPropertySet and the DesignerProperty if they don't exist.
    /// </summary>
    internal class ChangeDesignerPropertyCommand : Command
    {
        private readonly string _name;
        private readonly string _value;
        private readonly DesignerInfo _designerInfo;

        internal ChangeDesignerPropertyCommand(string name, string value, DesignerInfo designerInfo)
        {
            ValidateString(name);
            ValidateString(value);
            Debug.Assert(designerInfo != null, "designerInfo is null");

            _name = name;
            _value = value;
            _designerInfo = designerInfo;
        }

        internal ChangeDesignerPropertyCommand(string name, string value, DesignerInfo designerInfo, bool allowNullValues)
        {
            ValidateString(name);
            if (allowNullValues == false)
            {
                ValidateString(value);
            }
            Debug.Assert(designerInfo != null, "designerInfo is null");

            _name = name;
            _value = value;
            _designerInfo = designerInfo;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // if the DesignerInfoPropertySet doesn't exist then we need to create it.
            _designerInfo.PropertySet ??= new DesignerInfoPropertySet(_designerInfo, null);

            // if the DesignerProperty doesn't exist then we need to create it.
            if (!_designerInfo.PropertySet.TryGetDesignerProperty(_name, out DesignerProperty designerProperty))
            {
                designerProperty = new DesignerProperty(_designerInfo.PropertySet, null);
                designerProperty.LocalName.Value = _name;
                _designerInfo.PropertySet.AddDesignerProperty(_name, designerProperty);
            }

            // First let's check make sure any non-valid values are caught up the stack
            if (!designerProperty.ValueAttr.IsValidValue(_value))
            {
                throw new CommandValidationFailedException(
                    String.Format(CultureInfo.CurrentCulture, Resources.NonValidDesignerProperty, _value, _name));
            }

            // now we update the value of the designer property
            UpdateDefaultableValueCommand<string> cmdUpdateDefaultableValue = new UpdateDefaultableValueCommand<string>(designerProperty.ValueAttr, _value);
            CommandProcessor.InvokeSingleCommand(cpc, cmdUpdateDefaultableValue);

            // normalize and resolve the entire DesignerInfo
            XmlModelHelper.NormalizeAndResolve(_designerInfo);
        }
    }
}
