// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Data.Entity.Design.XmlEngine.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common
{
    internal class VsTextBufferFactory
    {
        /// <summary>
        ///     Instantiates and sites a new VsTextBuffer.
        /// </summary>
        internal static T CreateInstance<T>(IServiceProvider serviceProvider, ILocalRegistry localRegistry) where T : IVsTextBuffer
        {
            ArgumentValidation.CheckForNullReference(serviceProvider, "serviceProvider");
            ArgumentValidation.CheckForNullReference(localRegistry, "localRegistry");

            var obj = CreateObject<T>(localRegistry);

            IObjectWithSite objectWithSite = obj as IObjectWithSite;
            objectWithSite?.SetSite(serviceProvider);

            return obj;
        }

        /// <summary>
        ///     Instantiates and sites a new VsTextBuffer.
        /// </summary>
        internal static T CreateInstance<T>(System.IServiceProvider serviceProvider, ILocalRegistry localRegistry) where T : IVsTextBuffer
        {
            ArgumentValidation.CheckForNullReference(serviceProvider, "serviceProvider");
            ArgumentValidation.CheckForNullReference(localRegistry, "localRegistry");

            var obj = CreateObject<T>(localRegistry);

            IObjectWithSite objectWithSite = obj as IObjectWithSite;
            objectWithSite?.SetSite(serviceProvider);

            return obj;
        }

        /// <summary>
        ///     Instantiates and sites a new VsTextBuffer.
        /// </summary>
        internal static T CreateInstance<T>(ServiceProviderHelper serviceProvider, ILocalRegistry localRegistry) where T : IVsTextBuffer
        {
            ArgumentValidation.CheckForNullReference(serviceProvider, "serviceProvider");
            ArgumentValidation.CheckForNullReference(localRegistry, "localRegistry");

            var obj = CreateObject<T>(localRegistry);

            IObjectWithSite objectWithSite = obj as IObjectWithSite;
            objectWithSite?.SetSite(serviceProvider);

            return obj;
        }

        /// <summary>
        ///     Instanciates a new VsTextBuffer from the local registry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="localRegistry"></param>
        /// <returns></returns>
        private static T CreateObject<T>(ILocalRegistry localRegistry)
        {
            object obj = null;

            var iid = typeof(T).GUID;
            var ptr = IntPtr.Zero;

            NativeMethods.ThrowOnFailure(
                localRegistry.CreateInstance(typeof(VsTextBufferClass).GUID, null, ref iid, (uint)CLSCTX.CLSCTX_INPROC_SERVER, out ptr));

            try
            {
                obj = Marshal.GetObjectForIUnknown(ptr);
            }
            finally
            {
                Marshal.Release(ptr);
            }

            return (T)obj;
        }
    }
}
