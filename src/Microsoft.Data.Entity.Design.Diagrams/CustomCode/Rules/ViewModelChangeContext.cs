// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Design.Dsl.Rules
{
    internal class ViewModelChangeContext
    {
        private readonly List<CommonViewModelChange> _modelChanges = [];
        private static readonly Guid Guid = new Guid("1FC81C5A-159D-40c1-A9BF-E10E22031F7F");

        internal static ViewModelChangeContext GetNewOrExistingContext(Transaction tx)
        {
            Debug.Assert(tx != null);
            ViewModelChangeContext context = null;
            tx.Context.ContextInfo.TryGetValue(Guid, out object o);
            if (o == null)
            {
                context = new ViewModelChangeContext();
                tx.Context.ContextInfo.Add(Guid, context);
            }
            else
            {
                context = o as ViewModelChangeContext;
                Debug.Assert(context != null);
            }
            return context;
        }

        internal static ViewModelChangeContext GetExistingContext(Transaction tx)
        {
            Debug.Assert(tx != null);
            ViewModelChangeContext context = null;
            tx.Context.ContextInfo.TryGetValue(Guid, out object o);
            if (o != null)
            {
                context = o as ViewModelChangeContext;
                Debug.Assert(context != null);
            }

            return context;
        }

        internal List<CommonViewModelChange> ViewModelChanges
        {
            get { return _modelChanges; }
        }
    }

    internal abstract class CommonViewModelChange
    {
        /// <summary>
        ///     Changes will be invoked in order of priority (less number means it will be invoked sooner)
        ///     This property MUST be immutable since changes are sorted based on this
        /// </summary>
        internal virtual int InvokeOrderPriority
        {
            get { return 1000; }
        }
    }

    internal class ViewModelChangeComparer : IComparer<CommonViewModelChange>
    {
        public int Compare(CommonViewModelChange x, CommonViewModelChange y)
        {
            return x.InvokeOrderPriority < y.InvokeOrderPriority ? -1 : 1;
        }
    }
}
