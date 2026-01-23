// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    /// <summary>
    /// Contains all settings for diagram export operations.
    /// </summary>
    public class DiagramExportOptions
    {
        /// <summary>
        /// Gets or sets the full path to the output file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the export format.
        /// </summary>
        public ExportFormat Format { get; set; }

        /// <summary>
        /// Gets or sets whether to render with a transparent background.
        /// </summary>
        public bool TransparentBackground { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show data types alongside property names.
        /// </summary>
        public bool ShowTypes { get; set; }

        /// <summary>
        /// Gets or sets whether the diagram currently shows types (Scalar Property Format).
        /// This reflects the diagram's DisplayNameAndType setting at export time.
        /// </summary>
        public bool DiagramShowsTypes { get; set; }

        /// <summary>
        /// Returns true if ShowTypes matches the diagram's current setting,
        /// meaning we can use original dimensions without recalculation.
        /// </summary>
        public bool UseOriginalDimensions
        {
            get { return ShowTypes == DiagramShowsTypes; }
        }
    }
}
