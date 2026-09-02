// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing
{
    /// <summary>
    ///     EventArgs based class for communicating an EfiChangeGroup to those
    ///     subscribing to change events on EFService <see cref="EFService">.
    /// </summary>
    internal class EfiChangedEventArgs : EventArgs
    {
        private readonly EfiChangeGroup _changes;

        internal EfiChangedEventArgs(EfiChangeGroup changes)
        {
            _changes = changes;
        }

        public EfiChangeGroup ChangeGroup
        {
            get { return _changes; }
        }
    }
}
