// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{

    /// <summary>
    ///     Attribute which may be placed on a selectable object to specify an
    ///     TreeGridDesignerBranch that should be displayed in the TreeGrid Designer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class TreeGridDesignerRootBranchAttribute : TreeGridDesignerBaseAttribute
    {
        private readonly Type _branchType;

        /// <summary>
        ///     Construct an empty OperationDesignerRootBranchAttribute
        /// </summary>
        internal TreeGridDesignerRootBranchAttribute()
        {
        }

        /// <summary>
        ///     Construct an OperationDesignerRootBranchAttribute with the given branch type.
        /// </summary>
        /// <param name="branchType"></param>
        internal TreeGridDesignerRootBranchAttribute(Type branchType)
        {
            _branchType = branchType;
        }

        /// <summary>
        ///     Type of branch to be created.
        /// </summary>
        internal Type BranchType
        {
            get { return _branchType; }
        }
    }

}
