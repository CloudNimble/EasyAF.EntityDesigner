// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal class VSXmlChange : IXmlChange
    {
        private readonly XObject _node;
        private readonly XObjectChange _action;

        internal VSXmlChange(XmlModelChange modelChange)
        {
            _node = modelChange.Node;
            _action = modelChange.Action;
        }

        public XObject Node
        {
            get { return _node; }
        }

        public XObjectChange Action
        {
            get { return _action; }
        }
    }

}
