// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;

namespace Microsoft.Data.Entity.Design.Edmx
{
    internal class EDMXModelChangeEventArgs : ModelChangeEventArgs
    {
        private readonly EfiChangeGroup _efiChangeGroup;

        internal EDMXModelChangeEventArgs(EfiChangeGroup changeGroup)
        {
            _efiChangeGroup = changeGroup;
        }

        public override IEnumerable<ModelNodeChangeInfo> Changes
        {
            get
            {
                foreach (var c in _efiChangeGroup.Changes)
                {
                    var t = GetChangeTypeFromEfiChange(c);
                    ModelNodeChangeInfo info = new ModelNodeChangeInfo(c.Changed, t);
                    yield return info;
                }
            }
        }

        internal static ModelNodeChangeType GetChangeTypeFromEfiChange(EfiChange c)
        {
            ModelNodeChangeType t;

            switch (c.Type)
            {
                case EfiChange.EfiChangeType.Create:
                    t = ModelNodeChangeType.Added;
                    break;
                case EfiChange.EfiChangeType.Delete:
                    t = ModelNodeChangeType.Deleted;
                    break;
                case EfiChange.EfiChangeType.Update:
                    t = ModelNodeChangeType.Changed;
                    break;
                default:
                    Debug.Fail("unexpected type of EfiChangeType");
                    throw new InvalidOperationException();
            }

            return t;
        }
    }
}
