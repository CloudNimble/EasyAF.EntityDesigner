// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    // <summary>
    //     Wrapper around ErrorListProvider
    // </summary>
    internal class DesignerErrorList : IDisposable
    {
        private readonly ErrorListProvider _provider;

        public DesignerErrorList(IServiceProvider provider)
        {
            _provider = new ErrorListProvider(provider);
        }

        ~DesignerErrorList()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _provider?.Dispose();
            }
        }

        public ErrorListProvider Provider
        {
            get { return _provider; }
        }

        public void Clear()
        {
            _provider.Tasks.Clear();
        }

        public void AddItem(ErrorTask error)
        {
            _provider.Tasks.Add(error);
        }
    }
}
