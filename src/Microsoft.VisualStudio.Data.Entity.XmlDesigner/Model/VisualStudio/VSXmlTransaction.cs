// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.XmlEditor;
using XmlModel = Microsoft.Data.Entity.Design.XmlEngine.Model.XmlModel;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio
{

    internal sealed class VSXmlTransaction : XmlTransaction
    {
        private readonly XmlEditingScope _editorTransaction;
        private readonly VSXmlModelProvider _provider;

        private readonly Dictionary<XmlModelChange, IXmlChange> _changeMap = [];

        public VSXmlTransaction(
            VSXmlModelProvider provider,
            XmlEditingScope editorTransaction)
        {
            _provider = provider;
            _editorTransaction = editorTransaction;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _editorTransaction.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override XmlModelProvider Provider
        {
            get { return _provider; }
        }

        public override string Name
        {
            get { return _editorTransaction.Name; }
        }

        public override XmlTransaction Parent
        {
            get
            {
                var parentTx =
                    _provider.GetTransaction(_editorTransaction.Parent);
                return parentTx;
            }
        }

        public override object UserState
        {
            get { return _editorTransaction.UserState; }
        }

        public override object UndoUserState
        {
            get
            {
                object undoUserState = null;
                if (_editorTransaction.UndoScope != null)
                {
                    undoUserState = _editorTransaction.UndoScope.UserState;
                }
                return undoUserState;
            }
        }

        public override XmlTransactionStatus Status
        {
            get
            {
                var status = XmlTransactionStatus.Aborted;
                switch (_editorTransaction.Status)
                {
                    case XmlEditingScopeStatus.Reverted:
                        status = XmlTransactionStatus.Aborted;
                        break;
                    case XmlEditingScopeStatus.Active:
                        status = XmlTransactionStatus.Active;
                        break;
                    case XmlEditingScopeStatus.Completed:
                        status = XmlTransactionStatus.Committed;
                        break;
                    default:
                        status = XmlTransactionStatus.Aborted;
                        break;
                }
                return status;
            }
        }

        public override IEnumerable<IXmlChange> Changes()
        {
            foreach (var model in _provider.OpenXmlModels)
            {
                foreach (var change in Changes(model))
                {
                    yield return change;
                }
            }
        }

        public override IEnumerable<IXmlChange> Changes(XmlModel model)
        {
            VSXmlModel vsXmlModel = model as VSXmlModel;
            Debug.Assert(vsXmlModel != null, "vsXmlModel != null");
            if (vsXmlModel != null)
            {
                var internalModel = vsXmlModel.XmlModel;
                foreach (var modelChange in _editorTransaction.Changes(internalModel))
                {
                    yield return GetXmlChange(modelChange);
                }
            }
        }

        public override void Commit()
        {
            _editorTransaction.Complete();
        }

        public override void Rollback()
        {
            _editorTransaction.Revert();
        }

        private IXmlChange GetXmlChange(XmlModelChange modelChange)
        {
            if (_changeMap.TryGetValue(modelChange, out IXmlChange result))
            {
                return result;
            }

            if (result == null)
            {
                if (modelChange is AddNodeChange addChange)
                {
                    result = new VSXmlAddNodeChange(addChange);
                }
            }

            if (result == null)
            {
                if (modelChange is RemoveNodeChange removeChange)
                {
                    result = new VSXmlRemoveNodeChange(removeChange);
                }
            }

            if (result == null)
            {
                if (modelChange is NodeNameChange nodeNameChange)
                {
                    result = new VSXmlNodeNameChange(nodeNameChange);
                }
            }

            if (result == null)
            {
                if (modelChange is NodeValueChange nodeValueChange)
                {
                    result = new VSXmlNodeValueChange(nodeValueChange);
                }
            }

            if (result != null)
            {
                _changeMap[modelChange] = result;
            }

            return result;
        }
    }

}
