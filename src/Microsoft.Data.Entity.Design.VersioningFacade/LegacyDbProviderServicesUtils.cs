// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Common;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    internal class LegacyDbProviderServicesUtils
    {
        public static bool CanGetDbProviderServices(IServiceProvider serviceProvider)
        {
            try
            {
                return serviceProvider.GetService(typeof(DbProviderServices)) != null;
            }
            catch (Exception)
            {
                // just swallow the exception.  Something failed with the call above.
                // this could be caused by having an out-of-date provider installed on the machine.
                // Not swallowing this exception will cause the wizard to crash.
            }

            return false;
        }
    }
}
