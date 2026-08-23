// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Entity.Design.Edmx.Designer;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Holds the available layout engines and hands out the one a diagram asked for.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var manager = new LayoutEngineManager([new DslLayoutEngine(), new MsAglLayoutEngine()]);
    ///     manager.Resolve(LayoutMode.Modern).Layout(surface, shapes, ConnectorMode.Orthogonal);
    ///     </code>
    /// </example>
    /// <remarks>
    ///     Engines are supplied at construction and keyed by <see cref="LayoutEngineBase.Mode" />, so the set is
    ///     a decision the host makes once and tests can replace wholesale.
    ///     <para>
    ///     There is deliberately no current engine. One manager serves every open diagram, and each diagram
    ///     carries its own <see cref="Edmx.Designer.Diagram.LayoutMode" />, so a single shared selection would
    ///     be wrong for every diagram but the last one touched.
    ///     </para>
    ///     See specs/diagram-layout-engines.md.
    /// </remarks>
    internal sealed class LayoutEngineManager
    {

        #region Fields

        private readonly LayoutEngineBase _default;
        private readonly Dictionary<LayoutMode, LayoutEngineBase> _engines;

        #endregion

        #region Properties

        /// <summary>
        ///     The registered engines, keyed by <see cref="LayoutEngineBase.Mode" />.
        /// </summary>
        public IReadOnlyDictionary<LayoutMode, LayoutEngineBase> LayoutEngines
        {
            get { return _engines; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Registers <paramref name="engines" />, the first of which becomes the fallback.
        /// </summary>
        /// <param name="engines">The engines to register. Must contain at least one, with distinct modes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="engines" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="engines" /> is empty, contains a null entry, or contains two engines with the
        ///     same <see cref="LayoutEngineBase.Mode" />.
        /// </exception>
        /// <remarks>
        ///     The first engine is the fallback because the caller's ordering is the only statement of intent
        ///     available here, and a manager that could return nothing would push a null check onto every
        ///     layout call site.
        /// </remarks>
        public LayoutEngineManager(IEnumerable<LayoutEngineBase> engines)
        {
            if (engines is null)
            {
                throw new ArgumentNullException(nameof(engines));
            }

            var ordered = engines.ToList();

            if (ordered.Count == 0)
            {
                throw new ArgumentException("At least one layout engine is required.", nameof(engines));
            }

            if (ordered.Any(engine => engine is null))
            {
                throw new ArgumentException("Layout engines cannot be null.", nameof(engines));
            }

            var duplicate = ordered.GroupBy(engine => engine.Mode).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new ArgumentException(
                    $"More than one layout engine is registered for the mode '{duplicate.Key}'.", nameof(engines));
            }

            _engines = ordered.ToDictionary(engine => engine.Mode);
            _default = ordered[0];
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Returns the engine registered for <paramref name="mode" />, or the fallback when none is.
        /// </summary>
        /// <param name="mode">The mode the diagram asked for.</param>
        /// <returns>An engine, never <see langword="null" />.</returns>
        /// <remarks>
        ///     Falls back rather than throwing because the value comes out of a file a user can hand-edit, and
        ///     an unrecognized mode should cost a diagram its preferred arrangement, not its ability to open.
        /// </remarks>
        public LayoutEngineBase Resolve(LayoutMode mode)
        {
            return _engines.TryGetValue(mode, out var engine) ? engine : _default;
        }

        #endregion

    }

}
