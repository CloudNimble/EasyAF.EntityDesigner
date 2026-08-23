// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    internal partial class VirtualTreeControl
    {
        #region Private Splitter Structures

        private struct VerticalSplitter
        {
            private int myTop;
            private int myBottom;
            private int myCurrentX;
            private int myFrameX;

            public int CurrentX
            {
                get { return myCurrentX; }
            }

            public void Start(int top, int bottom, int startX, int frameX)
            {
                myTop = top;
                myBottom = bottom;
                myCurrentX = startX;
                myFrameX = frameX;
                DrawLines(false, startX);
            }

            public void Move(int currentX)
            {
                if (myCurrentX != currentX)
                {
                    DrawLines(true, currentX);
                }
            }

            public void End()
            {
                DrawLines(false, myCurrentX);
            }

            private void DrawLines(bool drawTwice, int newX)
            {
                ControlPaint.DrawReversibleLine(new Point(myCurrentX, myTop), new Point(myCurrentX, myBottom), SystemColors.Control);
                myCurrentX = newX;
                if (drawTwice)
                {
                    ControlPaint.DrawReversibleLine(new Point(myCurrentX, myTop), new Point(myCurrentX, myBottom), SystemColors.Control);
                }
                else
                {
                    ControlPaint.DrawReversibleLine(new Point(myFrameX, myTop), new Point(myFrameX, myBottom), SystemColors.Control);
                }
            }
        }

        #endregion // Private Splitter Structures

        #region ColumnHeaderBounds Structure

        /// <summary>
        ///     A structure used to determine when a ColumnHeader switches from
        ///     using its percentage to its minimum width. The threshold where
        ///     a given column switches to a fixed-width is calculated and cached
        ///     to make it easy to determine if a column is using its fixed or
        ///     percentage width for a given control width.
        /// </summary>
        private struct ColumnHeaderBounds
        {
            #region ColumnHeaderBound Structure and comparer class

            /// <summary>
            ///     A structure used to hold the boundary size for a given index.
            ///     An array of these structures is kept in sorted order, with the
            ///     highest threshold going first.
            /// </summary>
            private struct ColumnHeaderBound
            {
                public ColumnHeaderBound(int index, float threshold, float percentage)
                {
                    HeaderIndex = index;
                    FixedThreshold = threshold;
                    IncrementalPercentage = percentage;
                }

                /// <summary>
                ///     An index in the header array
                /// </summary>
                public readonly int HeaderIndex;

                /// <summary>
                ///     The threshold where this header switches from percentage
                ///     to fixed width.
                /// </summary>
                public float FixedThreshold;

                /// <summary>
                ///     The incremental percentage of this item. This compares
                ///     to the Percentage from the column headers, which are
                ///     an ascending sequence (0&lt;percent&lt;=1).
                /// </summary>
                public readonly float IncrementalPercentage;
            }

            private class ColumnHeaderBoundComparer : IComparer<ColumnHeaderBound>
            {
                #region IComparer<ColumnHeaderBound> Members

                int IComparer<ColumnHeaderBound>.Compare(ColumnHeaderBound x, ColumnHeaderBound y)
                {
                    var xBound = x.FixedThreshold;
                    var yBound = y.FixedThreshold;
                    // Sort descending to match the algorithm
                    if (xBound < yBound)
                    {
                        return 1;
                    }
                    else if (xBound > yBound)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                #endregion // IComparer<ColumnHeaderBound> Members
            }

            #endregion // ColumnHeaderBound Structure and comparer class

            #region ColumnHeaderBounds Member Variables

            /// <summary>
            ///     The pixel count of all of the fixed width items.
            /// </summary>
            private int myTotalFixedWidth;

            private readonly ColumnHeaderBound[] myVariableBounds;
            private readonly VirtualTreeColumnHeader[] myHeaders;

            private readonly float[] myColumnWidths;
                                     // Column widths are frequently retrieved. It is well worth the trouble to cache the widths for a given control width

            private int myAdjustingColumn;
            private int myAdjustMaxX;
            private int myAdjustMinX;
            private int myAdjustMouseX;
            private static IComparer<ColumnHeaderBound> myComparer;

            #endregion // ColumnHeaderBounds Member Variables

            #region ColumnHeaderBounds Constructors and Methods

            public ColumnHeaderBounds(VirtualTreeColumnHeader[] headers)
            {
                Debug.Assert(headers != null && headers.Length > 0, "headers != null && headers.Length > 0");
                myHeaders = headers;
                var headersCount = headers.Length;
                var fixedWidth = 0;
                var variableCount = 0;
                for (var i = 0; i < headersCount; ++i)
                {
                    if (headers[i].Percentage == 0f)
                    {
                        fixedWidth += headers[i].Width;
                    }
                    else
                    {
                        ++variableCount;
                    }
                }
                myTotalFixedWidth = fixedWidth;
                if (variableCount == 0)
                {
                    myVariableBounds = null;
                    myColumnWidths = null; // Get same information straight from headers
                }
                else
                {
                    myVariableBounds = new ColumnHeaderBound[variableCount];
                    myColumnWidths = new float[headersCount];
                }
                myAdjustingColumn = myAdjustMaxX = myAdjustMinX = myAdjustMouseX = 0;
                Recalculate();
            }

            /// <summary>
            ///     A internal object used for synchronization.
            /// </summary>
            private static object internalSyncObject;

            /// <summary>
            ///     Gets the internal object used for synchronization.
            /// </summary>
            private static object InternalSyncObject
            {
                get
                {
                    if (internalSyncObject == null)
                    {
                        var o = new object();
                        Interlocked.CompareExchange(ref internalSyncObject, o, null);
                    }
                    return internalSyncObject;
                }
            }

            /// <summary>
            ///     Recalculate the bounds
            /// </summary>
            public void Recalculate()
            {
                if (myVariableBounds != null)
                {
                    var headersCount = myHeaders.Length;
                    var variableCount = myVariableBounds.Length;
                    var curBound = 0;
                    VirtualTreeColumnHeader header;
                    ColumnHeaderBound bound;
                    float incrementalPercentage;
                    var lastVariableHeader = -1;
                    for (var i = 0; i < headersCount; ++i)
                    {
                        header = myHeaders[i];
                        if (header.Percentage != 0f)
                        {
                            incrementalPercentage = header.Percentage;
                            if (lastVariableHeader != -1)
                            {
                                incrementalPercentage -= myHeaders[lastVariableHeader].Percentage;
                            }
                            myVariableBounds[curBound] = new ColumnHeaderBound(
                                i, header.Width / incrementalPercentage, incrementalPercentage);
                            ++curBound;
                            if (curBound == variableCount)
                            {
                                // We have them all
                                break;
                            }
                            lastVariableHeader = i;
                        }
                    }

                    // Sort the array in descending order. This gives us an
                    // array where the first item is the first to transition
                    // to fixed width as the control width gets smaller, the
                    // second item is the next to transition, etc. 
                    if (myComparer == null)
                    {
                        lock (InternalSyncObject)
                        {
                            myComparer ??= new ColumnHeaderBoundComparer();
                        }
                    }
                    Array.Sort(myVariableBounds, myComparer);

                    // We now need to adjust FixedThreshold for the
                    // items after the first because the percentage
                    // values for the leading items are no longer valid
                    // for the trailing items. The formula looks like this
                    // (min is the width from the header, and pcnt is the
                    // incremental percentage for the item):
                    //
                    // newThreshold(n) = min(n)/(pcnt(n)/(1 - sum(i=0 to n-1,pcnt(i))) + sum(i=0 to n-1,min(i))
                    //
                    if (variableCount > 1)
                    {
                        bound = myVariableBounds[0];
                        var totalMin = myHeaders[bound.HeaderIndex].Width;
                        var percentageAdjust = 1f - bound.IncrementalPercentage;
                        int currentMin;
                        for (var i = 1; i < variableCount; ++i)
                        {
                            bound = myVariableBounds[i];
                            currentMin = myHeaders[bound.HeaderIndex].Width;
                            myVariableBounds[i].FixedThreshold = totalMin + currentMin * percentageAdjust / bound.IncrementalPercentage;
                            percentageAdjust -= bound.IncrementalPercentage;
                            totalMin += currentMin;
                        }
                    }

                    // Flag widths as dirty
                    myColumnWidths[0] = -1;
                }
            }

            private void EnsureColumnWidths(int controlWidth)
            {
                Debug.Assert(myVariableBounds != null && myColumnWidths != null, "myVariableBounds != null && myColumnWidths != null");
                if (myColumnWidths[0] == -1) // Dirty flag, recalculate
                {
                    // Figure out which variable items are below their minimum and
                    // flag these to use the Width instead of the percentage. We also
                    // need to get the total percentage that is not being used to
                    // correctly adjust the other items.
                    var columnCount = myColumnWidths.Length;
                    var variableCount = myVariableBounds.Length;
                    Array.Clear(myColumnWidths, 0, columnCount); // Zero the array
                    var basePercentage = 1f;
                    var percentageWidth = controlWidth - myTotalFixedWidth;
                    var startPercentageWidth = percentageWidth;
                    int currentWidth;
                    int i;
                    for (i = 0; i < variableCount; ++i)
                    {
                        var bound = myVariableBounds[i];
                        if (startPercentageWidth > bound.FixedThreshold)
                        {
                            break;
                        }
                        basePercentage -= bound.IncrementalPercentage;
                        currentWidth = myHeaders[bound.HeaderIndex].Width;
                        percentageWidth -= currentWidth;
                        myColumnWidths[bound.HeaderIndex] = currentWidth; // Fill in now, signal for next loop
                    }

                    if (i == variableCount)
                    {
                        // We've filled in the widths for all of the variable
                        // items and there is no need to adjust the width of
                        // the last column for roundoff error. If there are
                        // fixed columns, then locate them and fill in their
                        // widths. Otherwise, we're done.
                        if (columnCount > variableCount)
                        {
                            for (i = 0; i < columnCount; ++i)
                            {
                                if (myColumnWidths[i] == 0)
                                {
                                    myColumnWidths[i] = myHeaders[i].Width;
                                }
                            }
                        }
                    }
                    else
                    {
                        var prevPercentage = 0f;
                        float totalWidth = 0;
                        for (i = 0; i < columnCount; ++i)
                        {
                            if (myColumnWidths[i] == 0)
                            {
                                var header = myHeaders[i];
                                if (header.Percentage == 0f)
                                {
                                    myColumnWidths[i] = header.Width;
                                }
                                else
                                {
                                    myColumnWidths[i] = ((header.Percentage - prevPercentage) / basePercentage) * percentageWidth;
                                    prevPercentage = header.Percentage;
                                }
                            }
                            else
                            {
                                prevPercentage = myHeaders[i].Percentage;
                                Debug.Assert(prevPercentage != 0f, "prevPercentage != 0f");
                            }
                            totalWidth += myColumnWidths[i];
                        }
                    }
                }
            }

            /// <summary>
            ///     Attempt to set the column to the given size. If this is a fixed width
            ///     adjustable column then the adjustment is straightforward and always gives
            ///     the full size. If this is a proportional column, then an attempt is made
            ///     to set the percentage high enough to get the full width. The percentage
            ///     is taken proportionally from all other proportional columns.
            /// </summary>
            /// <param name="control">The control we're adjusting, pass in to allow calculations to be performed only when needed</param>
            /// <param name="displayColumn">The column to adjust</param>
            /// <param name="requestedWidth">The width to set it to</param>
            /// <returns>True if a column width changed</returns>
            public bool RequestSetColumnWidth(VirtualTreeControl control, int displayColumn, int requestedWidth)
            {
                if (myHeaders == null)
                {
                    return false;
                }
                var retVal = false;
                var header = myHeaders[displayColumn];
                if (AllowColumnAdjustment(displayColumn)
                    && header.IsColumnAdjustable)
                {
                    if (header.Percentage == 0f)
                    {
                        if (requestedWidth < VirtualTreeColumnHeader.MinimumPixelWidth)
                        {
                            requestedWidth = VirtualTreeColumnHeader.MinimumPixelWidth;
                        }
                        var widthDelta = requestedWidth - header.Width;
                        if (widthDelta != 0)
                        {
                            myTotalFixedWidth += widthDelta;
                            myHeaders[displayColumn].SetWidth(requestedWidth);
                            Recalculate();
                            retVal = true;
                        }
                    }
                    else
                    {
                        Rectangle itemRect = new Rectangle(0, 0, 0, 0);
                        control.LimitRectToColumn(displayColumn, ref itemRect, false, -1, true);
                        BeginColumnAdjustment(displayColumn, itemRect.Left, 0);
                        requestedWidth = LimitColumnAdjustment(itemRect.Left + requestedWidth);
                        if (control.LeftScrollBar
                            && !control.HasVerticalScrollBar)
                        {
                            requestedWidth -= SystemInformation.VerticalScrollBarWidth;
                        }
                        retVal = EndColumnAdjustment(control.FullPercentageHeaderWidth, requestedWidth);
                    }
                    if (retVal)
                    {
                        control.SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, true);
                    }
                }
                return retVal;
            }

            /// <summary>
            ///     Get the total width required to display the headers
            ///     for the given control width. If this returns the input
            ///     value, then no scrollbar is required and there is no trailing
            ///     blank region in the control. A return value greater than the
            ///     input requires a scrollbar, and a value less than the input
            ///     implies a blank region on the control. Note that this method
            ///     generates an internal cache of column widths and must be
            ///     called before getting column bounds.
            /// </summary>
            /// <param name="controlWidth">The current width of the control.</param>
            /// <returns>The total width required to display the headers.</returns>
            public int ChangeControlWidth(int controlWidth)
            {
                if (myHeaders == null)
                {
                    return controlWidth;
                }
                else if (myVariableBounds != null)
                {
                    // Signal to recalculate widths
                    myColumnWidths[0] = -1f;
                    return (int)(CalculateNonFixedControlWidth(controlWidth) + .5f);
                }
                else
                {
                    return myTotalFixedWidth;
                }
            }

            /// <summary>
            ///     Helper function to get widths without clearing current column widths
            /// </summary>
            /// <param name="controlWidth">The current width of the control.</param>
            /// <returns>The total width required to display the headers.</returns>
            private float CalculateNonFixedControlWidth(int controlWidth)
            {
                Debug.Assert(myVariableBounds != null, "myVariableBounds != null"); // Don't call otherwise
                var boundsCount = myVariableBounds.Length;
                var testWidth = controlWidth - myTotalFixedWidth;
                if (testWidth <= myVariableBounds[boundsCount - 1].FixedThreshold)
                {
                    // A scrollbar is required once all of the variable items hit their
                    // minimum values. The threshold for this is stored in the final item
                    return myTotalFixedWidth + myVariableBounds[boundsCount - 1].FixedThreshold;
                }

                // Everything fits in the area provided.
                return controlWidth;
            }

            /// <summary>
            ///     Get the left and width for the expanded column. Numbers returned
            ///     here are based solely on the header information, they do not account
            ///     for the horizontal scrollbar position or the vertical scrollbar visibility.
            /// </summary>
            /// <param name="controlWidth">The current width of the control.</param>
            /// <param name="leftColumn">The leftmost column</param>
            /// <param name="rightColumn">The rightmost column</param>
            /// <param name="itemLeft">The left edge of this column</param>
            /// <param name="itemWidth">The width of this column</param>
            public void GetColumnBounds(int controlWidth, int leftColumn, int rightColumn, out int itemLeft, out int itemWidth)
            {
                var leftTotal = 0;
                var widthTotal = 0;
                if (myVariableBounds != null)
                {
                    GetVariableColumnBounds(controlWidth, leftColumn, rightColumn, out float floatLeft, out float floatRight);
                    if (leftColumn > 0)
                    {
                        leftTotal = (int)(floatLeft + .5f);
                    }
                    widthTotal = (int)(floatRight + .5f) - leftTotal;
                }
                else
                {
                    for (var i = 0; i < leftColumn; ++i)
                    {
                        leftTotal += myHeaders[i].Width;
                    }
                    for (var i = leftColumn; i <= rightColumn; ++i)
                    {
                        widthTotal += myHeaders[i].Width;
                    }
                }
                itemLeft = leftTotal;
                itemWidth = widthTotal;
            }

            private void GetVariableColumnBounds(int controlWidth, int leftColumn, int rightColumn, out float itemLeft, out float itemRight)
            {
                Debug.Assert(myVariableBounds != null, "myVariableBounds != null");
                float leftTotal = 0;
                float rightTotal = 0;

                // See if any of our columns have moved below the minimal width
                EnsureColumnWidths(controlWidth);

                for (var i = 0; i < leftColumn; ++i)
                {
                    leftTotal += myColumnWidths[i];
                }
                rightTotal = leftTotal;
                for (var i = leftColumn; i <= rightColumn; ++i)
                {
                    rightTotal += myColumnWidths[i];
                }
                itemLeft = leftTotal;
                itemRight = rightTotal;
            }

            /// <summary>
            ///     Determine the column for a given hit point, as well as the
            ///     left and width of the column.
            /// </summary>
            /// <param name="controlWidth">The current width of the control.</param>
            /// <param name="x">The x offset into row</param>
            /// <param name="itemLeft">The left edge of this column</param>
            /// <param name="itemWidth">The width of this column</param>
            /// <returns>The column x is in</returns>
            public int ColumnHitTest(int controlWidth, int x, out int itemLeft, out int itemWidth)
            {
                var column = -1;
                itemLeft = 0;
                itemWidth = 0;
                var leftTotal = 0;
                var testWidth = 0;
                var headerCount = myHeaders.Length;
                if (myVariableBounds != null)
                {
                    // See if any of our columns have moved below the minimal width
                    EnsureColumnWidths(controlWidth);
                    var floatTotal = 0f;
                    var prevFloatTotal = 0f;

                    for (var i = 0; i < headerCount; ++i)
                    {
                        // Keep this consistent with GetVariableColumnBounds/GetColumnBounds
                        prevFloatTotal = floatTotal;
                        floatTotal += myColumnWidths[i];
                        testWidth = (int)(floatTotal + .5f);
                        if (x < testWidth)
                        {
                            column = i;
                            itemLeft = (int)(prevFloatTotal + .5f);
                            itemWidth = testWidth - itemLeft;
                            break;
                        }
                    }
                    if (column == -1)
                    {
                        column = headerCount - 1;
                        itemLeft = (int)(prevFloatTotal + .5f);
                        itemWidth = controlWidth - itemLeft;
                    }
                }
                else
                {
                    for (var i = 0; i < headerCount; ++i)
                    {
                        testWidth = myHeaders[i].Width;
                        if (x < testWidth)
                        {
                            column = i;
                            itemWidth = testWidth;
                            itemLeft = leftTotal;
                            break;
                        }
                        leftTotal += testWidth;
                        x -= testWidth;
                    }
                    if (column == -1)
                    {
                        column = headerCount - 1;
                        itemLeft = leftTotal - testWidth;
                        itemWidth = controlWidth - itemLeft;
                    }
                }
                return column;
            }

            /// <summary>
            ///     See if a given column width can be adjusted.
            /// </summary>
            /// <param name="adjustingColumn">The column to be adjusted</param>
            /// <returns>True if BeginColumnAdjustment should be called</returns>
            public bool AllowColumnAdjustment(int adjustingColumn)
            {
                return !HasVariableBounds || (adjustingColumn < (HeaderCount - 1));
            }

            /// <summary>
            ///     Beging a column adjustment. This function must be called
            ///     to get the property limits set.
            /// </summary>
            /// <param name="adjustingColumn">
            ///     The column that is being adjusted. Validate with
            ///     AllowColumnAdjustment before calling this
            /// </param>
            /// <param name="columnLeft">
            ///     The left edge of this column, adjusted by any
            ///     horizontal scrollbar offset. Should have been retrieved previously with
            ///     VirtualTreeControl.LimitRectToColumn.
            /// </param>
            /// <param name="adjustMouseX">
            ///     The difference between the mouse x position
            ///     and the right edge of the column. Tracking lines are shown at the true
            ///     edge, which is not directly under the mouse.
            /// </param>
            public void BeginColumnAdjustment(int adjustingColumn, int columnLeft, int adjustMouseX)
            {
                Debug.Assert(AllowColumnAdjustment(adjustingColumn), "AllowColumnAdjustment(adjustingColumn)");
                myAdjustingColumn = adjustingColumn;
                myAdjustMouseX = adjustMouseX;
                var header = myHeaders[adjustingColumn];
                if (header.Percentage == 0f)
                {
                    // Fixed size column, do not limit size we can adjust unless it is
                    // nonadjustable, in which case we lock it in place. Limit lower by minimum pixel width;
                    if (!header.IsColumnAdjustable)
                    {
                        Debug.Assert(header.Percentage == 0f, "header.Percentage == 0f"); // Percentage-based columns are always adjustable
                        myAdjustMinX = myAdjustMaxX = columnLeft + header.Width;
                    }
                    else
                    {
                        myAdjustMinX = columnLeft + VirtualTreeColumnHeader.MinimumPixelWidth;
                        myAdjustMaxX = int.MaxValue;
                    }
                }
                else
                {
                    Debug.Assert(myVariableBounds != null, "myVariableBounds != null");
                    Debug.Assert(myColumnWidths[0] != -1, "myColumnWidths[0] != -1"); // VirtualTreeControl.LimitRectToColumn calls GetColumnBounds, which sets this
                    // Don't size smaller than the minimum 
                    myAdjustMinX = columnLeft + header.Width;
                    var allowedTrailingAdjustment = myColumnWidths[adjustingColumn];
                    var headersCount = HeaderCount;
                    for (var i = adjustingColumn + 1; i < headersCount; ++i)
                    {
                        allowedTrailingAdjustment += myColumnWidths[i] - myHeaders[i].Width;
                            // This will be zero for fixed size, not worth checking
                    }
                    myAdjustMaxX = columnLeft + (int)(allowedTrailingAdjustment + .5f);
                }
            }

            /// <summary>
            ///     Finish a column adjustment begun with EndColumnAdjustment
            /// </summary>
            /// <param name="controlWidth">The adjusted control width (FullPercentageHeaderWidth on the control)</param>
            /// <param name="dropPosition">The drop position, preadjusted for scroll position and leftscrollbar, and limited with LimitColumnAdjustment</param>
            /// <returns>True if the headers changed.</returns>
            public bool EndColumnAdjustment(int controlWidth, int dropPosition)
            {
                // The dropPosition will already have been adjusted with LimitColumnAdjustment,
                // so we don't touch adjustMouseX here.

                // Find the new width for the item. This is easy for fixed-size columns, but involves
                // stealing equitable amounts from all trailing variable-width columns for non-fixed size
                if (myHeaders[myAdjustingColumn].Percentage == 0f)
                {
                    GetColumnBounds(controlWidth, myAdjustingColumn, myAdjustingColumn, out int itemLeft, out int itemWidth);
                    var widthDelta = dropPosition - (itemLeft + itemWidth);
                    if (widthDelta == 0)
                    {
                        return false;
                    }
                    myTotalFixedWidth += widthDelta;
                    myHeaders[myAdjustingColumn].SetWidth(itemWidth + widthDelta);
                }
                else
                {
                    GetVariableColumnBounds(controlWidth, myAdjustingColumn, myAdjustingColumn, out float itemLeft, out float itemRight);
                    if (dropPosition == (int)(itemRight + .5f))
                    {
                        return false;
                    }

                    // First get the percentage change of the column that is
                    // being adjusted then share that percentage change with all
                    // of the trailing variable columns. Share regardless of whether
                    // they are at a minimum size or not.
                    var adjustingColumn = myAdjustingColumn;
                    var prevVariableColumn = adjustingColumn;
                    var prevPercent = myHeaders[adjustingColumn].Percentage;
                    var adjustColumnPercentChange = (dropPosition - itemRight)
                                                    / (CalculateNonFixedControlWidth(controlWidth) - myTotalFixedWidth);
                    var newPrevPercent = prevPercent + adjustColumnPercentChange;
                    myHeaders[adjustingColumn].SetPercentage(newPrevPercent);
                    var basePercentage = (1f - prevPercent) / (1f - newPrevPercent);
                    var columns = myHeaders.Length;
                    float percent;
                    for (var i = adjustingColumn + 1; i < columns; ++i)
                    {
                        percent = myHeaders[i].Percentage;
                        if (percent != 0f)
                        {
                            newPrevPercent += ((percent - prevPercent) / basePercentage);
                            myHeaders[i].SetPercentage(newPrevPercent);
                            prevPercent = percent;
                            prevVariableColumn = i;
                        }
                    }
                    if (prevVariableColumn == adjustingColumn)
                    {
                        // Don't do anything. This used to be asserted, but it
                        // is hard to stop the header control from trying this.
                        return false;
                    }
                    // Make sure the last one didn't go too far
                    myHeaders[prevVariableColumn].SetPercentage(1f);
                }
                Recalculate();
                return true;
            }

            /// <summary>
            ///     Limit the drop position to the allowed drop range for the given item.
            ///     Any call to EndColumnAdjustment assumes that the value has been
            ///     correctly limited with this call.
            /// </summary>
            /// <param name="dropPosition">
            ///     The drop position, in whatever coordinates
            ///     were passed to BeginColumnAdjustment (generally global screen coordinates)
            /// </param>
            /// <returns>The limit value</returns>
            public int LimitColumnAdjustment(int dropPosition)
            {
                dropPosition += myAdjustMouseX;
                if (dropPosition > myAdjustMaxX)
                {
                    dropPosition = myAdjustMaxX;
                }
                else if (dropPosition < myAdjustMinX)
                {
                    dropPosition = myAdjustMinX;
                }
                return dropPosition;
            }

            /// <summary>
            ///     Limit the range of motion for the mouse.
            /// </summary>
            /// <param name="mousePosition">The current mouse position in screen coordinates</param>
            /// <returns>The adjusted position</returns>
            public Point LimitMousePosition(Point mousePosition)
            {
                mousePosition.X = LimitColumnAdjustment(mousePosition.X) - myAdjustMouseX;
                return mousePosition;
            }

            /// <summary>
            ///     Move changes from the current (permuted) header back into the full header array
            /// </summary>
            /// <param name="permutation">The current column permutation</param>
            /// <param name="autoFillApplied">true if the current headers are variable size because the AutoFillFixedColumns property is true</param>
            /// <param name="fullHeaders">The full header array</param>
            public void ReverseIntegratePermutedSettings(
                ColumnPermutation permutation, bool autoFillApplied, VirtualTreeColumnHeader[] fullHeaders)
            {
                if ((permutation == null && !autoFillApplied)
                    || ReferenceEquals(myHeaders, fullHeaders))
                {
                    return;
                }
                var headersCount = myHeaders.Length;

                if (HasVariableBounds && !autoFillApplied)
                {
                    // This is the complicated case. We need to get the current percentages of
                    // the visible proportional columns from the full headers and then use the sum
                    // of those as the base percentage for integrating the other items back in.
                    // To facilitate this effort, we also switch the full header percentages to
                    // an incremental percentage instead of an ascending percentage and then switch
                    // them back later on.

                    // Switch the full headers to incremental. Update the fixed width columns while
                    // we're going through the loop.
                    var fullCount = fullHeaders.Length;
                    var lastFullPercentage = 0f;
                    var lastPercentageColumn = -1;
                    var basePercentage = 0f;
                    int i;
                    int permutedColumn;
                    float currentPercentage;
                    float incrementalPercentage;
                    for (i = 0; i < fullCount; ++i)
                    {
                        currentPercentage = fullHeaders[i].Percentage;
                        if (currentPercentage != 0f)
                        {
                            incrementalPercentage = currentPercentage - lastFullPercentage;
                            if (permutation.GetPermutedColumn(i) != -1)
                            {
                                basePercentage += incrementalPercentage;
                            }
                            fullHeaders[i].SetPercentage(incrementalPercentage);
                            lastFullPercentage = currentPercentage;
                            lastPercentageColumn = i;
                        }
                        else
                        {
                            // Fixed width column, need to update at some point, might as well do it now
                            permutedColumn = permutation.GetPermutedColumn(i);
                            if (permutedColumn != -1)
                            {
                                fullHeaders[i].SetWidth(myHeaders[permutedColumn].Width);
                            }
                        }
                    }

                    // Integrate the new settings into the full headers
                    if (basePercentage != 0f)
                    {
                        for (i = 0; i <= lastPercentageColumn; ++i)
                        {
                            if (fullHeaders[i].Percentage != 0f)
                            {
                                // See if the column is displayed
                                permutedColumn = permutation.GetPermutedColumn(i);
                                if (permutedColumn != -1)
                                {
                                    // Grab the current percentage
                                    currentPercentage = myHeaders[permutedColumn].Percentage;

                                    // Find the previous proportional column, if any.
                                    lastFullPercentage = 0f;
                                    for (var j = permutedColumn - 1; j >= 0; --j)
                                    {
                                        lastFullPercentage = myHeaders[j].Percentage;
                                        if (lastFullPercentage != 0f)
                                        {
                                            break;
                                        }
                                    }

                                    // Set the new incremental percentage in the full headers array
                                    fullHeaders[i].SetPercentage((currentPercentage - lastFullPercentage) * basePercentage);
                                }
                            }
                        }
                    }

                    // Switch the full headers back to ascending percentages
                    // The header percentages now contain an incremental percentage, not the total
                    // percentage they should hold. Walk down the list and fix this.
                    if (lastPercentageColumn != -1)
                    {
                        var newPercentage = 0f;
                        for (i = 0; i < lastPercentageColumn; ++i)
                        {
                            currentPercentage = fullHeaders[i].Percentage;
                            if (currentPercentage != 0f)
                            {
                                newPercentage += currentPercentage;
                                fullHeaders[i].SetPercentage(newPercentage);
                            }
                        }
                        fullHeaders[lastPercentageColumn].SetPercentage(1f);
                    }
                }
                else
                {
                    for (var i = 0; i < headersCount; ++i)
                    {
                        // Just set all of them. This will be a noop for the proportional columns,
                        // it isn't worth detecting the case.
                        fullHeaders[permutation.GetNativeColumn(i)].SetWidth(myHeaders[i].Width);
                    }
                }
            }

            #endregion // ColumnHeaderBounds Constructors and Methods

            #region ColumnHeaderBounds Accessor Properties

            /// <summary>
            ///     The headers associated with this column bound
            /// </summary>
            public VirtualTreeColumnHeader[] Headers
            {
                get { return myHeaders; }
            }

            /// <summary>
            ///     The number of displayed column headers
            /// </summary>
            public int HeaderCount
            {
                get { return (myHeaders != null) ? myHeaders.Length : 0; }
            }

            /// <summary>
            ///     Returns true if column headers are set.
            /// </summary>
            public bool HasHeaders
            {
                get { return myHeaders != null; }
            }

            /// <summary>
            ///     True if there is at least one percentage-based column in
            ///     the set of columns. If this is false, then there is
            ///     never any blank space in the grid.
            /// </summary>
            public bool HasVariableBounds
            {
                get { return myVariableBounds != null; }
            }

            #endregion // ColumnHeaderBounds Accessor Properties
        }

        #endregion // ColumnHeaderBounds Structure

        #region Column Header Member Variables

        private ColumnHeaderBounds myHeaderBounds;
        private VirtualTreeColumnHeader[] myFullHeaders; // The full set of headers, includes all columns
        private ColumnPermutation myColumnPermutation;
        private int myBorderOffset = -1;
        private VerticalSplitter mySplitData = new VerticalSplitter();

        #endregion // Column Header Member Variables

        #region Column Permutation Integration

        /// <summary>
        ///     The ColumnPermutation object for this control. A ColumnPermutation can
        ///     be used to reorder and hide different columns. The object is live and is not
        ///     evented, so it should not be shared across control instances. If HeaderDragDrop is
        ///     set, a ColumnPermutation object will be generated automatically if the user drags
        ///     column headers to reposition them.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        public ColumnPermutation ColumnPermutation
        {
            get { return myColumnPermutation; }
            set
            {
                if (value != myColumnPermutation)
                {
                    var oldPermutation = myColumnPermutation;
                    myColumnPermutation = value;
                    PermuteHeaders(oldPermutation, value, false, -1);
                    if (IsHandleCreated)
                    {
                        AfterHeaderSizeChanged();
                    }
                }
            }
        }

        /// <summary>
        ///     Update the myHeaders and myFullHeaders values to match the
        ///     given permutations.
        /// </summary>
        /// <param name="oldPermutation">
        ///     If this is set, then the relative sizes
        ///     of the current myHeaders will be reintegrated into myFullHeaders
        /// </param>
        /// <param name="newPermutation">The new permutation to apply</param>
        /// <param name="autoFillChange">AutoFill is being turned on or off</param>
        /// <param name="oldSelectionColumn">
        ///     The prior native selection column, or -1 if none.
        ///     Ignored if oldPermutation is set.
        /// </param>
        private void PermuteHeaders(
            ColumnPermutation oldPermutation, ColumnPermutation newPermutation, bool autoFillChange, int oldSelectionColumn)
        {
            var newSelectionColumn = mySelectionColumn;
            var resetSelectionColumn = IsHandleCreated;
            var autoFillColumn = -2; // Special value to enable delayed calculation
            var autoFillApplied = GetStateFlag(VTCStateFlags.FixedColumnAutoFilled);
            if (oldPermutation != null)
            {
                newSelectionColumn = oldPermutation.GetNativeColumn(newSelectionColumn);
            }
            else if (oldSelectionColumn != -1)
            {
                newSelectionColumn = oldSelectionColumn;
            }
            if ((oldPermutation != null || autoFillApplied)
                && myFullHeaders != null
                && GetStateFlag(VTCStateFlags.FullColumnHeadersDirty))
            {
                myHeaderBounds.ReverseIntegratePermutedSettings(oldPermutation, autoFillApplied, myFullHeaders);
            }
            SetStateFlag(VTCStateFlags.FixedColumnAutoFilled, false);

            // Apply the new permutation, if specified
            if (newPermutation == null
                && (!AutoFillFixedColumns || (-1 == (autoFillColumn = FindAutoFillIndex(myFullHeaders)))))
            {
                SetHeaders(myFullHeaders);
                if (resetSelectionColumn && newSelectionColumn != mySelectionColumn)
                {
                    SetSelectionColumn(newSelectionColumn, false);
                }
            }
            else if (myFullHeaders == null)
            {
                SetHeaders(null);
                if (resetSelectionColumn && newSelectionColumn != mySelectionColumn)
                {
                    SetSelectionColumn(newSelectionColumn, false);
                }
            }
            else if (autoFillColumn >= 0)
            {
                // We have no permutation so the order, percentage, etc are the same. We
                // just need to clone the headers and change the setting on the autofill column.
                VirtualTreeColumnHeader[] newHeaders = (VirtualTreeColumnHeader[])myFullHeaders.Clone();
                newHeaders[autoFillColumn].SetPercentage(1f);
                SetStateFlag(VTCStateFlags.FixedColumnAutoFilled, true);
                SetHeaders(newHeaders);
            }
            else if (autoFillChange)
            {
                // No need to reapply all of the permutation pieces to turn the autofill on/off.
                // This updates the current headers inline.
                var currentHeaders = myHeaderBounds.Headers;
                if (autoFillApplied)
                {
                    for (var i = currentHeaders.Length - 1; i >= 0; --i)
                    {
                        if (currentHeaders[i].Percentage != 0f)
                        {
                            currentHeaders[i].SetPercentage(0f);
                            break;
                        }
                    }
                }
                if (AutoFillFixedColumns)
                {
                    autoFillColumn = FindAutoFillIndex(currentHeaders);
                    if (autoFillColumn != -1)
                    {
                        currentHeaders[autoFillColumn].SetPercentage(1f);
                        SetStateFlag(VTCStateFlags.FixedColumnAutoFilled, true);
                    }
                }
                if (autoFillApplied || (autoFillColumn >= 0))
                {
                    SetHeaders(currentHeaders);
                }
            }
            else
            {
                Debug.Assert(newPermutation != null, "newPermutation != null"); // Other cases in first if should all have been covered
                int i;
                var visibleColumns = newPermutation.VisibleColumnCount;
                var totalColumns = newPermutation.FullColumnCount;

                // Do this in two steps. The first step gets the percentage for
                // each visible column and the total percentage for the visible columns.
                // The second step reorders these values into a new VirtualTreeColumnHeader array.

                var rawPercentages = new float[visibleColumns];
                var visibleColumnIndices = new int[visibleColumns];
                var totalSkippedPercentage = 0f;
                var currentVisibleColumn = 0;
                var fullHeaders = myFullHeaders;
                var prevPercentageColumn = -1;
                var lastPercentageColumn_Visible = -1;
                float currentPercentage;
                bool fixedSizeColumn;

                // Step 1
                for (i = 0; i < totalColumns; ++i)
                {
                    currentPercentage = fullHeaders[i].Percentage;
                    fixedSizeColumn = currentPercentage == 0f;
                    // There is nothing to do for a fixed-size column
                    if (newPermutation.GetPermutedColumn(i) == -1)
                    {
                        if (!fixedSizeColumn)
                        {
                            totalSkippedPercentage += currentPercentage
                                                      - ((prevPercentageColumn == -1) ? 0f : fullHeaders[prevPercentageColumn].Percentage);
                            prevPercentageColumn = i;
                        }
                    }
                    else
                    {
                        visibleColumnIndices[currentVisibleColumn] = i;
                        if (!fixedSizeColumn)
                        {
                            rawPercentages[currentVisibleColumn] = currentPercentage - totalSkippedPercentage;
                            prevPercentageColumn = i;
                            lastPercentageColumn_Visible = currentVisibleColumn;
                        }
                        ++currentVisibleColumn;
                        Debug.Assert(currentVisibleColumn <= visibleColumns, "currentVisibleColumn <= visibleColumns");
                    }
                }

                // Step 2
                VirtualTreeColumnHeader[] newHeaders = new VirtualTreeColumnHeader[visibleColumns];
                var remainingPercentage = 1f - totalSkippedPercentage;
                int permutedColumn;
                int nativeColumn;
                float newPercentage;
                prevPercentageColumn = -1;
                for (i = 0; i < visibleColumns; ++i)
                {
                    nativeColumn = visibleColumnIndices[i];
                    permutedColumn = newPermutation.GetPermutedColumn(nativeColumn);

                    // Copy the raw information
                    newHeaders[permutedColumn] = fullHeaders[nativeColumn];

                    // Update the percentage
                    currentPercentage = rawPercentages[i];
                    if (currentPercentage != 0f)
                    {
                        if (permutedColumn == lastPercentageColumn_Visible)
                        {
                            newPercentage = 1f;
                        }
                        else
                        {
                            newPercentage = (currentPercentage - ((prevPercentageColumn == -1) ? 0f : rawPercentages[prevPercentageColumn]))
                                            / remainingPercentage;
                        }
                        prevPercentageColumn = i;
                        newHeaders[permutedColumn].SetPercentage(newPercentage);
                    }
                }

                if (lastPercentageColumn_Visible != -1)
                {
                    // The header percentages now contain an incremental percentage, not the total
                    // percentage they should hold. Walk down the list and fix this.
                    newPercentage = 0f;
                    for (i = 0; i < lastPercentageColumn_Visible; ++i)
                    {
                        currentPercentage = newHeaders[i].Percentage;
                        if (currentPercentage != 0f)
                        {
                            newPercentage += currentPercentage;
                            newHeaders[i].SetPercentage(newPercentage);
                        }
                    }
                }
                else if (AutoFillFixedColumns)
                {
                    autoFillColumn = FindAutoFillIndex(newHeaders);
                    if (autoFillColumn != -1)
                    {
                        newHeaders[autoFillColumn].SetPercentage(1f);
                        SetStateFlag(VTCStateFlags.FixedColumnAutoFilled, true);
                    }
                }
                SetHeaders(newHeaders);

                // Reset the selection column
                if (resetSelectionColumn)
                {
                    newSelectionColumn = newPermutation.GetPermutedColumn(newSelectionColumn);
                    if (newSelectionColumn != mySelectionColumn)
                    {
                        if (newSelectionColumn != -1)
                        {
                            SetSelectionColumn(newSelectionColumn, oldPermutation, false);
                        }
                        else
                        {
                            // Fallback on the closest displayed column since the column
                            // we used to be on is no longer visible.
                            var currentRow = CurrentIndex;
                            if (currentRow != -1)
                            {
                                var preferredColumn = mySelectionColumn;
                                if (newPermutation.PreferLeftBlanks
                                    && preferredColumn > 0)
                                {
                                    // This corresponds to RightToLeft == true. Let the left column
                                    // slip into the opened space instead of the right
                                    --preferredColumn;
                                }
                                preferredColumn = Math.Min(preferredColumn, newPermutation.VisibleColumnCount - 1);
                                var blankExpansion = myTree.GetBlankExpansion(currentRow, preferredColumn, newPermutation);
                                if (blankExpansion.AnchorColumn == VirtualTreeConstant.NullIndex)
                                {
                                    // Blank row
                                    mySelectionColumn = 0;
                                }
                                else if (blankExpansion.TopRow != currentRow)
                                {
                                    if (blankExpansion.AnchorColumn != mySelectionColumn
                                        || oldPermutation != null)
                                    {
                                        SetSelectionColumn(blankExpansion.AnchorColumn, oldPermutation, false);
                                    }
                                    CurrentIndex = blankExpansion.TopRow;
                                }
                                else
                                {
                                    SetSelectionColumn(blankExpansion.AnchorColumn, oldPermutation, true);
                                }
                            }
                            else
                            {
                                mySelectionColumn = 0;
                            }
                        }
                    }
                }
            }
            SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, false);
        }

        /// <summary>
        ///     Return a column that can be used to autofill fixed columns
        /// </summary>
        /// <param name="headers">A header array</param>
        /// <returns>-1 if autofill is not needed (there are variable columns), or if all columns are nonadjustable</returns>
        private int FindAutoFillIndex(VirtualTreeColumnHeader[] headers)
        {
            if (headers == null)
            {
                return -1;
            }
            var count = headers.Length;
            int start;
            int end;
            int incr;
            if (RightToLeft == RightToLeft.Yes)
            {
                // Search from the left
                start = 0;
                end = count;
                incr = 1;
            }
            else
            {
                // Search from the right
                start = count - 1;
                end = -1;
                incr = -1;
            }
            for (var i = start; i != end; i += incr)
            {
                if (headers[i].Percentage == 0f)
                {
                    if (headers[i].IsColumnAdjustable)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void SetHeaders(VirtualTreeColumnHeader[] headers)
        {
            if (headers == null)
            {
                myHeaderBounds = new ColumnHeaderBounds();
            }
            else
            {
                myHeaderBounds = new ColumnHeaderBounds(headers);
                if (myHeaderContainer != null)
                {
                    if (GetStateFlag(VTCStateFlags.ChangingColumnOrder))
                    {
                        if (GetStateFlag(VTCStateFlags.UpdateHeaderControl))
                        {
                            UpdateHeaderControlWidths(myHeaderContainer.HeaderControl);
                        }
                    }
                    else
                    {
                        PopulateHeaderControl(myHeaderContainer.HeaderControl);
                    }
                }
            }
            if (IsHandleCreated)
            {
                Refresh();
            }
        }

        #endregion // Column Permutation Integration

        #region Column Header Routines

        private void ValidateHeadersAfterTreeChange()
        {
            if (myTree != null)
            {
                var permuteAndRedraw = false;
                var expectedColumns = (myMctree == null) ? 1 : myMctree.ColumnCount;
                if (myColumnPermutation != null)
                {
                    if (myColumnPermutation.FullColumnCount != expectedColumns)
                    {
                        myColumnPermutation = null;
                        permuteAndRedraw = true;
                    }
                }
                if (myFullHeaders != null)
                {
                    Debug.Assert(myFullHeaders.Length != 0, "myFullHeaders.Length != 0");
                    if (expectedColumns != 0
                        && myFullHeaders.Length != expectedColumns)
                    {
                        myFullHeaders = null;
                        permuteAndRedraw = true;
                    }
                }

                if (permuteAndRedraw)
                {
                    PermuteHeaders(null, myColumnPermutation, false, -1);
                }
            }
        }

        /// <summary>
        ///     Specify the column headers for this control. Headers are in the order
        ///     of the native columns, not the current ColumnPermutation. The number
        ///     of items in the header must match the number of columns in the current
        ///     tree or multi-column tree.
        /// </summary>
        /// <param name="headers">An array of headers, with ascending perctages as specified in the VirtualTreeColumnHeader documentation.</param>
        /// <param name="calculatePercentage">true if the percentages for non-fixed width columns should be divided evenly among all proportional headers.</param>
        public void SetColumnHeaders(VirtualTreeColumnHeader[] headers, bool calculatePercentage)
        {
            if (headers == null
                || headers.Length == 0)
            {
                if (myFullHeaders != null)
                {
                    myFullHeaders = null;
                    PermuteHeaders(null, myColumnPermutation, false, -1);
                    RedrawHeaderFrame();
                }
                return;
            }

            var columnCount = (myMctree == null) ? ((myTree == null) ? 0 : 1) : myMctree.ColumnCount;
            if (columnCount == 0)
            {
                columnCount = headers.Length;
            }
            else if (headers.Length != columnCount)
            {
                throw new ArgumentException(VirtualTreeStrings.GetString(VirtualTreeStrings.HeaderArrayException), "headers");
            }

            if (calculatePercentage)
            {
                // First count the non-fixed width columns
                var proportionalCount = 0;
                for (var i = 0; i < columnCount; ++i)
                {
                    if (headers[i].Percentage != 0f)
                    {
                        ++proportionalCount;
                    }
                }
                var percentIncrement = (float)1 / proportionalCount;
                var percent = 1f;
                for (var i = columnCount - 1; i >= 0; --i)
                {
                    if (headers[i].Percentage != 0f)
                    {
                        headers[i].SetPercentage(percent);
                        percent -= percentIncrement;
                    }
                }
            }
            else
            {
                // Having the variable header items be an ascending sequence is critical to
                // all of the header routines. Make sure that this is the case here.
                var prevVariableColumn = -1;
                float percent;
                var prevPercent = 0f;
                for (var i = 0; i < columnCount; ++i)
                {
                    percent = headers[i].Percentage;
                    if (percent != 0)
                    {
                        if (percent <= prevPercent)
                        {
                            throw new ArgumentException(
                                VirtualTreeStrings.GetString(VirtualTreeStrings.PercentageBasedHeadersInvalidException), "headers");
                        }
                        prevPercent = percent;
                        prevVariableColumn = i;
                    }
                }
                if (prevVariableColumn != -1
                    && prevPercent != 1f)
                {
                    throw new ArgumentException(
                        VirtualTreeStrings.GetString(VirtualTreeStrings.PercentageBasedHeadersLastInvalidException), "headers");
                }
            }
            var frameChange = myFullHeaders == null;
            myFullHeaders = headers;
            var oldSelectionColumn = mySelectionColumn;
            if (oldSelectionColumn >= 0
                && myColumnPermutation != null)
            {
                oldSelectionColumn = myColumnPermutation.GetNativeColumn(oldSelectionColumn);
            }
            PermuteHeaders(null, myColumnPermutation, false, oldSelectionColumn);
            if (frameChange && DisplayColumnHeaders)
            {
                // The headers are turn for a created control in on the DisplayColumnHeaders setter. Fake it out
                // by first setting the value to false so we can use this code.
                SetStyleFlag(VTCStyleFlags.DisplayColumnHeaders, false);
                DisplayColumnHeaders = true;
            }
        }

        /// <summary>
        ///     Retrieve the full set of column headers, with the values and percentages
        ///     currently displayed by the control.
        /// </summary>
        /// <returns>A header array, or null if headers are not set.</returns>
        public VirtualTreeColumnHeader[] GetColumnHeaders()
        {
            if (myFullHeaders != null)
            {
                if (GetStateFlag(VTCStateFlags.FullColumnHeadersDirty))
                {
                    myHeaderBounds.ReverseIntegratePermutedSettings(
                        myColumnPermutation, GetStateFlag(VTCStateFlags.FixedColumnAutoFilled), myFullHeaders);
                    SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, false);
                }
                return (VirtualTreeColumnHeader[])myFullHeaders.Clone();
            }
            return null;
        }

        /// <summary>
        ///     Retrieve a single column header.
        /// </summary>
        /// <param name="column">The native column. Use ColumnPermutation.GetNativeColumn if you have a display column.</param>
        /// <returns>A column header. The IsEmpty property of the returned structure will be true if no headers are set.</returns>
        public VirtualTreeColumnHeader GetColumnHeader(int column)
        {
            if (myFullHeaders != null)
            {
                if (GetStateFlag(VTCStateFlags.FullColumnHeadersDirty))
                {
                    myHeaderBounds.ReverseIntegratePermutedSettings(
                        myColumnPermutation, GetStateFlag(VTCStateFlags.FixedColumnAutoFilled), myFullHeaders);
                    SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, false);
                }
                return myFullHeaders[column];
            }
            // Return an empty header
            return new VirtualTreeColumnHeader();
        }

        /// <summary>
        ///     Update the appearance fields of a column header. Does not affect the width or order of the header
        /// </summary>
        /// <param name="column">
        ///     The column to update. This is the native column value, which may not be the same as
        ///     the displayed column position, depending on the current ColumnPermutation.
        /// </param>
        /// <param name="headerText">The string to display in the header</param>
        /// <param name="style">The style to use</param>
        /// <param name="imageIndex">The index in the HeaderImageList property, or -1 for no image</param>
        public void UpdateColumnHeaderAppearance(int column, string headerText, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            if (myFullHeaders != null)
            {
                myFullHeaders[column].SetAppearanceFields(headerText, style, imageIndex);
                if (ReferenceEquals(myHeaderBounds.Headers, myFullHeaders))
                {
                    myHeaderContainer?.HeaderControl.UpdateItemAppearance(myFullHeaders[column], column);
                }
                else
                {
                    var displayColumn = column;
                    if (myColumnPermutation != null)
                    {
                        displayColumn = myColumnPermutation.GetPermutedColumn(column);
                        if (displayColumn == -1)
                        {
                            return;
                        }
                    }
                    myHeaderBounds.Headers[displayColumn].SetAppearanceFields(headerText, style, imageIndex);
                    myHeaderContainer?.HeaderControl.UpdateItemAppearance(myHeaderBounds.Headers[displayColumn], displayColumn);
                }
            }
        }

        /// <summary>
        ///     Display column headers on the control. Defaults to true. The column headers will be
        ///     visible if SetColumnHeaders has been called. If column headers are set and this property
        ///     is not set, then they can be used to modify column layout without a visible header control.
        /// </summary>
        [DefaultValue(true)]
        public bool DisplayColumnHeaders
        {
            get { return GetStyleFlag(VTCStyleFlags.DisplayColumnHeaders); }
            set
            {
                if (DisplayColumnHeaders != value)
                {
                    // Make sure the flag is set before calling EnsureHeaderContainer
                    // or it doesn't do anything.
                    SetStyleFlag(VTCStyleFlags.DisplayColumnHeaders, value);
                    if (IsHandleCreated)
                    {
                        if (myHeaderBounds.HasHeaders)
                        {
                            if (value)
                            {
                                EnsureHeaderContainer();
                                AttachHeaderContainer();
                            }
                            else if (myHeaderContainer != null)
                            {
                                var container = myHeaderContainer;
                                myHeaderContainer = null;
                                try
                                {
                                }
                                finally
                                {
                                    // FxCop game, it wants Dispose in a finally
                                    container.Dispose();
                                }
                            }
                            RedrawHeaderFrame();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Start tracking a column. Used by the header control.
        /// </summary>
        /// <param name="displayColumn">The column being adjusted</param>
        /// <param name="mousePosition">The current position of the mouse, in screen coordinates</param>
        /// <returns>True if tracking is enabled for this column</returns>
        internal bool BeginTrackingSplitter(int displayColumn, Point mousePosition)
        {
            if (!myHeaderBounds.AllowColumnAdjustment(displayColumn))
            {
                return false;
            }
            var x = mousePosition.X;
            // This will give us the backwards shift for the y position, handle with one call
            var splitPoint = PointToClient(new Point(x, 0));
            Rectangle itemRect = new Rectangle(0, 0, 0, 0);
            LimitRectToColumn(displayColumn, ref itemRect, false, -1, true);
            var scrollShift = -myXPos;
            if (scrollShift != 0)
            {
                itemRect.Offset(scrollShift, 0);
            }
            var splitterTop = -splitPoint.Y; // -HeaderHeight;
            var itemLeft = itemRect.Left + x - splitPoint.X;
            mySplitData.Start(
                splitterTop,
                splitterTop + ClientSize.Height, //Height - 2 * BorderOffset,
                itemRect.Right + x - splitPoint.X,
                itemLeft);
            myHeaderBounds.BeginColumnAdjustment(displayColumn, itemLeft, itemRect.Right - splitPoint.X);
            return true;
        }

        /// <summary>
        ///     See VirtualTreeHeaderControl.TrackSplitter
        /// </summary>
        /// <param name="mousePosition"></param>
        internal void TrackSplitter(Point mousePosition)
        {
            // Splitters are moved in screen coordinates, so convert the client to screen coordinates.
            mySplitData.Move(myHeaderBounds.LimitColumnAdjustment(mousePosition.X));
        }

        /// <summary>
        ///     See VirtualTreeHeaderControl.LimitTrackedMousePosition
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <returns></returns>
        internal Point LimitTrackedMousePosition(Point mousePosition)
        {
            return myHeaderBounds.LimitMousePosition(mousePosition);
        }

        /// <summary>
        ///     Raw information coming from the column header
        /// </summary>
        /// <param name="displayColumn">The positional index of the item clicked</param>
        /// <param name="clickStyle">The style of the event</param>
        /// <param name="mousePosition">The mouse position in screen coordinates.</param>
        internal void OnRawColumnHeaderEvent(int displayColumn, VirtualTreeColumnHeaderClickStyle clickStyle, Point mousePosition)
        {
            var nativeColumn = displayColumn;
            if (myColumnPermutation != null)
            {
                nativeColumn = myColumnPermutation.GetNativeColumn(displayColumn);
            }
            OnColumnHeaderClick(
                new VirtualTreeColumnHeaderClickEventArgs(
                    myHeaderContainer?.HeaderControl, clickStyle, myHeaderBounds.Headers[displayColumn],
                    nativeColumn, mousePosition));
        }

        /// <summary>
        ///     Change the visible order to match the new order.
        /// </summary>
        /// <param name="oldOrder">The old order. Acts as a base order to determine new order</param>
        /// <param name="newOrder">The new order. Compare columns to old order to deduce current display order.</param>
        /// <param name="updateHeaderControl">Set to true if the headers should be updated during this function</param>
        /// <returns>true to allow the change, false to block it</returns>
        public bool ChangeColumnOrder(int[] oldOrder, int[] newOrder, bool updateHeaderControl)
        {
            if (!AllowColumnOrderChange(oldOrder, newOrder))
            {
                return false;
            }
            try
            {
                SetStateFlag(VTCStateFlags.UpdateHeaderControl, updateHeaderControl);
                SetStateFlag(VTCStateFlags.ChangingColumnOrder, true);
                // Integrate before doing the new permutation so that we can apply
                // the change inline in the current permutation
                var needOldPermutation = GetStyleFlag(VTCStyleFlags.MultiSelect);
                if (!needOldPermutation
                    && GetStateFlag(VTCStateFlags.FullColumnHeadersDirty))
                {
                    myHeaderBounds.ReverseIntegratePermutedSettings(
                        myColumnPermutation, GetStateFlag(VTCStateFlags.FixedColumnAutoFilled), myFullHeaders);
                    SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, false);
                }
                var oldSelectionColumn = -1;
                ColumnPermutation oldPermutation = null;
                if (myColumnPermutation == null)
                {
                    myColumnPermutation = new ColumnPermutation(newOrder.Length, newOrder, RightToLeft == RightToLeft.Yes);
                }
                else
                {
                    if (needOldPermutation)
                    {
                        // Unfortunately, the oldPermutation is required here to enable cleanly changing selecting
                        // the columns in multiselect mode. I'd prefer not to make the copy, but multiselect selection
                        // state is very hard to propagate without a copy of the old permutation to accurately filter
                        // the selected indices
                        oldPermutation = myColumnPermutation.Clone();
                    }
                    else
                    {
                        // For the single select case, just pass in the oldSelectionColumn
                        oldSelectionColumn = myColumnPermutation.GetNativeColumn(mySelectionColumn);
                    }
                    myColumnPermutation.ChangeVisibleColumnOrder(oldOrder, newOrder);
                }
                PermuteHeaders(oldPermutation, myColumnPermutation, false, oldSelectionColumn);
                Refresh();
            }
            finally
            {
                SetStateFlag(VTCStateFlags.ChangingColumnOrder, false);
            }
            return true;
        }

        /// <summary>
        ///     Test if the new ordering is allowed by the current column header settings
        /// </summary>
        /// <param name="oldOrder">The old order. Acts as a base order to determine new order</param>
        /// <param name="newOrder">The new order. Compare columns to old order to deduce current display order.</param>
        /// <returns>true to allow the change, false to block it</returns>
        protected virtual bool AllowColumnOrderChange(int[] oldOrder, int[] newOrder)
        {
            // See if any of the items we're moving should not be moving
            var itemCount = oldOrder.Length;
            var headers = myHeaderBounds.Headers;
            for (var i = 0; i < itemCount; ++i)
            {
                if (0 != (headers[i].Style & VirtualTreeColumnHeaderStyles.ColumnPositionLocked))
                {
                    if (oldOrder[i] != newOrder[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///     Populate the header control with the current header items
        /// </summary>
        /// <param name="header">The control to populate</param>
        public void PopulateHeaderControl(VirtualTreeHeaderControl header)
        {
            header.Clear();
            var columns = myHeaderBounds.HeaderCount;
            var rect = Rectangle.Empty;
            var headers = myHeaderBounds.Headers;
            for (var i = 0; i < columns; ++i)
            {
                header.AssociatedControl.LimitRectToColumn(i, ref rect, true);
                header.AddItem(headers[i], rect.Width);
            }
        }

        /// <summary>
        ///     Update the widths of the columns in the header control.
        /// </summary>
        /// <param name="header">The control to adjust</param>
        public void UpdateHeaderControlWidths(VirtualTreeHeaderControl header)
        {
            var columns = myHeaderBounds.HeaderCount;
            var rect = Rectangle.Empty;
            for (var i = 0; i < columns; ++i)
            {
                header.AssociatedControl.LimitRectToColumn(i, ref rect, true);
                header.SetItemWidth(i, rect.Width);
            }

            // set the edit size if we're in a label edit, unless we're already processing the edit dismissal.
            if (header.AssociatedControl.InLabelEdit
                && !header.AssociatedControl.GetStateFlag(VTCStateFlags.LabelEditProcessing))
            {
                header.AssociatedControl.SetEditSize();
            }
        }

        /// <summary>
        ///     If all columns are fixed-width, then treat the last adjustable column as percentage
        ///     based to force the headers to span the entire control.
        ///     Explanation: If you use column headers and any of the columns are percentage-based then you
        ///     never have a blank region in the control after the last header. If you choose to
        ///     use fixed-width headers, then you can emulate this behavior by giving the last column
        ///     a reasonable minimum width and a percentage of 100% (1f). This approach breaks down
        ///     as soon as you start reordering or hiding columns because the percentage-based column
        ///     jumps around or disappears.
        /// </summary>
        [DefaultValue(false)]
        public bool AutoFillFixedColumns
        {
            get { return GetStyleFlag(VTCStyleFlags.AutoFillFixedColumns); }
            set
            {
                if (value != GetStyleFlag(VTCStyleFlags.AutoFillFixedColumns))
                {
                    SetStyleFlag(VTCStyleFlags.AutoFillFixedColumns, value);
                    if (value
                            ? (myHeaderBounds.HasHeaders && !myHeaderBounds.HasVariableBounds)
                            : GetStateFlag(VTCStateFlags.FixedColumnAutoFilled))
                    {
                        try
                        {
                            // Hijack the flags used for reorder so SetHeader just updates the width instead
                            // of repopulating the control
                            SetStateFlag(VTCStateFlags.ChangingColumnOrder | VTCStateFlags.UpdateHeaderControl, true);
                            PermuteHeaders(myColumnPermutation, myColumnPermutation, true, -1);
                        }
                        finally
                        {
                            SetStateFlag(VTCStateFlags.ChangingColumnOrder, false);
                        }
                    }
                }
            }
        }

        internal void FinishSplitterAdjustment(bool cancel)
        {
            if (cancel)
            {
                mySplitData.End();
                return;
            }

            // Translate the current split position back into
            // a value that can be applied to the header bounds,
            // which don't know about global or scrollbar position.
            var x = PointToClient(new Point(mySplitData.CurrentX, 0)).X;
            if (LeftScrollBar && !HasVerticalScrollBar)
            {
                x -= SystemInformation.VerticalScrollBarWidth;
            }
            x += myXPos;
            var boundsChanged = myHeaderBounds.EndColumnAdjustment(FullPercentageHeaderWidth, x);

            mySplitData.End();
            if (boundsChanged)
            {
                SetStateFlag(VTCStateFlags.FullColumnHeadersDirty, true);
                AfterHeaderSizeChanged();
            }
        }

        internal bool BeginHeaderDrag(int displayColumn)
        {
            // See if we're allowed to drag from this column
            return 0 == (myHeaderBounds.Headers[displayColumn].Style & VirtualTreeColumnHeaderStyles.DragDisabled);
        }

        private void AfterHeaderSizeChanged()
        {
            var prevPos = myXPos;
            CalcScrollBars();
            if (myHeaderContainer != null)
            {
                if (prevPos != myXPos)
                {
                    // Update column size and position
                    myHeaderContainer.UpdateHeaderControlPosition(true);
                }
                else
                {
                    // Just change the column size
                    UpdateHeaderControlWidths(myHeaderContainer.HeaderControl);
                }
            }
            NativeMethods.InvalidateRect(Handle, IntPtr.Zero, true);
        }

        private void RedrawHeaderFrame()
        {
            if (IsHandleCreated)
            {
                NativeMethods.SetWindowPos(
                    Handle,
                    IntPtr.Zero,
                    0, 0, 0, 0,
                    NativeMethods.SetWindowPosFlags.SWP_NOMOVE |
                    NativeMethods.SetWindowPosFlags.SWP_NOSIZE |
                    NativeMethods.SetWindowPosFlags.SWP_NOZORDER |
                    NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE |
                    NativeMethods.SetWindowPosFlags.SWP_FRAMECHANGED |
                    NativeMethods.SetWindowPosFlags.SWP_DRAWFRAME);
            }
        }

        private bool HeaderVisible
        {
            get { return myHeaderBounds.HasHeaders && DisplayColumnHeaders; }
        }

        private int HeaderHeight
        {
            get { return (myHeaderContainer != null) ? myHeaderContainer.HeaderHeight : 0; }
        }

        private int FullPercentageHeaderWidth
        {
            get { return Bounds.Width - 2 * BorderOffset - SystemInformation.VerticalScrollBarWidth; }
        }

        private int BorderOffset
        {
            get
            {
                if (myBorderOffset == -1)
                {
                    Debug.Assert(!GetStateFlag(VTCStateFlags.WindowPositionChanging), "!GetStateFlag(VTCStateFlags.WindowPositionChanging)");
                    var nonClientWidth = Width - ClientSize.Width;
                    if (HasVerticalScrollBar)
                    {
                        nonClientWidth -= SystemInformation.VerticalScrollBarWidth;
                        if (nonClientWidth < 0)
                        {
                            nonClientWidth = 0;
                        }
                    }
                    Debug.Assert((nonClientWidth % 1) == 0, "(nonClientWidth % 1) == 0");
                    Debug.Assert(nonClientWidth >= 0, "nonClientWidth >= 0");
                    myBorderOffset = nonClientWidth / 2;
                }
                return myBorderOffset;
            }
        }

        /// <summary>
        ///     Automatically resize a visible column to match the contents of the column.
        ///     Note that for proportional columns the full size of the contained text may
        ///     not be available, so this is not guaranteed to have no truncated text in the
        ///     column. This routine will not resize nonadjustable fixed-size columns, columns
        ///     hidden by the current column permutation.
        /// </summary>
        /// <param name="nativeColumn">The column to resize, using the native indexing ordering</param>
        /// <returns>true if a column resize was attempted.</returns>
        public bool AutoSizeColumn(int nativeColumn)
        {
            if (myHeaderBounds.HasHeaders)
            {
                // Check if the column is visible
                var displayColumn = nativeColumn;
                if (myColumnPermutation != null)
                {
                    displayColumn = myColumnPermutation.GetPermutedColumn(nativeColumn);
                    if (displayColumn == -1)
                    {
                        return false;
                    }
                }

                var header = myHeaderBounds.Headers[displayColumn];
                if (header.IsColumnAdjustable)
                {
                    var fullWidth = ComputeColumnWidth(nativeColumn);
                    if (myHeaderBounds.RequestSetColumnWidth(this, displayColumn, fullWidth))
                    {
                        AfterHeaderSizeChanged();
                    }
                }
            }
            return false;
        }

        #endregion // Column Header Routines

        #region Column Header Events

        private static readonly object EVENT_COLUMNHEADERCLICKED = new object();
        private static readonly object EVENT_DRAWCOLUMNHEADERITEM = new object();

        /// <summary>
        ///     Receive an event when a column header is clicked.
        /// </summary>
        public event VirtualTreeColumnHeaderClickEventHandler ColumnHeaderClick
        {
            add { Events.AddHandler(EVENT_COLUMNHEADERCLICKED, value); }
            remove { Events.RemoveHandler(EVENT_COLUMNHEADERCLICKED, value); }
        }

        /// <summary>
        ///     Called when a column header is clicked. Fires the ColumnHeaderClick event if enabled, and
        ///     calls AutoSizeColumn for a DividerDoubleClick if the event was not handled by the user.
        /// </summary>
        /// <param name="e">VirtualTreeColumnHeaderClickEventArgs</param>
        protected virtual void OnColumnHeaderClick(VirtualTreeColumnHeaderClickEventArgs e)
        {
            var handled = false;
            if (Events[EVENT_COLUMNHEADERCLICKED] is VirtualTreeColumnHeaderClickEventHandler handler)
            {
                handler(this, e);
                handled = e.Handled;
            }
            if (!handled
                && e.ClickStyle == VirtualTreeColumnHeaderClickStyle.DividerDoubleClick)
            {
                AutoSizeColumn(e.Column);
            }
        }

        /// <summary>
        ///     Event that indicates listeners should perform owner drawing of a header item.  The e.Index property contains the
        ///     native index of the header that was clicked.  This will only be fired for columns that have either the
        ///     VirtualTreeColumnHeaderStyle.OwnerDraw or the VirtualTreeColumnHeaderStyle.OwnerDrawOverlay style set.
        /// </summary>
        public event DrawItemEventHandler DrawColumnHeaderItem
        {
            add { Events.AddHandler(EVENT_DRAWCOLUMNHEADERITEM, value); }
            remove { Events.RemoveHandler(EVENT_DRAWCOLUMNHEADERITEM, value); }
        }

        /// <summary>
        ///     Called when a column header is clicked. Fires the ColumnHeaderClick event if enabled, and
        ///     calls AutoSizeColumn for a DividerDoubleClick if the event was not handled by the user.
        /// </summary>
        /// <param name="e">VirtualTreeColumnHeaderClickEventArgs</param>
        protected internal virtual void OnDrawColumnHeaderItem(DrawItemEventArgs e)
        {
            if (Events[EVENT_DRAWCOLUMNHEADERITEM] is DrawItemEventHandler handler)
            {
                handler(this, e);
            }
        }

        #endregion // Column Header Events
    }

}
