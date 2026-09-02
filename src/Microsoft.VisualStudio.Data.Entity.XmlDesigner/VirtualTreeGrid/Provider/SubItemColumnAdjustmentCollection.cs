// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     A read-only collection of SubItemColumnAdjustment structures. Used by ItemCountChangedEventArgs
    ///     to disseminate information about changes in sub item columns.
    /// </summary>
    internal sealed class SubItemColumnAdjustmentCollection : IList
    {
        private readonly SubItemColumnAdjustment[] myInner;

        internal SubItemColumnAdjustmentCollection(SubItemColumnAdjustment[] adjustments)
        {
            myInner = adjustments;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            myInner.CopyTo(array, index);
        }

        int ICollection.Count
        {
            get { return myInner.Length; }
        }

        bool ICollection.IsSynchronized
        {
            get { return myInner.IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return myInner.SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return myInner.GetEnumerator();
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return (myInner as IList).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return (myInner as IList).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        bool IList.IsFixedSize
        {
            get { return true; }
        }

        bool IList.IsReadOnly
        {
            get { return true; }
        }

        object IList.this[int index]
        {
            get { return myInner[index]; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        ///     Retrieve a SubItemColumnAdjust from the collection
        /// </summary>
        public SubItemColumnAdjustment this[int index]
        {
            get { return myInner[index]; }
        }

        /// <summary>
        ///     Add not supported
        /// </summary>
        public void Add(SubItemColumnAdjustment adjustment)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     The number of column adjustments in the collection
        /// </summary>
        public int Count
        {
            get { return myInner.Length; }
        }

        /// <summary>
        ///     Copy items into a separate array
        /// </summary>
        /// <param name="array">A pre-allocated array</param>
        /// <param name="index">The starting index to copy from</param>
        public void CopyTo(SubItemColumnAdjustment[] array, int index)
        {
            myInner.CopyTo(array, index);
        }

        /// <summary>
        ///     Find the index of the given value in this collection
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int IndexOf(SubItemColumnAdjustment value)
        {
            return (myInner as IList).IndexOf(value);
        }

        /// <summary>
        ///     Test whether the collection contains this item.
        /// </summary>
        public bool Contains(SubItemColumnAdjustment value)
        {
            return (myInner as IList).Contains(value);
        }

        /// <summary>
        ///     Insert not supported
        /// </summary>
        public void Insert(int index, SubItemColumnAdjustment value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Remove not supported
        /// </summary>
        public void Remove(SubItemColumnAdjustment value)
        {
            throw new NotSupportedException();
        }
    }

}
