// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.Package
{

    /// <summary>
    /// Identifies the type of element that was clicked in the diagram.
    /// </summary>
    internal enum DiagramHitTarget
    {
        /// <summary>Empty diagram surface (no element)</summary>
        Surface,
        /// <summary>An association connector</summary>
        Association,
        /// <summary>An entity type shape</summary>
        EntityType,
        /// <summary>An inheritance connector</summary>
        Inheritance,
        /// <summary>A scalar property within an entity</summary>
        ScalarProperty,
        /// <summary>A complex property within an entity</summary>
        ComplexProperty,
        /// <summary>A navigation property within an entity</summary>
        NavigationProperty,
        /// <summary>Unknown or unsupported element</summary>
        Unknown
    }

}
