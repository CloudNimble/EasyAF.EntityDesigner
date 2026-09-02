// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails
{
    internal static class MappingDetailsImages
    {
        public static readonly short ICONS_TABLE = 0;
        public static readonly short ICONS_FUNCTION = 1;
        public static readonly short ICONS_PROPERTY = 2;
        public static readonly short ICONS_CONDITION = 3;
        public static readonly short ICONS_PARAMETER = 4;
        public static readonly short ICONS_FOLDER = 5;
        public static readonly short ICONS_PROPERTY_KEY = 6;
        public static readonly short ICONS_COLUMN = 7;
        public static readonly short ICONS_RESULT_BINDING = 8;
        public static readonly short ICONS_COLUMN_KEY = 9;
        public static readonly short ICONS_COMPLEX_PROPERTY = 10;

        public static readonly short ARROWS_LEFT = 0;
        public static readonly short ARROWS_BOTH = 1;
        public static readonly short ARROWS_RIGHT = 2;

        public static readonly short TOOLBAR_TABLE = 0;
        public static readonly short TOOLBAR_SPROCS = 1;

        private static ImageList _imageListIcons;
        private static ImageList _imageListArrows;
        private static ImageList _imageListToolbar;

        public static ImageList GetToolbarImageList()
        {
            return _imageListToolbar
                       ??= ThemeUtils.GetThemedImageList(
                           EdmxDesignerResources.MappingDetailsCommandStrip,
                           EnvironmentColors.CommandBarOptionsBackgroundColorKey);
        }

        public static ImageList GetIconsImageList()
        {
            return _imageListIcons
                       ??= ThemeUtils.GetThemedImageList(
                           EdmxDesignerResources.MappingDetailsIconsImageList,
                           TreeViewColors.BackgroundColorKey);
        }

        public static ImageList GetArrowsImageList()
        {
            return _imageListArrows
                       ??= ThemeUtils.GetThemedImageList(
                           EdmxDesignerResources.MappingDetailsArrowsImageList,
                           TreeViewColors.BackgroundColorKey);
        }

        public static void InvalidateCache()
        {
            _imageListIcons = null;
            _imageListArrows = null;
            _imageListToolbar = null;
        }
    }
}