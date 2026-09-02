// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing
{

    /// <summary>
    ///     This class is used to sort EfiChanges using stable sort algorithm (i.e. one that is preserving order of equal values)
    ///     It remembers original position of EfiChange so the compare method can decide which should come first.
    /// </summary>
    internal class EfiChangeStableSortItem
    {
        private readonly EfiChange _change;
        private readonly int _position;

        public EfiChangeStableSortItem(EfiChange change, int position)
        {
            _change = change;
            _position = position;
        }

        public EfiChange EfiChange
        {
            get { return _change; }
        }

        public int Position
        {
            get { return _position; }
        }
    }

}
