// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal class VSXmlNodeValueChange : VSXmlChange, IXmlNodeValueChange
    {
        private readonly string _oldValue;
        private readonly string _newValue;

        internal VSXmlNodeValueChange(NodeValueChange modelChange)
            : base(modelChange)
        {
            _oldValue = modelChange.OldValue;
            _newValue = modelChange.NewValue;
        }

        public string OldValue
        {
            get { return _oldValue; }
        }

        public string NewValue
        {
            get { return _newValue; }
        }
    }

}
