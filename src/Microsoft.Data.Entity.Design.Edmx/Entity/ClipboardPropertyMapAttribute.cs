// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    internal sealed class ClipboardPropertyMapAttribute : Attribute
    {
        private readonly string _attributeName;
        private readonly bool _isExcluded;

        internal ClipboardPropertyMapAttribute(string attributeName)
            : this(attributeName, false)
        {
        }

        internal ClipboardPropertyMapAttribute(string attributeName, bool isExcluded)
        {
            _attributeName = attributeName;
            _isExcluded = isExcluded;
        }

        internal string AttributeName
        {
            get { return _attributeName; }
        }

        internal bool IsExcluded
        {
            get { return _isExcluded; }
        }
    }

}
