// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Common;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    /// <summary>
    ///     Determines whether an ADO.NET provider factory can supply Entity Framework provider services.
    /// </summary>
    /// <remarks>
    ///     This used to probe for System.Data.Common.DbProviderServices from the .NET Framework's in-box
    ///     System.Data.Entity assembly. That assembly was removed along with ObjectContext code generation, so the
    ///     probe now asks for the Entity Framework 6 type of the same name. Providers that support EF6 answer this
    ///     one; the in-box type only ever identified providers built for the pre-EF6 stack.
    /// </remarks>
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
