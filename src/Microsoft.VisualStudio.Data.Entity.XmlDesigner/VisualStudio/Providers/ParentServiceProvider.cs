// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio.Providers
{
    internal sealed class ParentServiceProvider
    {
        private ParentServiceProvider()
        {
        }

        /// <summary>
        ///     Helper method that locates a service from our parent frame.
        ///     This can return null if the service doesn't exist or if the
        ///     parent frame doesn't exist.
        /// </summary>
        /// <typeparam name="ServiceType"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal static ServiceType GetParentService<ServiceType>(IServiceProvider provider)
        {
            object service = null;

            Debug.Assert(null != provider);
            if (null != provider)
            {
                if (provider.GetService(typeof(IVsWindowFrame)) is IVsWindowFrame ourFrame)
                {
                    var hr = ourFrame.GetProperty((int)__VSFPROPID2.VSFPROPID_ParentFrame, out object @var);

                    if (NativeMethods.Succeeded(hr)
                        && @var != null)
                    {
                        IVsWindowFrame parentFrame = (IVsWindowFrame)@var;
                        hr = parentFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out @var);
                        Debug.Assert(NativeMethods.Succeeded(hr));
                        if (NativeMethods.Succeeded(hr))
                        {
                            if (@var is IServiceProvider parentViewProvider)
                            {
                                service = parentViewProvider.GetService(typeof(ServiceType));
                            }
                        }
                    }
                }
            }
            return (ServiceType)service;
        }
    }
}
