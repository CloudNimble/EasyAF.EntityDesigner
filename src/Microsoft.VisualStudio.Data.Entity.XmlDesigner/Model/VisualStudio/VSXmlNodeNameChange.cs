// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal class VSXmlNodeNameChange : VSXmlChange, IXmlNodeNameChange
    {
        private readonly XName _oldName;
        private readonly XName _newName;

        internal VSXmlNodeNameChange(NodeNameChange modelChange)
            : base(modelChange)
        {
            _oldName = modelChange.OldName;
            _newName = modelChange.NewName;
        }

        public XName OldName
        {
            get { return _oldName; }
        }

        public XName NewName
        {
            get { return _newName; }
        }
    }

}
