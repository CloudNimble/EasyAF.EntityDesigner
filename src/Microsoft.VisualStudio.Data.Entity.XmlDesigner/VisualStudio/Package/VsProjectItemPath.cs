// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package
{

    // <summary>
    //     This is just wrapper for project item path
    // </summary>
    internal struct VsProjectItemPath
    {
        internal Url BaseUrl;
        internal string RelativePath;

        internal VsProjectItemPath(Url baseUrl, string relativePath)
        {
            BaseUrl = baseUrl;
            RelativePath = relativePath;
        }

        internal string Path
        {
            get
            {
                if (BaseUrl != null
                    && !string.IsNullOrEmpty(RelativePath))
                {
                    Url url = new Url(BaseUrl, RelativePath);
                    return url.AbsoluteUrl;
                }
                return RelativePath;
            }
        }
    }

}
