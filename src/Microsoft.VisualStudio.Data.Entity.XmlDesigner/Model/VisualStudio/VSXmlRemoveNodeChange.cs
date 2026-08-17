// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal class VSXmlRemoveNodeChange : VSXmlChange, IXmlRemoveNodeChange
    {
        private readonly XObject _nextNode;
        private readonly XContainer _parent;

        internal VSXmlRemoveNodeChange(RemoveNodeChange modelChange)
            : base(modelChange)
        {
            _nextNode = modelChange.NextNode;
            _parent = modelChange.Parent;
        }

        public XObject NextNode
        {
            get { return _nextNode; }
        }

        public XContainer Parent
        {
            get { return _parent; }
        }
    }

}
