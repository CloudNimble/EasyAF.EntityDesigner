// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Validation;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Visitor;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    /// Owns the lifetime of every <see cref="EFArtifact"/> loaded into the designer, keyed by <see cref="Uri"/>, and drives
    /// the load pipeline (parse, normalize, resolve) that turns raw XLinq trees into a connected object model.
    /// </summary>
    /// <remarks>
    /// This type lives in the foundation layer: it deliberately takes no dependency on Visual Studio, on any UI framework, or on
    /// the shell, so that the model can be loaded and mutated from a command-line host or from unit tests. Keep it that way.
    /// <para>
    /// Instances are shared, so nearly every member takes <c>lock (this)</c> before touching the artifact dictionaries. The lock
    /// is re-entrant by design: for example <see cref="ClearArtifact"/> is called from inside <see cref="Load"/>'s exception path.
    /// </para>
    /// </remarks>
    public abstract class ModelManager : IDisposable
    {

        #region Fields

        /// <summary>
        /// Maps each loaded artifact to the set (or, in theory, sets) it participates in.
        /// </summary>
        /// <remarks>
        /// Only a single set per artifact is supported today; the list-valued shape exists so multi-set membership can be added
        /// without changing the field. TODO: we should have a case-insensitive URI comparison here.
        /// </remarks>
        private readonly Dictionary<EFArtifact, List<EFArtifactSet>> _artifact2ArtifactSets = [];

        /// <summary>
        /// Creates the concrete <see cref="EFArtifact"/> instances for a given <see cref="Uri"/>. Supplied by the host so the
        /// artifact type can vary without subclassing this manager. Cleared to <see langword="null"/> on dispose.
        /// </summary>
        private IEFArtifactFactory _artifactFactory;

        /// <summary>
        /// The identity map of loaded artifacts, keyed by the <see cref="Uri"/> they were loaded from.
        /// </summary>
        /// <remarks>
        /// TODO: we should have a case-insensitive URI comparison here. Until then two URIs that differ only by case are treated
        /// as distinct documents.
        /// </remarks>
        private readonly Dictionary<Uri, EFArtifact> _artifactsByUri = [];

        /// <summary>
        /// Creates the concrete <see cref="EFArtifactSet"/> that newly loaded artifacts are added to. Cleared to
        /// <see langword="null"/> on dispose.
        /// </summary>
        private IEFArtifactSetFactory _artifactSetFactory;

        /// <summary>
        /// Records the change groups produced by a model transaction, which may span several XLinq transactions.
        /// </summary>
        /// <remarks>
        /// A queue is used rather than a list because committing one change group can itself produce another; enqueuing while
        /// draining is safe, whereas mutating a list mid-enumeration would throw.
        /// </remarks>
        private readonly Queue<EfiChangeGroup> _changeGroups = new Queue<EfiChangeGroup>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the factory used to create <see cref="EFArtifact"/> instances during <see cref="Load"/>.
        /// </summary>
        internal IEFArtifactFactory ArtifactFactory => _artifactFactory;

        /// <summary>
        /// Gets the artifacts currently loaded by this manager.
        /// </summary>
        /// <remarks>
        /// The returned collection is the live dictionary value collection, not a snapshot; the lock is released before the caller
        /// enumerates it, so callers that may load or clear artifacts while iterating must copy it first.
        /// </remarks>
        internal ICollection<EFArtifact> Artifacts
        {
            get
            {
                lock (this)
                {
                    return _artifactsByUri.Values;
                }
            }
        }

        /// <summary>
        /// Gets the factory used to create the <see cref="EFArtifactSet"/> that newly loaded artifacts are registered into.
        /// </summary>
        internal IEFArtifactSetFactory ArtifactSetFactory => _artifactSetFactory;

        /// <summary>
        /// Gets or sets the handler invoked immediately before a batch of model changes is committed, so views can capture
        /// pre-change state.
        /// </summary>
        /// <remarks>
        /// This is a settable <see cref="EventHandler{TEventArgs}"/> <em>property</em>, not a C# <c>event</c>. That distinction
        /// matters: <c>+=</c> on a property compiles to a read, a <see cref="Delegate.Combine(Delegate, Delegate)"/>, and a write,
        /// with no compiler-generated synchronization, so concurrent subscribers can clobber one another; and any caller can
        /// replace the entire invocation list with a plain assignment, silently unsubscribing everyone else. Subscribe from a
        /// single thread and prefer <c>+=</c>/<c>-=</c> over assignment.
        /// </remarks>
        internal EventHandler<EfiChangingEventArgs> BeforeModelChangesCommitted { get; set; }

        /// <summary>
        /// Gets or sets the handler invoked once per change group after changes have been committed to the model, so views can
        /// refresh themselves.
        /// </summary>
        /// <remarks>
        /// As with <see cref="BeforeModelChangesCommitted"/>, this is a settable property rather than a C# <c>event</c>, so
        /// <c>+=</c> is an unsynchronized read-combine-write and a bare assignment discards every existing subscriber.
        /// </remarks>
        internal EventHandler<EfiChangedEventArgs> ModelChangesCommitted { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelManager"/> class.
        /// </summary>
        /// <param name="artifactFactory">Creates the <see cref="EFArtifact"/> instances for a given <see cref="Uri"/>.</param>
        /// <param name="artifactSetFactory">Creates the <see cref="EFArtifactSet"/> that newly loaded artifacts are added to.</param>
        /// <remarks>
        /// Both factories are injected rather than abstract methods so that tests and alternate hosts can supply lightweight
        /// implementations without deriving a new manager.
        /// </remarks>
        internal ModelManager(IEFArtifactFactory artifactFactory, IEFArtifactSetFactory artifactSetFactory)
        {
            _artifactFactory = artifactFactory;
            _artifactSetFactory = artifactSetFactory;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ModelManager"/> class that was not disposed.
        /// </summary>
        /// <remarks>
        /// Reaching the finalizer is always a bug: artifacts hold XLinq documents and host resources that must be released
        /// deterministically, so the failure is asserted loudly in debug builds before the best-effort cleanup runs.
        /// </remarks>
        ~ModelManager()
        {
            Debug.Fail("ModelManager was not disposed of properly!");
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases all artifacts owned by this manager and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the artifacts, artifact-set mappings, pending change groups, and factory references held by this manager.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from the finalizer.</param>
        /// <remarks>
        /// Nothing is released on the finalizer path because every resource held here is itself a managed object with its own
        /// finalization story, and touching them from the finalizer thread would be unsafe.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    // An artifact might be depended on another artifact which means disposing an artifact might also automatically dispose its dependent artifacts.
                    foreach (var a in _artifactsByUri.Values.ToArray())
                    {
                        if (a is not null
                            && a.IsDisposed == false)
                        {
                            a.Dispose();
                        }
                    }

                    _artifactsByUri.Clear();
                    _artifact2ArtifactSets.Clear();
                    ClearChangeGroups();

                    _artifactFactory = null;
                    _artifactSetFactory = null;
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Raises <see cref="BeforeModelChangesCommitted"/> so listeners can react before the pending changes are applied.
        /// </summary>
        /// <param name="cpc">The context describing the command processor transaction that is about to commit.</param>
        internal void BeforeCommitChangeGroups(CommandProcessorContext cpc)
        {
            EfiChangingEventArgs args = new EfiChangingEventArgs(cpc);
            if (BeforeModelChangesCommitted is not null)
            {
                // now tell everyone that the changes are about to be committed
                BeforeModelChangesCommitted(this, args);
            }
        }

        /// <summary>
        /// Removes the artifact associated with the given <see cref="Uri"/> from this manager and from its artifact set,
        /// disposing it in the process.
        /// </summary>
        /// <param name="uri">The URI of the artifact to remove. Unknown URIs are ignored.</param>
        /// <remarks>
        /// The artifact is disposed <em>before</em> the bookkeeping dictionaries are updated, because disposal walks back to its
        /// owning set through this manager. Virtual for testing.
        /// </remarks>
        internal virtual void ClearArtifact(Uri uri)
        {
            lock (this)
            {
                if (_artifactsByUri.TryGetValue(uri, out EFArtifact artifact))
                {
                    if (artifact is not null)
                    {
                        var artifactSet = GetArtifactSet(uri);
                        Debug.Assert(artifactSet is not null);

                        // need to dispose the artifact first, as it will try and access it's set
                        artifact.Dispose();

                        // clean up the model manager's collections
                        _artifact2ArtifactSets.Remove(artifact);
                        _artifactsByUri.Remove(uri);

                        // remove this artifact from the set
                        artifactSet.RemoveArtifact(artifact);
                    }
                }
            }
        }

        /// <summary>
        /// Discards every change group recorded but not yet routed.
        /// </summary>
        internal void ClearChangeGroups()
        {
            _changeGroups.Clear();
        }

        /// <summary>
        /// Creates the command that renames the given item, letting each concrete manager supply the rename semantics
        /// appropriate to its model.
        /// </summary>
        /// <param name="element">The normalizable item to rename.</param>
        /// <param name="newName">The new name to assign.</param>
        /// <param name="uniquenessIsCaseSensitive">Whether the uniqueness check for the new name treats case as significant.</param>
        /// <returns>A <see cref="RenameCommand"/> that performs the rename when executed.</returns>
        internal abstract RenameCommand CreateRenameCommand(EFNormalizableItem element, string newName, bool uniquenessIsCaseSensitive);

        /// <summary>
        /// Gets the <see cref="EFArtifact"/> for a particular <see cref="Uri"/> from the cache, or <see langword="null"/> if it
        /// has not been loaded.
        /// </summary>
        /// <param name="uri">The URI to look up.</param>
        /// <returns>The cached artifact, or <see langword="null"/> when the URI has not been loaded.</returns>
        /// <remarks>Virtual to allow mocking.</remarks>
        internal virtual EFArtifact GetArtifact(Uri uri)
        {
            lock (this)
            {
                if (_artifactsByUri.TryGetValue(uri, out EFArtifact result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="EFArtifactSet"/> containing the artifact for a particular <see cref="Uri"/> from the cache, or
        /// <see langword="null"/> if it has not been loaded.
        /// </summary>
        /// <param name="uri">The URI of an artifact whose owning set is wanted.</param>
        /// <returns>The set containing the artifact, or <see langword="null"/> when the URI has not been loaded.</returns>
        /// <remarks>Virtual to allow mocking.</remarks>
        internal virtual EFArtifactSet GetArtifactSet(Uri uri)
        {
            lock (this)
            {
                _artifactsByUri.TryGetValue(uri, out EFArtifact artifact);
                if (artifact is not null)
                {
                    if (_artifact2ArtifactSets.TryGetValue(artifact, out List<EFArtifactSet> result))
                    {
                        Debug.Assert(result.Count == 1, "Support for an artifact spanning multiple sets is not yet implemented");
                        return result[0];
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="AttributeContentValidator"/> used for validating XML attribute values for this
        /// <see cref="ModelManager"/>.
        /// </summary>
        /// <param name="artifact">The artifact whose schema determines the legal attribute content.</param>
        /// <returns>The validator appropriate to the artifact's schema version.</returns>
        internal abstract AttributeContentValidator GetAttributeContentValidator(EFArtifact artifact);

        /// <summary>
        /// Gets the <see cref="EFArtifact"/> for a particular <see cref="Uri"/>, loading it if it has not been loaded yet.
        /// </summary>
        /// <param name="uri">The URI of the artifact to fetch or load.</param>
        /// <param name="xmlModelProvider">The provider supplying the XLinq model. This should always be <see langword="null"/> except for our unit tests, where the shell-backed provider is unavailable.</param>
        /// <returns>The artifact for the URI, or <see langword="null"/> when it could not be loaded.</returns>
        /// <remarks>
        /// Loading one artifact can pull in related artifacts, so the load result is searched for the requested URI rather than
        /// assuming a single artifact came back. Virtual for testing.
        /// </remarks>
        internal virtual EFArtifact GetNewOrExistingArtifact(Uri uri, XmlModelProvider xmlModelProvider)
        {
            lock (this)
            {
                var result = GetArtifact(uri);
                if (result is null)
                {
                    // Loading an artifact might cause related artifacts to be automatically loaded.
                    var artifacts = Load(uri, xmlModelProvider);
                    if (artifacts is not null)
                    {
                        result = artifacts.Where(a => a.Uri == uri).FirstOrDefault();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the namespace that the root element of the given node's document lives in.
        /// </summary>
        /// <param name="node">The node whose document root namespace is wanted.</param>
        /// <returns>The root <see cref="XNamespace"/> for the node's artifact.</returns>
        internal abstract XNamespace GetRootNamespace(EFObject node);

        /// <summary>
        /// Gets the names of every XML element encountered during parsing that no <see cref="EFElement"/> claimed.
        /// </summary>
        /// <returns>
        /// The distinct unprocessed element names across all loaded artifacts in debug builds; an empty sequence in release
        /// builds.
        /// </returns>
        /// <remarks>
        /// Unprocessed-element tracking is a debug-only diagnostic used to catch schema elements the model does not yet
        /// understand, so it is compiled out of release builds to avoid the per-element bookkeeping cost.
        /// </remarks>
        internal IEnumerable<XName> GetUnprocessedElements()
        {
#if DEBUG
            var allXnames = new HashSet<XName>();
            foreach (var a in _artifactsByUri.Values)
            {
                foreach (var xname in a.UnprocessedElements)
                {
                    allXnames.Add(xname);
                }
            }
            return allXnames;
#else
            return new XName[0];
#endif
        }

        /// <summary>
        /// Asks the passed in item and all of its children to create their normalized names and load them into the global
        /// symbol table.
        /// </summary>
        /// <param name="item">The container whose subtree should be normalized.</param>
        /// <exception cref="InvalidDataException">Thrown when a pass over the subtree normalizes nothing new while items remain unnormalized, which means those items can never resolve.</exception>
        /// <remarks>
        /// Normalization runs in repeated passes because an item's normalized name can depend on an ancestor or sibling that has
        /// not been normalized yet; the loop ends when nothing is left or no forward progress is made.
        /// </remarks>
        internal static void NormalizeItem(EFContainer item)
        {
            NormalizingVisitor visitor = new NormalizingVisitor();

            var lastMissedCount = -1;
            while (visitor.MissedCount != 0)
            {
                visitor.ResetMissedCount();
                visitor.Traverse(item);

                // every item should be able to normalize
                if (lastMissedCount == visitor.MissedCount)
                {
                    // subsequent passes didn't normalize any new items
                    throw new InvalidDataException();
                }

                lastMissedCount = visitor.MissedCount;
            }
        }

        /// <summary>
        /// Records a change group produced by the current model transaction so it can be routed to listeners on commit.
        /// </summary>
        /// <param name="changeGroup">The change group to record. Null and empty groups are ignored.</param>
        internal void RecordChangeGroup(EfiChangeGroup changeGroup)
        {
            if (changeGroup is not null
                &&
                changeGroup.Count > 0)
            {
                _changeGroups.Enqueue(changeGroup);
            }
        }

        /// <summary>
        /// Adds an artifact to the given set, records it in this manager's lookup tables, and initializes it.
        /// </summary>
        /// <param name="efArtifact">The artifact to register.</param>
        /// <param name="efArtifactSet">The set the artifact belongs to.</param>
        /// <remarks>
        /// Registration is idempotent by omission rather than by error: an artifact that is already known is silently skipped
        /// after asserting, because double registration indicates a caller bug but should not corrupt an otherwise valid load.
        /// </remarks>
        internal void RegisterArtifact(EFArtifact efArtifact, EFArtifactSet efArtifactSet)
        {
            Debug.Assert(
                _artifactsByUri.ContainsKey(efArtifact.Uri) == false && _artifact2ArtifactSets.ContainsKey(efArtifact) == false,
                "This artifact has been registered in model manager.");

            if (_artifactsByUri.ContainsKey(efArtifact.Uri) == false
                && _artifact2ArtifactSets.ContainsKey(efArtifact) == false)
            {
                if (efArtifactSet.Artifacts.Contains(efArtifact) == false)
                {
                    efArtifactSet.Add(efArtifact);
                }

                _artifactsByUri[efArtifact.Uri] = efArtifact;

                List<EFArtifactSet> artifactSetList = new List<EFArtifactSet>(1)
                {
                    efArtifactSet
                };
                _artifact2ArtifactSets[efArtifact] = artifactSetList;
                efArtifact.Init();
            }
        }

        /// <summary>
        /// Re-keys a loaded artifact from one <see cref="Uri"/> to another, for example after the underlying file is renamed.
        /// </summary>
        /// <param name="oldUri">The URI the artifact is currently registered under.</param>
        /// <param name="newUri">The URI to register the artifact under.</param>
        /// <remarks>
        /// The dictionary key and the artifact's own URI are updated together so the identity map and the artifact never
        /// disagree. Unknown old URIs are ignored.
        /// </remarks>
        internal void RenameArtifact(Uri oldUri, Uri newUri)
        {
            lock (this)
            {
                if (_artifactsByUri.TryGetValue(oldUri, out EFArtifact result))
                {
                    _artifactsByUri.Remove(oldUri);
                    _artifactsByUri.Add(newUri, result);
                    result.RenameArtifact(newUri);
                }
            }
        }

        /// <summary>
        /// Asks the passed in item and all its children to resolve references to other <see cref="EFElement"/> instances across
        /// the entire model, i.e., a ScalarProperty will link up to its entity and storage properties.
        /// </summary>
        /// <param name="item">The container whose subtree should be resolved.</param>
        /// <remarks>
        /// Unlike normalization, failing to resolve is legitimate — a reference may point at something the user has not created
        /// yet — so the loop simply stops once a pass makes no further progress instead of throwing.
        /// </remarks>
        internal void ResolveItem(EFContainer item)
        {
            lock (this)
            {
                ResolvingVisitor visitor = new ResolvingVisitor(item.Artifact.ArtifactSet);

                var lastMissedCount = visitor.MissedCount;

                while (visitor.MissedCount != 0)
                {
                    visitor.ResetMissedCount();
                    visitor.Traverse(item);

                    // if we can't resolve any more then we are done
                    if (lastMissedCount == visitor.MissedCount)
                    {
                        break;
                    }

                    lastMissedCount = visitor.MissedCount;
                }
            }
        }

        /// <summary>
        /// Drains the recorded change groups, raising <see cref="ModelChangesCommitted"/> once per group.
        /// </summary>
        /// <remarks>
        /// The queue is drained rather than enumerated because a listener handling one group can record another; the queue is
        /// cleared in a <c>finally</c> so a throwing listener cannot leave stale groups to be replayed on the next commit.
        /// </remarks>
        internal void RouteChangeGroups()
        {
            try
            {
                while (_changeGroups.Count > 0)
                {
                    var changeGroup = _changeGroups.Dequeue();
                    EfiChangedEventArgs args = new EfiChangedEventArgs(changeGroup);
                    if (ModelChangesCommitted is not null)
                    {
                        // now tell everyone that things have changed
                        ModelChangesCommitted(this, args);
                    }
                }
            }
            finally
            {
                ClearChangeGroups();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the artifact with the given URI, along with any related artifacts the factory pulls in, and runs the full
        /// parse/normalize/resolve pipeline over the resulting artifact set.
        /// </summary>
        /// <param name="fileUri">The URI of the artifact to load.</param>
        /// <param name="xmlModelProvider">The provider supplying the XLinq model, or <see langword="null"/> to let the factory choose.</param>
        /// <returns>The artifacts created by the factory, or <see langword="null"/> when the factory produced nothing usable.</returns>
        /// <exception cref="InvalidDataException">Thrown when normalization cannot make progress over the loaded set.</exception>
        /// <remarks>
        /// Depending on the artifact-set mode, a new artifact set might be created for the newly created artifact(s); a single
        /// set is created lazily and shared by every artifact in this load. On failure each artifact is both disposed and
        /// cleared, since an artifact that failed before registration will not be present in the URI table.
        /// </remarks>
        private IList<EFArtifact> Load(Uri fileUri, XmlModelProvider xmlModelProvider)
        {
            lock (this)
            {
                List<EFArtifact> artifacts = null;
                EFArtifactSet artifactSet = null;
                try
                {
                    artifacts = _artifactFactory.Create(this, fileUri, xmlModelProvider) as List<EFArtifact>;
                    // Case where the artifact factory failed to instantiate artifact(s).
                    // This was written as && rather than ||, which made the guard unreachable: a null
                    // artifacts short circuited into dereferencing it, and a non-null one skipped the
                    // check entirely. Either condition means the factory produced nothing usable.
                    if (artifacts is null
                        || artifacts.Count <= 0)
                    {
                        Debug.Assert(false, "Could not create EFArtifact using current factory");
                        return null;
                    }
                        // Case where artifact factory failed to load the artifact with give URI.
                    else if (artifacts.Where(a => a.Uri == fileUri).FirstOrDefault() is null)
                    {
                        Debug.Assert(false, "Artifact Factory does not created an artifact with URI:" + fileUri.LocalPath);
                        return null;
                    }

                    EFArtifactSet efArtifactSet = null;
                    // Initialize each artifact in the list.
                    foreach (var artifact in artifacts)
                    {
                        Debug.Assert(
                            _artifact2ArtifactSets.ContainsKey(artifact) == false, "Unexpected entry for artifact in artifact2ArtifactSet");
                        if (_artifact2ArtifactSets.ContainsKey(artifact) == false)
                        {
                            efArtifactSet ??= _artifactSetFactory.CreateArtifactSet(artifact);
                            RegisterArtifact(artifact, efArtifactSet);
                        }
                    }

                    artifactSet = GetArtifactSet(fileUri);
                    ParseArtifactSet(artifactSet);
                    NormalizeArtifactSet(artifactSet);
                    ResolveArtifactSet(artifactSet);

                    // Tell the artifacts that they are loaded and ready
                    artifacts.ForEach((a) => { a.OnLoaded(); });
                }
                catch (Exception)
                {
                    // an exception occurred during loading, dispose each artifact in the list and rethrow.
                    // call dispose & clear the artifact.  We need both since the the entry may not be
                    // in the _artifactsByUri table.
                    artifacts?.ForEach(
                        artifact =>
                            {
                                artifact.Dispose();
                                ClearArtifact(artifact.Uri);
                            });
                    throw;
                }

                return artifacts;
            }
        }

        /// <summary>
        /// Asks every <see cref="EFElement"/> in the given <see cref="EFArtifactSet"/> to create its normalized name and load
        /// that into the global symbol table.
        /// </summary>
        /// <param name="artifactSet">The set whose artifacts should be normalized.</param>
        /// <exception cref="InvalidDataException">Thrown when a full pass normalizes nothing new while items remain unnormalized, which means those items can never resolve.</exception>
        /// <remarks>
        /// Every item is expected to normalize eventually, so unlike resolution a stalled pass is treated as corrupt input rather
        /// than as a normal stopping condition.
        /// </remarks>
        private void NormalizeArtifactSet(EFArtifactSet artifactSet)
        {
            lock (this)
            {
                NormalizingVisitor visitor = new NormalizingVisitor();

                var lastMissedCount = -1;
                while (visitor.MissedCount != 0)
                {
                    visitor.ResetMissedCount();

                    foreach (var artifact in artifactSet.Artifacts)
                    {
                        visitor.Traverse(artifact);
                    }

                    // every item should be able to normalize
                    if (lastMissedCount == visitor.MissedCount)
                    {
                        throw new InvalidDataException("Subsequent passes didn't normalize any new items.");
                    }

                    lastMissedCount = visitor.MissedCount;
                }
            }
        }

        /// <summary>
        /// Parses all of the loaded Entity and Mapping models in the given <see cref="EFArtifactSet"/>, creating
        /// <see cref="EFElement"/> instances for every node in the XLinq tree.
        /// </summary>
        /// <param name="artifactSet">The set whose artifacts should be parsed.</param>
        /// <remarks>
        /// Artifacts that have already advanced past <see cref="EFElementState.None"/> are skipped, so an artifact pulled into
        /// this set by an earlier load is not parsed twice. In debug builds the artifact's unprocessed-element set is reset and
        /// handed to the parse so unclaimed elements can be reported afterwards.
        /// </remarks>
        private void ParseArtifactSet(EFArtifactSet artifactSet)
        {
            lock (this)
            {
                foreach (var a in artifactSet.Artifacts)
                {
                    if (a.State == EFElementState.None)
                    {
                        HashSet<XName> s = null;
#if DEBUG
                        s = a.UnprocessedElements;
                        s.Clear();
#endif
                        a.Parse(s);
                    }
                }
            }
        }

        /// <summary>
        /// Asks every <see cref="EFElement"/> in the given <see cref="EFArtifactSet"/> to resolve references to other
        /// <see cref="EFElement"/> instances in the set, i.e., a ScalarProperty will link up to its entity and storage properties.
        /// </summary>
        /// <param name="artifactSet">The set whose artifacts should be resolved.</param>
        /// <remarks>
        /// Repeated passes are needed because resolving one reference can make another resolvable. Unresolved references are a
        /// valid end state — the user may not have created the target yet — so the loop stops on the first pass that makes no
        /// progress instead of throwing.
        /// </remarks>
        private void ResolveArtifactSet(EFArtifactSet artifactSet)
        {
            lock (this)
            {
                ResolvingVisitor visitor = new ResolvingVisitor(artifactSet);

                var lastMissedCount = visitor.MissedCount;

                while (visitor.MissedCount != 0)
                {
                    visitor.ResetMissedCount();

                    foreach (var artifact in artifactSet.Artifacts)
                    {
                        visitor.Traverse(artifact);
                    }

                    // if we can't resolve any more then we are done
                    if (lastMissedCount == visitor.MissedCount)
                    {
                        break;
                    }

                    lastMissedCount = visitor.MissedCount;
                }
            }
        }

        #endregion

    }

}
