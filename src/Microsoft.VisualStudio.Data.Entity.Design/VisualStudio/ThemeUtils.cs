// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    internal static class ThemeUtils
    {
        public static readonly Color TransparentColor = Color.Magenta;

        public static ImageList GetThemedImageList(Bitmap bitmap, ThemeResourceKey backgroundColorKey)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            bitmap.MakeTransparent(TransparentColor);
            var themedBitmap = ThemeBitmap(bitmap, VSColorTheme.GetThemedColor(backgroundColorKey));
            ImageList imageList = new ImageList
                {
                    ColorDepth = ColorDepth.Depth32Bit,
                    ImageSize = new Size(16, 16)
                };
            imageList.Images.AddStrip(themedBitmap);

#pragma warning disable 0618 // DpiHelper is obsolete, need to move to DpiAwareness (and ImageManifest)
            // scales images as appropriate for screen resolution
            DpiHelper.LogicalToDeviceUnits(ref imageList);
#pragma warning restore 0618

            return imageList;
        }

        public static Bitmap GetThemedButtonImage(Bitmap bitmap, ThemeResourceKey backgroundColorKey)
        {
            return GetThemedButtonImage(bitmap, VSColorTheme.GetThemedColor(backgroundColorKey));
        }

        public static Bitmap GetThemedButtonImage(Bitmap bitmap, Color backgroundColor)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            bitmap.MakeTransparent(TransparentColor);
            var themedBitmap = ThemeBitmap(bitmap, backgroundColor);

#pragma warning disable 0618 // DpiHelper is obsolete, need to move to DpiAwareness (and ImageManifest)
            // scales images as appropriate for screen resolution
            DpiHelper.LogicalToDeviceUnits(ref themedBitmap);
#pragma warning restore 0618

            return themedBitmap;
        }

        /// <summary>
        /// Gets a themed button image for property icons in compartments.
        /// High-res 128x128 source images represent 16x16 logical (8x scale = 768 DPI).
        /// </summary>
        /// <param name="bitmap">The source bitmap (typically 128x128 high-res)</param>
        /// <param name="backgroundColor">The background color for theming</param>
        /// <param name="scaleFor100Zoom">True if zoom is at 100% and we need to scale to device pixels</param>
        public static Bitmap GetThemedPropertyIcon(Bitmap bitmap, Color backgroundColor, bool scaleFor100Zoom)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            var themedBitmap = ThemeBitmap(bitmap, backgroundColor);

            // At 100% zoom, the DSL framework draws images at their native pixel size
            // without any zoom transformation. We need to scale the high-res image
            // to the correct device pixel size.
            // At non-100% zoom, the DSL zoom transformation handles scaling, so we
            // return the high-res image for best quality.
            if (scaleFor100Zoom)
            {
                const int logicalSize = 16;

#pragma warning disable 0618 // DpiHelper is obsolete, need to move to DpiAwareness (and ImageManifest)
                int deviceSize = DpiHelper.LogicalToDeviceUnitsX(logicalSize);
#pragma warning restore 0618

                // Scale the high-res image to device size using high-quality interpolation
                var scaledBitmap = new Bitmap(deviceSize, deviceSize);
                scaledBitmap.SetResolution(themedBitmap.HorizontalResolution, themedBitmap.VerticalResolution);
                using (var g = Graphics.FromImage(scaledBitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(themedBitmap, 0, 0, deviceSize, deviceSize);
                }
                return scaledBitmap;
            }

            // At non-100% zoom, return the high-res image with DPI metadata set.
            // This tells the system the 128x128 image represents 16x16 logical pixels (8x scale).
            // The DSL zoom transformation will scale it appropriately.
            float scaleFactor = bitmap.Width / 16f;
            themedBitmap.SetResolution(96f * scaleFactor, 96f * scaleFactor);
            return themedBitmap;
        }

        private static Bitmap ThemeBitmap(Bitmap bitmap, Color backgroundColor)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsUIShell)) is not IVsUIShell5 uiShell5)
            {
                return bitmap;
            }
            Bitmap bitmapClone = (Bitmap)bitmap.Clone();
            var bitmapData
                = bitmapClone.LockBits(
                    new Rectangle(0, 0, bitmapClone.Width, bitmapClone.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
            var size = Math.Abs(bitmapData.Stride) * bitmapData.Height;
            var bytes = new byte[size];
            Marshal.Copy(bitmapData.Scan0, bytes, 0, size);
            uiShell5.ThemeDIBits(
                (uint)size,
                bytes,
                (uint)bitmapData.Width,
                (uint)bitmapData.Height,
                bitmapData.Stride > 0,
                (uint)ColorTranslator.ToWin32(backgroundColor));
            Marshal.Copy(bytes, 0, bitmapData.Scan0, size);
            bitmapClone.UnlockBits(bitmapData);
            return bitmapClone;
        }

        /// <summary>
        /// Colorizes all non-transparent pixels in a bitmap to the specified foreground color,
        /// preserving the alpha channel. Use this when you need an icon to match a specific
        /// text color rather than relying on VS automatic theming.
        /// </summary>
        /// <param name="bitmap">The source bitmap to colorize.</param>
        /// <param name="foregroundColor">The color to apply to all non-transparent pixels.</param>
        /// <returns>A new bitmap with all non-transparent pixels set to the foreground color.</returns>
        public static Bitmap ColorizeIcon(Bitmap bitmap, Color foregroundColor)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            result.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            var bitmapData = result.LockBits(
                new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            var sourceBitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int size = Math.Abs(bitmapData.Stride) * bitmapData.Height;
                byte[] sourceBytes = new byte[size];
                byte[] resultBytes = new byte[size];

                Marshal.Copy(sourceBitmapData.Scan0, sourceBytes, 0, size);

                // Process each pixel: preserve alpha, set RGB to foreground color
                for (int i = 0; i < size; i += 4)
                {
                    byte alpha = sourceBytes[i + 3];
                    if (alpha > 0)
                    {
                        // BGRA format
                        resultBytes[i] = foregroundColor.B;     // Blue
                        resultBytes[i + 1] = foregroundColor.G; // Green
                        resultBytes[i + 2] = foregroundColor.R; // Red
                        resultBytes[i + 3] = alpha;             // Alpha (preserved)
                    }
                    // Transparent pixels remain transparent (all zeros)
                }

                Marshal.Copy(resultBytes, 0, bitmapData.Scan0, size);
            }
            finally
            {
                bitmap.UnlockBits(sourceBitmapData);
                result.UnlockBits(bitmapData);
            }

            return result;
        }

        /// <summary>
        /// Gets a colorized icon for use in entity type shape headers.
        /// High-res 128x128 source images represent 16x16 logical pixels (8x scale).
        /// The icon is colorized to the specified foreground color (to match header text)
        /// and DPI metadata is set so the system renders it at the correct size.
        /// </summary>
        /// <param name="bitmap">The source bitmap (128x128 high-res representing 16x16 logical).</param>
        /// <param name="foregroundColor">The color to apply (typically the header text color).</param>
        /// <returns>A colorized bitmap with correct DPI metadata.</returns>
        public static Bitmap GetColorizedHeaderIcon(Bitmap bitmap, Color foregroundColor)
        {
            Debug.Assert(bitmap != null, "bitmap != null");
            var colorizedBitmap = ColorizeIcon(bitmap, foregroundColor);

            // Set resolution so the system knows 128px = 16 logical pixels (8x scale = 768 DPI)
            float scaleFactor = bitmap.Width / 16f;
            colorizedBitmap.SetResolution(96f * scaleFactor, 96f * scaleFactor);

            return colorizedBitmap;
        }
    }
}
