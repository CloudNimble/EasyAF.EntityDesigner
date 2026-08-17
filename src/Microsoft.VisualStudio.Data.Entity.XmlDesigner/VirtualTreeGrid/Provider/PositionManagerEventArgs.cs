// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for ListShuffle events. Positions are bound to items, which
    ///     are then tracked internally as the tree structure is being modified.
    /// </summary>
    internal sealed class PositionManagerEventArgs : EventArgs, IEnumerable
    {
        private readonly Hashtable myTable;
        private readonly VirtualTree myMultiColumnTree;

        internal PositionManagerEventArgs(VirtualTree owningTree)
        {
            myMultiColumnTree = (null != (owningTree as IMultiColumnTree)) ? owningTree : null;
            myTable = [];
        }

        /// <summary>
        ///     Store positions to track. Generally called from an BeforeListShuffle event
        /// </summary>
        /// <param name="positions">An array of PositionTracker structures</param>
        /// <param name="key">The key for the tracked positions. Usually the instance of the object tracking positions.</param>
        /// <param name="multiColumnPositions">Specify if the positions use multi or single column indices. Note that this value can be different in the RetrievePositions call.</param>
        public void StorePositions(PositionTracker[] positions, object key, bool multiColumnPositions)
        {
            if (positions != null)
            {
                // Translate from single column positions into multicolumn indices when
                // we store the data, then translate back if needed on the way out. Feeding
                // the position tracking algorithms in the core engine consistent data is well
                // worth the up front translation cost.
                if (myMultiColumnTree != null)
                {
                    int i;
                    int startRow;
                    var rowBound = (myMultiColumnTree as ITree).VisibleItemCount;
                    var positionsCount = positions.Length;
                    if (multiColumnPositions)
                    {
                        // Find any items with a column of 'I don't care' and
                        // bind it to the first possible column.
                        for (i = 0; i < positionsCount; ++i)
                        {
                            if (positions[i].Column == -1)
                            {
                                startRow = positions[i].StartRow;
                                if (startRow != -1
                                    && startRow < rowBound)
                                {
                                    positions[i].Column = myMultiColumnTree.FindFirstNonBlankColumn(startRow);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (i = 0; i < positionsCount; ++i)
                        {
                            startRow = positions[i].StartRow;
                            if (startRow != -1
                                && startRow < rowBound)
                            {
                                positions[i].StartRow = myMultiColumnTree.TranslateSingleColumnRow(startRow);
                                positions[i].Column = 0; // Ignore NoColumnAffinity setting, this will always bind correctly
                            }
                        }
                    }
                }
                myTable[key] = positions;
            }
        }

        /// <summary>
        ///     Retrieve tracked positions. Generally called from an AfterListShuffle event
        /// </summary>
        /// <param name="key">The key for the tracked positions. Usually the instance of the object tracking positions.</param>
        /// <param name="multiColumnPositions">Specify if the positions use multi or single column indices</param>
        /// <returns>An array of PositionTracker structures</returns>
        public PositionTracker[] RetrievePositions(object key, bool multiColumnPositions)
        {
            PositionTracker[] positions = myTable[key] as PositionTracker[];
            if (!multiColumnPositions
                && positions != null
                && myMultiColumnTree != null)
            {
                var positionsCount = positions.Length;
                int endRow;
                for (var i = 0; i < positionsCount; ++i)
                {
                    endRow = positions[i].EndRow;
                    if (endRow != -1)
                    {
                        positions[i].EndRow = myMultiColumnTree.TranslateMultiColumnRow(endRow);
                        positions[i].Column = 0;
                    }
                }
            }
            return positions;
        }

        /// <summary>
        ///     Enumerator all PositionTracker arrays in the PositionManager.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return myTable.Values.GetEnumerator();
        }
    }

}
