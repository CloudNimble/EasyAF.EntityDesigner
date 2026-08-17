// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    [Serializable]
    internal class EnumTypeMembersClipboardFormat
    {
        private readonly List<EnumTypeMemberClipboardFormat> _members = [];

        public EnumTypeMembersClipboardFormat(IEnumerable<EnumTypeMember> members)
        {
            foreach (var member in members)
            {
                _members.Add(new EnumTypeMemberClipboardFormat(member));
            }
        }

        public List<EnumTypeMemberClipboardFormat> ClipboardMembers
        {
            get { return _members; }
        }
    }
}
