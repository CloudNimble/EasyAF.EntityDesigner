// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Holds the available layout engines and tracks which one is in effect.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var manager = new LayoutEngineManager([new DslLayoutEngine()]);
    ///     manager.TrySetCurrent(DslLayoutEngine.EngineKey);
    ///     manager.Current.Layout(surface, shapes);
    ///     </code>
    /// </example>
    /// <remarks>
    ///     Engines are supplied at construction and keyed by <see cref="LayoutEngineBase.Key" />, so the set is
    ///     a decision the host makes once and tests can replace wholesale. <see cref="Current" /> is what the
    ///     designer's toolbar toggle moves. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal sealed class LayoutEngineManager
    {

        #region Fields

        private readonly Dictionary<string, LayoutEngineBase> _engines;
        private LayoutEngineBase _current;

        #endregion

        #region Properties

        /// <summary>
        ///     The engine that <see cref="View.EntityDesignerSurface.AutoLayoutDiagram(System.Collections.IList)" />
        ///     will use.
        /// </summary>
        /// <exception cref="ArgumentNullException">A null engine was assigned.</exception>
        /// <exception cref="ArgumentException">The engine assigned was not one of the registered engines.</exception>
        /// <remarks>
        ///     Assigning an unregistered engine throws rather than silently adopting it, so the set of engines
        ///     stays the one the host chose and <see cref="LayoutEngines" /> never disagrees with what is running.
        /// </remarks>
        public LayoutEngineBase Current
        {
            get { return _current; }
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!_engines.ContainsKey(value.Key))
                {
                    throw new ArgumentException(
                        $"'{value.Key}' is not a registered layout engine.", nameof(value));
                }

                _current = value;
            }
        }

        /// <summary>
        ///     The registered engines, keyed by <see cref="LayoutEngineBase.Key" />.
        /// </summary>
        public IReadOnlyDictionary<string, LayoutEngineBase> LayoutEngines
        {
            get { return _engines; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Registers <paramref name="engines" /> and makes the first one current.
        /// </summary>
        /// <param name="engines">The engines to register. Must contain at least one, with distinct keys.</param>
        /// <exception cref="ArgumentNullException"><paramref name="engines" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="engines" /> is empty, contains a null entry, or contains two engines with the
        ///     same <see cref="LayoutEngineBase.Key" />.
        /// </exception>
        /// <remarks>
        ///     The first engine wins by default because the caller's ordering is the only statement of intent
        ///     available here, and a manager with no current engine would push a null check onto every layout
        ///     call site.
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

            var duplicate = ordered.GroupBy(engine => engine.Key).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new ArgumentException(
                    $"More than one layout engine is registered under the key '{duplicate.Key}'.", nameof(engines));
            }

            _engines = ordered.ToDictionary(engine => engine.Key, StringComparer.OrdinalIgnoreCase);
            _current = ordered[0];
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Makes the engine registered under <paramref name="key" /> current.
        /// </summary>
        /// <param name="key">The key of the engine to select.</param>
        /// <returns>
        ///     <see langword="true" /> if an engine was registered under <paramref name="key" />; otherwise
        ///     <see langword="false" />, leaving <see cref="Current" /> unchanged.
        /// </returns>
        /// <remarks>
        ///     Returns false rather than throwing because the callers are a toolbar toggle and a command line
        ///     argument, both of which want to fall back to what is already running instead of failing.
        /// </remarks>
        public bool TrySetCurrent(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !_engines.TryGetValue(key, out var engine))
            {
                return false;
            }

            _current = engine;

            return true;
        }

        #endregion

    }

}
