// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.View.Events;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.View.Events;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Immutability;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;
using ModelAssociation = Microsoft.Data.Entity.Design.Edmx.Entity.Association;
using ModelDiagram = Microsoft.Data.Entity.Design.Edmx.Designer.Diagram;
using ViewModelEntityType = Microsoft.Data.Entity.Design.Diagrams.ViewModel.EntityType;
using ViewModelNavigationProperty = Microsoft.Data.Entity.Design.Diagrams.ViewModel.NavigationProperty;
using ViewModelProperty = Microsoft.Data.Entity.Design.Diagrams.ViewModel.Property;
using ViewModelPropertyBase = Microsoft.Data.Entity.Design.Diagrams.ViewModel.PropertyBase;

namespace Microsoft.Data.Entity.Design.Diagrams.View
{
    /// <remarks>
    ///     <b>Single threaded.</b> Committing a transaction on this surface writes a static dictionary inside the
    ///     Modeling SDK, so two surfaces cannot be driven concurrently in one process. See
    ///     specs/threading-model.md.
    /// </remarks>
    partial class EntityDesignerSurface : IViewDiagram
    {
        private const int undefinedZoomLevel = -1;
        internal const int IMPLICIT_AUTO_LAYOUT_CEILING = 1000;

        [NonSerialized]
        internal AutoArrangeHelper Arranger = new AutoArrangeHelper();

        /// <summary>
        ///     The layout engines this surface can arrange itself with, and which one is in effect.
        /// </summary>
        /// <remarks>
        ///     Seeded with the Modeling SDK's own layout so the designer behaves exactly as it always has until
        ///     something moves <see cref="LayoutEngineManager.Current" />. See specs/diagram-layout-engines.md.
        /// </remarks>
        [NonSerialized]
        internal LayoutEngineManager LayoutEngines = new LayoutEngineManager([new DslLayoutEngine()]);

        private bool _displayNameAndType;
        private bool _disableFixUpDiagramSelection;
        private EntitiesClipboardFormat _clipboardObjects;

        internal event EventHandler OnDiagramTitleChanged;

        private readonly EmphasizedShapes _emphasizedShapes = [];

        internal bool DisableFixUpDiagramSelection
        {
            get { return _disableFixUpDiagramSelection; }
            set { _disableFixUpDiagramSelection = value; }
        }

        /// <summary>
        ///     Updates the selection during FixUpDiagram.  If _disableFixUpDiagramSelection is false, the behavior is to select
        ///     the newChildShape on the active diagram view if there is one, or on all
        ///     views if there is no active view.
        /// </summary>
        /// <param name="newChildShape">The new child shape that is added by FixUpDiagram</param>
        public override IList FixUpDiagramSelection(ShapeElement newChildShape)
        {
            if (_disableFixUpDiagramSelection)
            {
                return new ArrayList(0);
            }
            else
            {
                return base.FixUpDiagramSelection(newChildShape);
            }
        }

        /// <summary>
        ///     When diagram is finished initialized, it will automatically select a shape that is closest to origin (coordinate 0,0).
        ///     This property allows the client to specify which diagram item to select.
        /// </summary>
        internal DiagramItemCollection InitialDiagramItemSelection { get; set; }

        /// <summary>
        ///     Whether the modeling framework has initialized this diagram yet.
        /// </summary>
        /// <remarks>
        ///     Lets a host that attached after <see cref="Initialized" /> fired know it still has work to do.
        /// </remarks>
        internal bool IsInitialized { get; private set; }

        #region IViewDiagram interface

        public void AddOrShowEFElementInDiagram(EFElement efElement)
        {
            ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
            if (modelDiagram.IsEFObjectRepresentedInDiagram(efElement))
            {
                /// Navigate to the "most-appropriate" DSL node for the given EFObject in the diagram.
                DiagramNavigator.NavigateToNodeInDiagram(this, efElement);
            }
            else
            {
                List<EFElement> efElementList = new List<EFElement>
                {
                    efElement
                };
                CommandProcessorContext cpc = new CommandProcessorContext(
                    ModelElement.EditingContext,
                    EfiTransactionOriginator.EntityDesignerOriginatorId,
                    EntityDesignerRes.Tx_DropItems);
                CommandProcessor.InvokeSingleCommand(cpc, new CreateDiagramItemForEFElementsCommand(efElementList, modelDiagram, false));
            }
            EnsureSelectionVisible();
        }

        #endregion

        #region Host requests

        /// <summary>
        ///     Raised when an inheritance was abandoned because it would have been circular.
        /// </summary>
        internal event EventHandler<CircularInheritanceDetectedEventArgs> CircularInheritanceDetected;

        /// <summary>
        ///     Raised when the designer could not rebuild itself from the model.
        /// </summary>
        internal event EventHandler<DiagramReloadFailedEventArgs> DiagramReloadFailed;

        /// <summary>
        ///     Raised when navigation landed on a mapping element.
        /// </summary>
        internal event EventHandler<MappingDetailsNavigationRequestedEventArgs> MappingDetailsNavigationRequested;

        /// <summary>
        ///     Raised when the designer needs the details of a new association.
        /// </summary>
        internal event EventHandler<NewAssociationRequestedEventArgs> NewAssociationRequested;

        /// <summary>
        ///     Raised when the designer needs the details of a new entity type.
        /// </summary>
        internal event EventHandler<NewEntityTypeRequestedEventArgs> NewEntityTypeRequested;

        /// <summary>
        ///     Raised when the designer needs the details of a new function import.
        /// </summary>
        internal event EventHandler<NewFunctionImportRequestedEventArgs> NewFunctionImportRequested;

        /// <summary>
        ///     Raised when the designer needs the base and derived types for a new inheritance.
        /// </summary>
        internal event EventHandler<NewInheritanceRequestedEventArgs> NewInheritanceRequested;

        /// <summary>
        ///     Raised when the user asks to edit an association's referential constraint.
        /// </summary>
        internal event EventHandler<ReferentialConstraintRequestedEventArgs> ReferentialConstraintRequested;

        /// <summary>
        ///     Raised when a delete would leave storage entity sets unmapped.
        /// </summary>
        internal event EventHandler<UnmappedStorageEntitySetsDeletionRequestedEventArgs> UnmappedStorageEntitySetsDeletionRequested;

        /// <summary>
        ///     Raised once the diagram has been initialized by the modeling framework.
        /// </summary>
        /// <remarks>
        ///     This fires while the document is still loading, before any view exists, so a host that attaches
        ///     later will miss it. Check <see cref="IsInitialized" /> on attach and catch up if it is already
        ///     set.
        /// </remarks>
        internal event EventHandler Initialized;

        /// <summary>
        ///     Raised when an operation that may take a noticeable time has finished.
        /// </summary>
        internal event EventHandler LongOperationEnded;

        /// <summary>
        ///     Raised when an operation that may take a noticeable time is starting.
        /// </summary>
        internal event EventHandler LongOperationStarted;

        /// <summary>
        ///     Raised when a view's watermark needs its clickable links attached.
        /// </summary>
        internal event EventHandler<WatermarkLinksRequestedEventArgs> WatermarkLinksRequested;

        /// <summary>
        ///     Raised to let the host replace the watermark the designer chose.
        /// </summary>
        internal event EventHandler<WatermarkTextRequestedEventArgs> WatermarkTextRequested;

        /// <summary>
        ///     Marks a stretch of work that may take long enough for the host to want to say so.
        /// </summary>
        /// <returns>A scope that reports the end of the operation when disposed.</returns>
        /// <remarks>
        ///     Whether that means a wait cursor, a progress bar or nothing at all is the host's to decide. With
        ///     nothing subscribed this costs a pair of null checks. See specs/layer-map.md.
        /// </remarks>
        /// <example>
        ///     <code>
        ///     using (BeginLongOperation())
        ///     {
        ///         AutoLayoutDiagram(shapes);
        ///     }
        ///     </code>
        /// </example>
        internal LongOperationScope BeginLongOperation()
        {
            LongOperationStarted?.Invoke(this, EventArgs.Empty);

            return new LongOperationScope(this);
        }

        /// <summary>
        ///     Reports that a long operation has finished.
        /// </summary>
        internal void OnLongOperationEnded()
        {
            LongOperationEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Reports a failed reload to the host.
        /// </summary>
        /// <param name="message">The localized message describing the failure.</param>
        /// <param name="artifactPath">The local path of the artifact being rebuilt.</param>
        internal void OnDiagramReloadFailed(string message, string artifactPath)
        {
            DiagramReloadFailed?.Invoke(this, new DiagramReloadFailedEventArgs(message, artifactPath));
        }

        /// <summary>
        ///     Reports that navigation landed on a mapping element.
        /// </summary>
        /// <param name="mappingElement">The mapping element that was navigated to.</param>
        /// <param name="usesFunctionMapping">
        ///     Whether the element maps through modification functions, or <see langword="null" /> if unknown.
        /// </param>
        internal void OnMappingDetailsNavigationRequested(EFObject mappingElement, bool? usesFunctionMapping)
        {
            MappingDetailsNavigationRequested?.Invoke(
                this, new MappingDetailsNavigationRequestedEventArgs(mappingElement, usesFunctionMapping));
        }

        /// <summary>
        ///     Reports a rejected circular inheritance to the host.
        /// </summary>
        /// <param name="derivedEntityType">The type whose base type was being set.</param>
        /// <param name="baseEntityType">The type that would have become the base type.</param>
        internal void OnCircularInheritanceDetected(
            ConceptualEntityType derivedEntityType, ConceptualEntityType baseEntityType)
        {
            CircularInheritanceDetected?.Invoke(
                this, new CircularInheritanceDetectedEventArgs(derivedEntityType, baseEntityType));
        }

        /// <summary>
        ///     Asks the host to let the user edit an association's referential constraint.
        /// </summary>
        /// <param name="association">The association being edited.</param>
        /// <returns>Commands describing the user's changes; empty when nothing changed or nothing handled it.</returns>
        internal IEnumerable<Command> RequestReferentialConstraint(ModelAssociation association)
        {
            var request = new ReferentialConstraintRequestedEventArgs(association);
            ReferentialConstraintRequested?.Invoke(this, request);

            return request.Commands;
        }

        #endregion

        #region Transactions

        /// <summary>
        ///     Runs <paramref name="work" /> in a store transaction tagged with this surface's diagram id, and
        ///     commits it.
        /// </summary>
        /// <param name="transactionName">Name of the transaction, which is what the undo stack shows.</param>
        /// <param name="work">The mutation to perform.</param>
        /// <remarks>
        ///     Tagging the transaction with <see cref="EfiTransactionOriginator.TransactionOriginatorDiagramId" />
        ///     is what tells the model layer which diagram a change came from. Forgetting it is silent, so every
        ///     mutation on this surface goes through here rather than opening its own transaction.
        /// </remarks>
        /// <example>
        ///     <code>
        ///     InDiagramTransaction(EntityDesignerRes.Tx_LayoutDiagram, () => AutoLayoutShapeElements(shapes));
        ///     </code>
        /// </example>
        internal void InDiagramTransaction(string transactionName, Action work)
        {
            if (work is null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            InDiagramTransactionCore(
                transactionName,
                _ =>
                {
                    work();

                    return true;
                });
        }

        /// <summary>
        ///     Runs <paramref name="work" /> in a store transaction tagged with this surface's diagram id, and
        ///     commits it only if <paramref name="work" /> reports that something changed.
        /// </summary>
        /// <param name="transactionName">Name of the transaction, which is what the undo stack shows.</param>
        /// <param name="work">The mutation to perform. Return false to roll back instead of committing.</param>
        /// <remarks>
        ///     Committing a transaction that changed nothing still pushes an entry onto the undo stack, so
        ///     callers that may find no work to do should return false rather than commit unconditionally.
        /// </remarks>
        internal void InDiagramTransaction(string transactionName, Func<bool> work)
        {
            if (work is null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            InDiagramTransactionCore(transactionName, _ => work());
        }

        /// <summary>
        ///     Applies a single view model change in a store transaction tagged with this surface's diagram id.
        /// </summary>
        /// <param name="transactionName">Name of the transaction, which is what the undo stack shows.</param>
        /// <param name="change">The change to enlist.</param>
        /// <remarks>
        ///     The change is enlisted in the transaction's <see cref="ViewModelChangeContext" />, which is what
        ///     replays it against the underlying EDMX when the transaction commits.
        /// </remarks>
        internal void ApplyViewModelChange(string transactionName, ViewModelChange change)
        {
            if (change is null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            InDiagramTransactionCore(
                transactionName,
                transaction =>
                {
                    ViewModelChangeContext.GetNewOrExistingContext(transaction).ViewModelChanges.Add(change);

                    return true;
                });
        }

        /// <summary>
        ///     Opens a store transaction tagged with this surface's diagram id and commits it when
        ///     <paramref name="work" /> returns true.
        /// </summary>
        /// <param name="transactionName">Name of the transaction.</param>
        /// <param name="work">Receives the open transaction; returns whether to commit.</param>
        private void InDiagramTransactionCore(string transactionName, Func<Transaction, bool> work)
        {
            using (var transaction = Store.TransactionManager.BeginTransaction(transactionName))
            {
                transaction.Context.Add(EfiTransactionOriginator.TransactionOriginatorDiagramId, DiagramId);

                if (work(transaction))
                {
                    transaction.Commit();
                }
            }
        }

        #endregion

        #region Drag & Drop support

        /// <summary>
        ///     The event handler when objects are dropped to the designer.
        ///     If clipboard object is not null, we will go through the objects in the clipboard object and create appropriate Escher objects.
        /// </summary>
        /// <param name="e"></param>
        public override void OnDragDrop(DiagramDragEventArgs e)
        {
            try
            {
                Arranger.Start(e.IsDropLocationUserSpecified ? e.MousePosition : PointD.Empty);
                if (_clipboardObjects != null)
                {
                    ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        ModelElement.EditingContext, EfiTransactionOriginator.EntityDesignerOriginatorId, EntityDesignerRes.Tx_DropItems);
                    CommandProcessor.InvokeSingleCommand(
                        cpc, 
                        new CreateDiagramItemForEFElementsCommand(
                            GetEFElementNotInDiagramFromClipboardObject(_clipboardObjects),
                            modelDiagram, e.Shift || e.Control));
                }
                else
                {
                    base.OnDragDrop(e);
                }
            }
            finally
            {
                _clipboardObjects = null;
                Arranger.End();
                if (e.IsDropLocationUserSpecified == false)
                {
                    EnsureSelectionVisible();
                }
            }
        }

        /// <summary>
        ///     The even handler when ojbects are dragged over the designer.
        /// </summary>
        /// <param name="e"></param>
        public override void OnDragEnter(DiagramDragEventArgs e)
        {
            // Depending on type of objects are being dragged, display the appropriate icons.
            _clipboardObjects = GetClipboardObjectFromDragEventArgs(e);
            if (_clipboardObjects != null)
            {
                e.Effect = GetDropEffects();
                e.Handled = true;
            }
            else
            {
                base.OnDragEnter(e);
            }
        }

        /// <summary>
        ///     OnDragLeave ensure that the _clipboardObject is cleared.
        /// </summary>
        /// <param name="e"></param>
        public override void OnDragLeave(DiagramPointEventArgs e)
        {
            _clipboardObjects = null;
            base.OnDragLeave(e);
        }

        /// <summary>
        ///     Handler when EFObjects are dragged over designer.
        ///     Set the appropriate drag effect.
        /// </summary>
        /// <param name="e"></param>
        public override void OnDragOver(DiagramDragEventArgs e)
        {
            e.Effect = GetDropEffects();
            if (e.Effect != DragDropEffects.None)
            {
                e.Handled = true;
            }
            else
            {
                base.OnDragOver(e);
            }
        }

        /// <summary>
        ///     Return DragDropEffects.Move if there are EFObjects should be represented in diagram but is not.
        /// </summary>
        /// <returns></returns>
        private DragDropEffects GetDropEffects()
        {
            if (_clipboardObjects != null)
            {
                if (GetEFElementNotInDiagramFromClipboardObject(_clipboardObjects).Any())
                {
                    return DragDropEffects.Move;
                }
            }
            return DragDropEffects.None;
        }

        /// <summary>
        ///     Retrieves the instance of EntitiesClipboardFormat from ClipboardData object.
        ///     return null if none exists.
        /// </summary>
        private static EntitiesClipboardFormat GetClipboardObjectFromDragEventArgs(DiagramDragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(EntitiesClipboardFormat).Name, false))
            {
                return e.Data.GetData(typeof(EntitiesClipboardFormat).Name, false) as EntitiesClipboardFormat;
            }
            return null;
        }

        /// <summary>
        ///     Loop through Clipboard objects and determine whether there are EFObject that should be in the diagram but are not.
        /// </summary>
        /// <param name="clipboardObjects"></param>
        /// <returns></returns>
        private IList<EFElement> GetEFElementNotInDiagramFromClipboardObject(EntitiesClipboardFormat clipboardObjects)
        {
            ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
            var artifactSet = modelDiagram.Artifact.ArtifactSet;

            IList<EFElement> efElements = [];

            // We are only interested in entity-types all the associations will be automatically created when entity-types are added to diagram.
            foreach (var clipboardObject in clipboardObjects.ClipboardEntities)
            {
                var efElement = artifactSet.LookupSymbol(clipboardObject.NormalizedName);
                if (efElement != null
                    && modelDiagram.IsEFObjectRepresentedInDiagram(efElement) == false)
                {
                    efElements.Add(efElement);
                }
            }

            return efElements;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public bool DisplayNameAndType
        {
            get { return _displayNameAndType; }
            set
            {
                _displayNameAndType = value;
                Invalidate(true);
                if (!ModelUtils.IsSerializing(Store))
                {
                    PersistDisplayType();
                }
            }
        }

        public new EntityDesignerViewModel ModelElement
        {
            get { return base.ModelElement as EntityDesignerViewModel; }
            set { base.ModelElement = value; }
        }

        /// <summary>
        ///     Returns the model associated with this diagram
        /// </summary>
        internal EntityDesignerViewModel GetModel()
        {
            return ModelElement;
        }

        /// <summary>
        ///     Returns true if this is an empty diagram, false otherwise
        /// </summary>
        /// <param name="diagram"></param>
        /// <returns></returns>
        internal static bool IsEmptyDiagram(EntityDesignerSurface diagram)
        {
            return (diagram != null) && (diagram.NestedChildShapes.Count == 0);
        }

        protected override void InitializeInstanceResources()
        {
            base.InitializeInstanceResources();
            ShowGrid = false;
            SnapToGrid = true;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            IsInitialized = true;
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Selects the shape closest to the origin, or the selection a host asked for.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Does nothing until there is a view to select in, which is why a host has to choose the moment
        ///         to call this: at <see cref="OnInitialize" /> the document is still loading and no view exists
        ///         yet. Scheduling that wait means a dispatcher, which is the host's to own — see
        ///         specs/layer-map.md.
        ///     </para>
        ///     <para>
        ///         Safe to call more than once. Once <see cref="InitialDiagramItemSelection" /> has been applied
        ///         it is cleared, so later calls fall back to the nearest-the-origin shape.
        ///     </para>
        /// </remarks>
        internal void SetInitialSelection()
        {
            if (ActiveDiagramView != null)
            {
                if (InitialDiagramItemSelection == null)
                {
                    // seed our "hypotenuse" with the length from upper-left to the bottom-right corner of the canvas rectangle
                    // (we don't need to Sqrt on the result since we are just comparing them, not interested in actual length)
                    var clientRectangle = ActiveDiagramView.ClientRectangle;
                    double hyp = ((clientRectangle.Width * clientRectangle.Width) + (clientRectangle.Height * clientRectangle.Height));

                    // find the entity closest to the origin
                    EntityTypeShape upperLeft = null;
                    foreach (var shape in NestedChildShapes)
                    {
                        if (shape is EntityTypeShape entityShape)
                        {
                            // calculate the distance from 0,0 to the shape's upper left corner
                            // (we don't need to Sqrt on the result since we are just comparing them, not interested in actual length)
                            var entityBounds = entityShape.AbsoluteBounds;
                            var entityHyp = ((entityBounds.X * entityBounds.X) + (entityBounds.Y * entityBounds.Y));

                            // if this is closer to the origin, remember it
                            if (entityHyp < hyp)
                            {
                                upperLeft = entityShape;
                                hyp = entityHyp;
                            }
                        }
                    }

                    // if we found one, select it and move it into view
                    if (upperLeft != null)
                    {
                        ActiveDiagramView.Focus();
                        DiagramItem selectMe = new DiagramItem(upperLeft);
                        ActiveDiagramView.Selection.Set(selectMe);
                        EnsureSelectionVisible();
                    }
                }
                else
                {
                    ActiveDiagramView.Focus();
                    ActiveDiagramView.Selection.Set(InitialDiagramItemSelection);
                    EnsureSelectionVisible();
                    InitialDiagramItemSelection = null;
                }
            }
        }

        // disable the default auto placement, since this is handled
        // by the AutoArrangeHelper class.
        public override bool ShouldAutoPlaceChildShapes
        {
            get { return false; }
        }

        /// <summary>
        ///     This is called before a transaction has committed,
        ///     This tests to see if the transaction was a drag & drop, and if
        ///     so the new elements can be auto-arranged before the transaction has finished.
        ///     See Also: ShapeAddedToDiagramRule
        /// </summary>
        public override void OnTransactionCommitting(TransactionCommitEventArgs e)
        {
            base.OnTransactionCommitting(e);

            Arranger.TransactionCommit(this);
        }

        internal void EnsureSelectionVisible()
        {
            if (ActiveDiagramView != null
                && ActiveDiagramView.DiagramClientView != null)
            {
                // ActiveDiagramView.Selection.EnsureVisible() method does not work correctly for straight Connector shapes, so use this instead
                ActiveDiagramView.DiagramClientView.EnsureVisible(ActiveDiagramView.Selection.BoundingBox);
            }
        }

        #region Expand/Collapse

        /// <summary>
        ///     Collapse the passed in entity type shape.
        /// </summary>
        /// <param name="entityTypeShape">Entity type shape to be collapsed.</param>
        internal void CollapseEntityTypeShape(EntityTypeShape entityTypeShape)
        {
            SetEntityShapesExpanded(new[] { entityTypeShape }, false);
        }

        /// <summary>
        ///     Collapse all entity type shapes in the diagram.
        /// </summary>
        internal void CollapseAllEntityTypeShapes()
        {
            SetEntityShapesExpanded(NestedChildShapes, false);
        }

        /// <summary>
        ///     Expand the passed in entity type shape.
        /// </summary>
        /// <param name="entityTypeShape"></param>
        internal void ExpandEntityTypeShape(EntityTypeShape entityTypeShape)
        {
            SetEntityShapesExpanded(new[] { entityTypeShape }, true);
        }

        /// <summary>
        ///     Expand all entity type shapes in the diagram.
        /// </summary>
        internal void ExpandAllEntityTypeShapes()
        {
            SetEntityShapesExpanded(NestedChildShapes, true);
        }

        /// <summary>
        ///     Helper function to expand or collapse entity type shape.
        /// </summary>
        /// <param name="shapeElements">Shape elements collection.</param>
        /// <param name="isExpanded">a flag that indicates whether the shape to be expanded or not.</param>
        private void SetEntityShapesExpanded(IList<ShapeElement> shapeElements, bool isExpanded)
        {
            // Put up an hourglass because this may take a while
            using (BeginLongOperation())
            {
                InDiagramTransaction(
                    EntityDesignerRes.Tx_SetEntityTypeIsExpandedProperty,
                    () =>
                    {
                        var entityShapeChanged = false;

                        foreach (var shape in shapeElements)
                        {
                            if (shape is EntityTypeShape entityShape)
                            {
                                if (isExpanded != entityShape.IsExpanded)
                                {
                                    entityShape.IsExpanded = isExpanded;
                                    entityShapeChanged = true;
                                }
                            }
                        }

                        // only commit if there is a change.
                        return entityShapeChanged;
                    });
            }
        }

        #endregion

        #region Watermark

        /// <remarks>
        ///     The designer chooses from what it can see in the model, then gives the host the chance to say
        ///     something it knows better — see <see cref="WatermarkTextRequested" />.
        /// </remarks>
        public override string WatermarkText
        {
            get
            {
                var request = new WatermarkTextRequestedEventArgs(GetModelWatermarkText());
                WatermarkTextRequested?.Invoke(this, request);

                return request.Text;
            }
        }

        /// <summary>
        ///     Chooses the watermark from the state of the model alone.
        /// </summary>
        /// <returns>The watermark text, with escaped newlines expanded.</returns>
        private string GetModelWatermarkText()
        {
            if (GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact is EntityDesignArtifact artifact)
            {
                if (!artifact.IsStructurallySafe)
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        EntityDesignerRes.DesignerWatermarkSafeModeErrorText,
                        EntityDesignerRes.DesignerWatermarkXmlEditorLink).Replace(@"\n", "\n");
                }

                // if the schema version in the document is not valid for the current target framework.
                if (!artifact.IsVersionSafe)
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        EntityDesignerRes.DesignerWatermarkUpgradeErrorText,
                        EntityDesignerRes.DesignerWatermarkXmlEditorLink,
                        EntityDesignerRes.DesignerWatermarkUpgradeLink).Replace(@"\n", "\n");
                }
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                EntityDesignerRes.DesignerWatermarkText,
                EntityDesignerRes.DesignerWatermarkToolboxLink,
                EntityDesignerRes.DesignerWatermarkModelBrowserLink).Replace(@"\n", "\n");
        }

        protected override void OnAssociated(DiagramAssociationEventArgs e)
        {
            base.OnAssociated(e);

            // Property icons are supplied by the host along with the palette; a host that paints nothing —
            // the command line renderer — supplies none and the shapes draw without them.

            // Initialize and add LinkLabels to diagram watermark
            if (e.DiagramView != null)
            {
                // OnAssociated gets called when the document gets loaded as well as reloaded. This means that
                // we need to reset the watermark before we refresh the link labels associated with them.
                // Resetting the watermark does the appropriate steps to check if the artifact is designer safe, etc.
                // to determine what watermark to display.
                if (e.DiagramView.Watermark != null)
                {
                    ResetWatermark(e.DiagramView);
                }

                Debug.Assert(e.DiagramView.Selection != null, "Why DiagramView's Selection property is null?");
                e.DiagramView.Selection?.ShapeSelectionChanged += diagramView_OnShapeSelectionChanged;
            }
        }

        /// <summary>
        ///     Unsubscribe to the shape selection changed event when view is about to be disposed.
        /// </summary>
        protected override void OnDisassociated(DiagramAssociationEventArgs e)
        {
            if (e?.DiagramView?.Selection is not null)
            {
                e.DiagramView.Selection.ShapeSelectionChanged -= diagramView_OnShapeSelectionChanged;
            }
            base.OnDisassociated(e);
        }

        /// <summary>
        ///     Event Handler when selections in the Diagram are changed.
        ///     When the selection changes in diagram, we build the list of the diagram items that will be emphasized and we ask the diagram item's shapes to be redrawn.
        ///     When the shape is redrawn, each shape determines whether it is in the list; if yes, it will draw the emphasis shape.
        /// </summary>
        private void diagramView_OnShapeSelectionChanged(object sender, EventArgs e)
        {
            if (ActiveDiagramView?.Selection is not null)
            {
                DiagramItemCollection shapesToBeEmphasized = new DiagramItemCollection();
                var selectedShapes = ActiveDiagramView.Selection;

                // For each DiagramItem in the Selection
                // - Get the corresponding model element.
                // - See if model element implement IContainRelatedElementsToEmphasizeWhenSelected.
                // - Get related model elements.
                // - For each related model elements, instantiates DiagramItem for the ModelElement.
                foreach (DiagramItem diagramItem in selectedShapes)
                {
                    IContainRelatedElementsToEmphasizeWhenSelected relatedModelElementToEmphasize = null;
                    if (diagramItem.Shape != null
                        && diagramItem.Shape.ModelElement != null)
                    {
                        relatedModelElementToEmphasize = diagramItem.Shape.ModelElement as IContainRelatedElementsToEmphasizeWhenSelected;
                    }
                    else
                    {
                        // DiagramItem.Shape.ModelElement is null when the selected item is not a ShapeElement(for example: Property/NavigationProperty).
                        // Fortunately, we can retrieve the information from RepresentedElements property.
                        relatedModelElementToEmphasize =
                            diagramItem.RepresentedElements.OfType<ModelElement>().FirstOrDefault() as
                            IContainRelatedElementsToEmphasizeWhenSelected;
                    }

                    if (relatedModelElementToEmphasize != null)
                    {
                        // For each ModelElement get the corresponding diagram item.
                        foreach (var emphasizedModelElement in relatedModelElementToEmphasize.RelatedElementsToEmphasizeOnSelected)
                        {
                            DiagramItem emphasizedDiagramItem = null;

                            // if ModelElement is a Property, we could not just instantiate a DiagramItem and pass in the property's PresentationElement to the constructor
                            // since property's PresentationElement is not a ShapeElement.
                            if (emphasizedModelElement is ViewModelPropertyBase propertyBase)
                            {
                                ViewModelEntityType et = null;
                                if (propertyBase is ViewModelNavigationProperty navigationProperty)
                                {
                                    et = navigationProperty.EntityType;
                                }
                                else if (propertyBase is ViewModelProperty property)
                                {
                                    et = property.EntityType;
                                }
                                else
                                {
                                    Debug.Fail("Unexpected property type. Type name:" + propertyBase.GetType().Name);
                                }

                                Debug.Assert(et != null, "Could not get EntityType for property: " + propertyBase.Name);
                                if (et != null)
                                {
                                    Debug.Assert(
                                        PresentationViewsSubject.GetPresentation(et).Count() <= 1,
                                        "There should be at most 1 EntityTypeShape for EntityType:" + et.Name);
                                    EntityTypeShape ets = PresentationViewsSubject.GetPresentation(et).FirstOrDefault() as EntityTypeShape;
                                    emphasizedDiagramItem = ets.GetDiagramItemForProperty(propertyBase);
                                }
                            }
                            else
                            {
                                var relatedPresentationElementToEmphasize =
                                    PresentationViewsSubject.GetPresentation(emphasizedModelElement).FirstOrDefault();
                                if (relatedPresentationElementToEmphasize != null)
                                {
                                    if (relatedPresentationElementToEmphasize is ShapeElement relatedShapeElementToEmphasize)
                                    {
                                        emphasizedDiagramItem = new DiagramItem(relatedShapeElementToEmphasize);
                                    }
                                }
                            }

                            // Only add if the DiagramItem hasn't been added to the list and the diagram item is not a member of selected diagram item list.
                            if (emphasizedDiagramItem != null
                                && shapesToBeEmphasized.Contains(emphasizedDiagramItem) == false
                                && selectedShapes.Contains(emphasizedDiagramItem) == false)
                            {
                                shapesToBeEmphasized.Add(emphasizedDiagramItem);
                            }
                        }
                    }
                }
                EmphasizedShapes.Set(shapesToBeEmphasized);
            }
        }

        internal void ResetWatermark(DiagramView diagramView)
        {
            // The way the DSL code is implemented, we need to set HasWatermark to false and then to true to get the watermark text to change.
            // if we are in the middle of creating diagram, ActiveDiagramView will be null. 
            if (diagramView is not null)
            {
                diagramView.HasWatermark = false;
                diagramView.HasWatermark = true;

                RefreshWatermarkLinks(diagramView);
            }
        }

        /// <summary>
        ///     Asks the host to attach the clickable links for <paramref name="diagramView" />'s watermark.
        /// </summary>
        /// <param name="diagramView">The view whose watermark was rebuilt.</param>
        /// <remarks>
        ///     Every link the watermark offers opens a window or retargets the document, so the host owns all of
        ///     them. With nothing subscribed the watermark is plain text. See specs/layer-map.md.
        /// </remarks>
        internal void RefreshWatermarkLinks(DiagramView diagramView)
        {
            if (diagramView?.Watermark is null)
            {
                return;
            }

            WatermarkLinksRequested?.Invoke(this, new WatermarkLinksRequestedEventArgs(diagramView));
        }

        #endregion Watermark

        #region Zoom & Layout

        /// <summary>
        ///     Performs an AutoLayoutDiagram command on all child shapes
        /// </summary>
        public void AutoLayoutDiagram()
        {
            AutoLayoutDiagram(NestedChildShapes);
        }

        /// <summary>
        ///     Arranges the given shapes using the current layout engine.
        /// </summary>
        /// <param name="shapes">The shapes to arrange.</param>
        /// <remarks>
        ///     The surface does not know how the arranging is done. Which engine runs is
        ///     <see cref="LayoutEngineManager.Current" />, moved by the designer's Advanced Layout toggle.
        ///     See specs/diagram-layout-engines.md.
        /// </remarks>
        public void AutoLayoutDiagram(IList shapes)
        {
            LayoutEngines.Current.Layout(this, shapes);
        }

        /// <summary>
        ///     Zooms the diagram to fit all the contents
        /// </summary>
        public void ZoomToFit()
        {
            ActiveDiagramView.ZoomToFit();
        }

        /// <summary>
        ///     ZoomIn
        /// </summary>
        public void ZoomIn()
        {
            ActiveDiagramView.ZoomIn();
        }

        /// <summary>
        ///     ZoomOut
        /// </summary>
        public void ZoomOut()
        {
            ActiveDiagramView.ZoomOut();
        }

        /// <summary>
        ///     Gets/Sets the zoom level in percent values
        /// </summary>
        public int ZoomLevel
        {
            get
            {
                if (ActiveDiagramView != null)
                {
                    return (int)(ActiveDiagramView.ZoomFactor * 100);
                }
                else
                {
                    return undefinedZoomLevel;
                }
            }
            set
            {
                ActiveDiagramView?.ZoomAtViewCenter((float)value / 100);
            }
        }

        #endregion Zoom & Layout

        // Accessibility name of the diagram will be the schema's namespace
        public override string AccessibleName
        {
            get
            {
                if (ModelElement == null)
                {
                    return base.AccessibleName;
                }

                return ModelElement.Namespace;
            }
        }

        public override string AccessibleDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_EntityDesignerViewModel,
                    ModelElement.GetType().Name);
            }
        }

        internal void AddNewEntityType(PointD dropPoint)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            ConceptualEntityModel model = ModelElement.ModelXRef.GetExisting(ModelElement) as ConceptualEntityModel;
            Debug.Assert(model != null);

            var request = new NewEntityTypeRequestedEventArgs(model);
            NewEntityTypeRequested?.Invoke(this, request);

            if (request.Cancelled)
            {
                return;
            }

            try
            {
                Arranger.Start(dropPoint);
                Store.RuleManager.DisableRule(typeof(EntityType_AddRule));

                ApplyViewModelChange(EntityDesignerRes.Tx_AddEntityType, new EntityTypeAddFromRequest(request));
            }
            finally
            {
                Store.RuleManager.EnableRule(typeof(EntityType_AddRule));
                Arranger.End();
                EnsureSelectionVisible();
            }
        }

        /// <summary>
        ///     Creates new association for given EntityTypeShape
        /// </summary>
        internal void AddNewAssociation(EntityTypeShape entityShape)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            // If user selected an EntityTypeShape, use corresponding EntityType
            // else default to first EntityType in the model
            ViewModelEntityType end1 = null;
            if (entityShape != null)
            {
                end1 = entityShape.ModelElement as ViewModelEntityType;
            }
            end1 ??= ModelElement.EntityTypes[0];

            // Pick something for the second end (defaulting to the next EntityType in the model)
            ViewModelEntityType end2 = null;
            var index = ModelElement.EntityTypes.IndexOf(end1) + 1;
            if (ModelElement.EntityTypes.Count <= index)
            {
                index = 0;
            }
            end2 = ModelElement.EntityTypes[index];

            Edmx.Entity.EntityType modelEnd1Entity = ModelElement.ModelXRef.GetExisting(end1) as Edmx.Entity.EntityType;
            Edmx.Entity.EntityType modelEnd2Entity = ModelElement.ModelXRef.GetExisting(end2) as Edmx.Entity.EntityType;
            Debug.Assert(modelEnd1Entity != null && modelEnd2Entity != null);
            ConceptualEntityModel model = modelEnd1Entity.Parent as ConceptualEntityModel;
            Debug.Assert(model != null);

            var request = new NewAssociationRequestedEventArgs(model.EntityTypes(), modelEnd1Entity, modelEnd2Entity);
            NewAssociationRequested?.Invoke(this, request);

            if (!request.Cancelled)
            {
                try
                {
                    Store.RuleManager.DisableRule(typeof(Association_AddRule));
                    ApplyViewModelChange(EntityDesignerRes.Tx_AddAssociation, new AssociationAddFromRequest(request));
                }
                finally
                {
                    Store.RuleManager.EnableRule(typeof(Association_AddRule));
                }
            }
        }

        /// <summary>
        ///     Creates new inheritance for given EntityTypeShape
        /// </summary>
        /// <param name="entityShape"></param>
        /// <returns></returns>
        internal void AddNewInheritance(EntityTypeShape entityShape)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            // If user selected an EntityTypeShape, use corresponding EntityType
            // else default to first EntityType in the model
            ViewModelEntityType baseEntity = null;
            ConceptualEntityType modelEntity = null;
            if (entityShape != null)
            {
                baseEntity = entityShape.ModelElement as ViewModelEntityType;
                modelEntity = ModelElement.ModelXRef.GetExisting(baseEntity) as ConceptualEntityType;
                Debug.Assert(modelEntity != null);
            }

            ConceptualEntityModel model = ModelElement.ModelXRef.GetExisting(ModelElement) as ConceptualEntityModel;
            Debug.Assert(model != null);

            List<ConceptualEntityType> cets = new List<ConceptualEntityType>(model.EntityTypes().Cast<ConceptualEntityType>());

            var request = new NewInheritanceRequestedEventArgs(cets, modelEntity);
            NewInheritanceRequested?.Invoke(this, request);

            if (!request.Cancelled)
            {
                ApplyViewModelChange(EntityDesignerRes.Tx_AddInheritance, new InheritanceAddFromRequest(request, this));
            }
        }

        /// <summary>
        ///     Creates a function import; if there is a selected entity type, it will use that for the return type.
        /// </summary>
        public void AddNewFunctionImport(EntityTypeShape entityShape)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            ViewModelEntityType baseEntity = null;
            Edmx.Entity.EntityType modelEntity = null;
            if (entityShape != null)
            {
                baseEntity = entityShape.TypedModelElement;
                Debug.Assert(baseEntity != null);
                modelEntity = ModelElement.ModelXRef.GetExisting(baseEntity) as Edmx.Entity.EntityType;
                Debug.Assert(modelEntity != null);
            }

            // get the necessary conceptual model elements
            ConceptualEntityModel cModel = ModelElement.ModelXRef.GetExisting(ModelElement) as ConceptualEntityModel;
            Debug.Assert(cModel != null, "Could not find a conceptual entity model associated with this artifact");
            ConceptualEntityContainer cContainer = null;
            if (cModel != null)
            {
                cContainer = cModel.FirstEntityContainer as ConceptualEntityContainer;
                Debug.Assert(cContainer != null, "There is no conceptual entity container in the conceptual entity model");
            }

            // get the necessary storage model elements and mapping model elements
            var artifactService = ModelElement.EditingContext.GetEFArtifactService();
            Debug.Assert(
                artifactService != null && artifactService.Artifact != null,
                "There is no artifact service/artifact associated with this editing context");
            StorageEntityModel sModel = null;
            EntityContainerMapping ecMapping = null;
            if (artifactService != null
                && artifactService.Artifact != null)
            {
                sModel = artifactService.Artifact.StorageModel();
                Debug.Assert(sModel != null, "Could not find a storage entity model associated with this artifact");
                var mModel = artifactService.Artifact.MappingModel();
                Debug.Assert(mModel != null, "Could not find a mapping model associated with this artifact");
                if (mModel != null)
                {
                    ecMapping = mModel.FirstEntityContainerMapping;
                    Debug.Assert(
                        ecMapping != null,
                        "There must be an entity container mapping in the mapping model to create the function import mapping");
                }
            }

            if (cModel != null
                && sModel != null
                && cContainer != null
                && ecMapping != null)
            {
                var schemaVersion = artifactService.Artifact.SchemaVersion;
                if (null == schemaVersion)
                {
                    Debug.Assert(
                        false,
                        typeof(EntityDesignerSurface).Name + " could not determine Version for path "
                        + artifactService.Artifact.Uri.LocalPath);
                    return;
                }

                NewFunctionImportRequested?.Invoke(
                    this,
                    new NewFunctionImportRequestedEventArgs(
                        ModelElement.EditingContext, artifactService.Artifact, sModel, cModel, cContainer, modelEntity));
            }
        }

        internal void PersistZoomLevel()
        {
            // make sure that we have ActiveDiagramView to get the current ZoomLevel from
            if (ActiveDiagramView != null
                && !ModelUtils.IsSerializing(Store))
            {
                ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
                Debug.Assert(modelDiagram != null, "Model Diagram should be present");
                if (modelDiagram != null)
                {
                    if (undefinedZoomLevel != ZoomLevel
                        && modelDiagram.ZoomLevel.Value != ZoomLevel)
                    {
                        CommandProcessorContext cpc = new CommandProcessorContext(
                            ModelElement.EditingContext, EfiTransactionOriginator.EntityDesignerOriginatorId, EntityDesignerRes.Tx_SetZoom);
                        UpdateDefaultableValueCommand<int> cmd = new UpdateDefaultableValueCommand<int>(modelDiagram.ZoomLevel, ZoomLevel);
                        CommandProcessor.InvokeSingleCommand(cpc, cmd);
                    }
                }
            }
        }

        internal void PersistShowGrid()
        {
            ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
            Debug.Assert(modelDiagram != null, "Model Diagram should be present");
            if (modelDiagram != null)
            {
                if (modelDiagram.ShowGrid.Value != ShowGrid)
                {
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        ModelElement.EditingContext, EfiTransactionOriginator.EntityDesignerOriginatorId, EntityDesignerRes.Tx_SetGridVisibility);
                    UpdateDefaultableValueCommand<bool> cmd = new UpdateDefaultableValueCommand<bool>(modelDiagram.ShowGrid, ShowGrid);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        internal void PersistSnapToGrid()
        {
            ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
            Debug.Assert(modelDiagram != null, "Model Diagram should be present");
            if (modelDiagram != null)
            {
                if (modelDiagram.SnapToGrid.Value != SnapToGrid)
                {
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        ModelElement.EditingContext, EfiTransactionOriginator.EntityDesignerOriginatorId, EntityDesignerRes.Tx_SetSnapToGrid);
                    UpdateDefaultableValueCommand<bool> cmd = new UpdateDefaultableValueCommand<bool>(modelDiagram.SnapToGrid, SnapToGrid);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        internal void PersistDisplayType()
        {
            ModelDiagram modelDiagram = ModelElement.ModelXRef.GetExisting(this) as ModelDiagram;
            Debug.Assert(modelDiagram != null, "Model Diagram should be present");
            if (modelDiagram != null)
            {
                if (modelDiagram.DisplayType.Value != DisplayNameAndType)
                {
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        ModelElement.EditingContext, EfiTransactionOriginator.EntityDesignerOriginatorId, EntityDesignerRes.Tx_SetMemberFormatValue);
                    UpdateDefaultableValueCommand<bool> cmd = new UpdateDefaultableValueCommand<bool>(modelDiagram.DisplayType, DisplayNameAndType);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        public override DiagramSelectionRules SelectionRules
        {
            get
            {
                return new EntityDesignerSurfaceSelectionRules(this);
            }
        }

        public class EntityDesignerSurfaceSelectionRules : DiagramSelectionRules
        {
            private readonly EntityDesignerSurface _diagram;

            public EntityDesignerSurfaceSelectionRules(EntityDesignerSurface diagram)
            {
                _diagram = diagram;
            }

            /// <summary>
            ///     Called by the design surface to allow selection filtering
            /// </summary>
            /// <param name="currentSelection">[in] The current selection before any ShapeElements are added or removed.</param>
            /// <param name="proposedItemsToAdd">[in/out] The proposed DiagramItems to be added to the selection.</param>
            /// <param name="proposedItemsToRemove">[in/out] The proposed DiagramItems to be removed from the selection.</param>
            /// <param name="primaryItem">
            ///     [in/out] The proposed DiagramItem to become the primary DiagramItem of the selection.
            ///     A null value signifies that the last DiagramItem in the resultant selection should be assumed as the
            ///     primary DiagramItem.
            /// </param>
            /// <returns>
            ///     true if some or all of the selection was accepted; false if the entire selection proposal
            ///     was rejected. If false, appropriate feedback will be given to the user to indicate that the
            ///     selection was rejected.
            /// </returns>
            public override bool GetCompliantSelection(
                SelectedShapesCollection currentSelection, DiagramItemCollection proposedItemsToAdd,
                DiagramItemCollection proposedItemsToRemove, DiagramItem primaryItem)
            {
                base.GetCompliantSelection(currentSelection, proposedItemsToAdd, proposedItemsToRemove, primaryItem);

                DiagramItem[] originalProposedItemsToAdd = new DiagramItem[proposedItemsToAdd.Count];
                proposedItemsToAdd.CopyTo(originalProposedItemsToAdd, 0);

                // we only perform this with selection rectangles, in which case the focused item will be the diagram 
                // and the user clicks on the "Properties" or "Navigation Properties" compartment on an entity
                if (currentSelection.FocusedItem != null
                    && currentSelection.FocusedItem.Shape == _diagram)
                {
                    foreach (var item in originalProposedItemsToAdd)
                    {
                        if (item.Shape != null
                            && item.Shape is ElementListCompartment)
                        {
                            // only perform this if we have selected the "Scalar Properties" ListCompartment or
                            // the "Navigation Properties" ListCompartment
                            ElementListCompartment elListCompartment = item.Shape as ElementListCompartment;
                            if (elListCompartment.DefaultCreationDomainClass.Id == ViewModelProperty.DomainClassId
                                || elListCompartment.DefaultCreationDomainClass.Id == ViewModelNavigationProperty.DomainClassId)
                            {
                                // we don't perform this if a Property or NavigationProperty was selected
                                var representedShapeElements = item.RepresentedElements.OfType<ShapeElement>();
                                if (representedShapeElements.Count() == 1
                                    && representedShapeElements.Contains(item.Shape))
                                {
                                    // find the parent EntityTypeShape that houses this ListCompartment
                                    EntityTypeShape entityTypeShape = elListCompartment.ParentShape as EntityTypeShape;
                                    Debug.Assert(
                                        entityTypeShape != null, "Why isn't the parent of the list compartment an EntityTypeShape?");
                                    DiagramItem entityTypeShapeDiagramItem = new DiagramItem(entityTypeShape);

                                    // add the parent EntityTypeShape if it doesn't already exist in the collection
                                    if (!currentSelection.Contains(entityTypeShapeDiagramItem)
                                        && proposedItemsToAdd.Contains(entityTypeShapeDiagramItem) == false)
                                    {
                                        proposedItemsToAdd.Add(entityTypeShapeDiagramItem);
                                    }

                                    proposedItemsToAdd.Remove(item);
                                }
                            }
                        }
                    }
                }

                // The code below is responsible to enforce the following diagram items selection rule. (see Multiple Diagram spec).
                // - if an entity-type-shape is selected, no diagram-item can be selected in the diagram.
                // - if diagram-item that is not an entity-type-shape is selected, no entity-type-shape can be selected.
                var firstDiagramItem = FirstSelectionItem(currentSelection, proposedItemsToAdd, proposedItemsToRemove);
                if (firstDiagramItem != null
                    && proposedItemsToAdd.Count > 0)
                {
                    var isFirstDiagramItemEntityTypeShape = firstDiagramItem.Shape is EntityTypeShape;

                    // For Multi selection rules, we can only select EntityTypeShapes or else but not both.
                    foreach (var item in proposedItemsToAdd.ToList())
                    {
                        if (isFirstDiagramItemEntityTypeShape && ((item.Shape is EntityTypeShape) == false))
                        {
                            RemoveSelectedDiagramItem(item, currentSelection, proposedItemsToAdd, proposedItemsToRemove);
                        }
                        else if (isFirstDiagramItemEntityTypeShape == false
                                 && (item.Shape is EntityTypeShape))
                        {
                            RemoveSelectedDiagramItem(item, currentSelection, proposedItemsToAdd, proposedItemsToRemove);
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        ///     Helper method to cancel a diagram item to be selected.
        /// </summary>
        /// <param name="diagramItem"></param>
        /// <param name="currentSelection"></param>
        /// <param name="proposedItemsToAdd"></param>
        /// <param name="proposedItemsToRemove"></param>
        private static void RemoveSelectedDiagramItem(
            DiagramItem diagramItem, SelectedShapesCollection currentSelection, DiagramItemCollection proposedItemsToAdd,
            DiagramItemCollection proposedItemsToRemove)
        {
            if (currentSelection.Contains(diagramItem) == false
                && proposedItemsToAdd.Contains(diagramItem))
            {
                proposedItemsToAdd.Remove(diagramItem);
            }
            if (currentSelection.Contains(diagramItem)
                && proposedItemsToRemove.Contains(diagramItem) == false)
            {
                proposedItemsToRemove.Add(diagramItem);
            }
        }

        /// <summary>
        ///     Returns the first selection item in the provided selection.
        /// </summary>
        /// <param name="currentSelection"></param>
        /// <param name="proposedShapesToAdd"></param>
        /// <param name="proposedShapesToRemove"></param>
        /// <returns></returns>
        private static DiagramItem FirstSelectionItem(
            SelectedShapesCollection currentSelection, DiagramItemCollection proposedShapesToAdd,
            DiagramItemCollection proposedShapesToRemove)
        {
            // Temporary list that stores what the selections will look like.
            DiagramItemCollection actualSelection = new DiagramItemCollection();

            // Add current selection items to the list.
            foreach (DiagramItem item in currentSelection)
            {
                if (item != null
                    && actualSelection.Contains(item) == false)
                {
                    actualSelection.Add(item);
                }
            }

            // Include additional diagram items that will be selected.
            foreach (var item in proposedShapesToAdd)
            {
                if (item != null
                    && actualSelection.Contains(item) == false)
                {
                    actualSelection.Add(item);
                }
            }

            // Remove diagram items that will be unselected.
            foreach (var item in proposedShapesToRemove)
            {
                if (item != null
                    && actualSelection.Contains(item))
                {
                    actualSelection.Remove(item);
                }
            }

            // Return the first item in the list.
            if (actualSelection.Count > 0)
            {
                return actualSelection[0];
            }
            return null;
        }

        /// <summary>
        ///     Asks the user if they would like to also delete any
        ///     storage EntitySets which will be unmapped. If the answer
        ///     is Yes, also has the side-effect of adding the list of
        ///     unmapped sets to DeleteUnmappedStorageEntitySetsProperty
        ///     in the view-model's Store PropertyBag
        /// </summary>
        /// <param name="selectedModelElements">C-side objects to be deleted</param>
        /// <returns>
        ///     Result of asking the user, or No if never asked;
        ///     possible results are Yes, No, or Cancel (No means just delete the  C-side
        ///     objects, Yes means also delete the S-side objects that will become unmapped,
        ///     Cancel means don't delete anything
        /// </returns>
        internal DialogResult ShouldDeleteUnmappedStorageEntitySets(List<EFElement> selectedModelElements)
        {
            // if there are no selected elements then return 'No'
            if (null == selectedModelElements
                || 0 >= selectedModelElements.Count)
            {
                return DialogResult.No;
            }

            // find which S-side EntitySets would be deleted
            var unmappedStorageEntitySets =
                DeleteUnmappedStorageEntitySetsCommand.UnmappedStorageEntitySetsIfDelete(selectedModelElements);

            // only offer user choice if there are any S-side EntitySets to delete
            if (0 >= unmappedStorageEntitySets.Count)
            {
                return DialogResult.No;
            }

            // ask the host to offer the user the choice: (a) delete only the
            // C-side objects selected, (b) to also delete any StorageEntitySets
            // which will end up unmapped, or (c) cancel the whole operation
            var request = new UnmappedStorageEntitySetsDeletionRequestedEventArgs(unmappedStorageEntitySets);
            UnmappedStorageEntitySetsDeletionRequested?.Invoke(this, request);

            // DeleteUnmappedSets: true = Yes, false = No, null = cancelled or unhandled
            DialogResult result;
            if (request.DeleteUnmappedSets == true)
            {
                result = DialogResult.Yes;
            }
            else if (request.DeleteUnmappedSets == false)
            {
                result = DialogResult.No;
            }
            else
            {
                result = DialogResult.Cancel;
            }

            if (DialogResult.Yes == result)
            {
                // user decided to also delete the unmapped StorageEntitySets;
                // the property below is checked in the EntityDesignerViewModel when committing the changes
                var viewModel = GetModel();
                Debug.Assert(null != viewModel, "viewModel should not be null");
                if (null != viewModel)
                {
                    List<ICollection<StorageEntitySet>> unmappedMasterList = null;
                    if (viewModel.Store.PropertyBag.ContainsKey(EntityDesignerViewModel.DeleteUnmappedStorageEntitySetsProperty))
                    {
                        unmappedMasterList =
                            viewModel.Store.PropertyBag[EntityDesignerViewModel.DeleteUnmappedStorageEntitySetsProperty] as
                            List<ICollection<StorageEntitySet>>;
                    }

                    if (unmappedMasterList == null)
                    {
                        unmappedMasterList = [];
                        viewModel.Store.PropertyBag[EntityDesignerViewModel.DeleteUnmappedStorageEntitySetsProperty] =
                            unmappedMasterList;
                    }

                    unmappedMasterList.Add(unmappedStorageEntitySets);
                }
            }

            return result;
        }

        internal void AddMissingEntityTypeShapes(EntityDesignArtifact efArtifact, out bool addedMissingShapes)
        {
            addedMissingShapes = false;
            Debug.Assert(efArtifact != null, "EFArtifact is null");
            if (efArtifact != null)
            {
                // Now, for all entitytypes, ensure that an entitytype shape is created.
                if (efArtifact.ConceptualModel() != null
                    && Diagram is EntityDesignerSurface dslDiagram)
                {
                    HashSet<Edmx.Entity.EntityType> entityTypesMaterializedAsShapes = new HashSet<Edmx.Entity.EntityType>();
                    foreach (
                        var designerEntityTypeShape in
                            efArtifact.DesignerInfo.Diagrams.Items.Where(d => d.Id.Value == DiagramId).SelectMany(d => d.EntityTypeShapes))
                    {
                        // First check if there's a ModelElement for this.
                        if (dslDiagram.ModelElement.ModelXRef.GetExisting(designerEntityTypeShape) is EntityTypeShape entityTypeShape
                            && designerEntityTypeShape.EntityType.Target != null
                            && !entityTypesMaterializedAsShapes.Contains(designerEntityTypeShape.EntityType.Target))
                        {
                            entityTypesMaterializedAsShapes.Add(designerEntityTypeShape.EntityType.Target);
                        }
                    }

                    CommandProcessorContext cpc = new CommandProcessorContext(
                        efArtifact.EditingContext,
                        EfiTransactionOriginator.EntityDesignerOriginatorId,
                        "Restore Excluded Elements");
                    CommandProcessor cp = new CommandProcessor(cpc, shouldNotifyObservers: true);

                    foreach (var entityType in efArtifact.ConceptualModel().EntityTypes())
                    {
                        if (!entityTypesMaterializedAsShapes.Contains(entityType))
                        {
                            if (dslDiagram.ModelElement.ModelXRef.GetExisting(dslDiagram) is ModelDiagram modelDiagram)
                            {
                                cp.EnqueueCommand(new CreateEntityTypeShapeCommand(modelDiagram, entityType));
                            }
                        }
                    }

                    if (cp.CommandCount > 0)
                    {
                        cp.Invoke();
                        addedMissingShapes = true;
                    }
                }
            }
        }

        internal void AddMissingAssociationConnectors(EntityDesignArtifact efArtifact, out bool addedMissingShapes)
        {
            addedMissingShapes = false;
            Debug.Assert(efArtifact != null, "EFArtifact is null");
            if (efArtifact != null)
            {
                // Now, for all associations, ensure that an association connector is created.
                if (efArtifact.ConceptualModel() != null
                    && Diagram is EntityDesignerSurface dslDiagram)
                {
                    HashSet<ModelAssociation> associationsMaterializedAsConnectors = new HashSet<ModelAssociation>();
                    foreach (
                        var connector in
                            efArtifact.DesignerInfo.Diagrams.Items.Where(d => d.Id.Value == DiagramId)
                                .SelectMany(d => d.AssociationConnectors))
                    {
                        // First check if there's a ModelElement for this.
                        if (dslDiagram.ModelElement.ModelXRef.GetExisting(connector) is AssociationConnector associationConnector
                            && connector.Association.Target != null
                            && !associationsMaterializedAsConnectors.Contains(connector.Association.Target))
                        {
                            associationsMaterializedAsConnectors.Add(connector.Association.Target);
                        }
                    }

                    CommandProcessorContext cpc = new CommandProcessorContext(
                        efArtifact.EditingContext,
                        EfiTransactionOriginator.EntityDesignerOriginatorId,
                        "Restore Excluded Elements");
                    CommandProcessor cp = new CommandProcessor(cpc, shouldNotifyObservers: true);

                    foreach (var association in efArtifact.ConceptualModel().Associations())
                    {
                        if (!associationsMaterializedAsConnectors.Contains(association))
                        {
                            var shouldAddConnector = true;
                            var entityTypesOnEnds = association.AssociationEnds().Select(ae => ae.Type.Target);
                            foreach (var entityType in entityTypesOnEnds)
                            {
                                if (entityType != null
                                    && entityType.GetAntiDependenciesOfType<Edmx.Designer.EntityTypeShape>()
                                           .Any(ets => ets.Diagram.Id == DiagramId) == false)
                                {
                                    shouldAddConnector = false;
                                    break;
                                }
                            }

                            if (shouldAddConnector)
                            {
                                if (dslDiagram.ModelElement.ModelXRef.GetExisting(dslDiagram) is ModelDiagram modelDiagram)
                                {
                                    cp.EnqueueCommand(new CreateAssociationConnectorCommand(modelDiagram, association));
                                }
                            }
                        }
                    }

                    if (cp.CommandCount > 0)
                    {
                        cp.Invoke();
                        addedMissingShapes = true;
                    }
                }
            }
        }

        public new string Title
        {
            get { return base.Title; }
            set
            {
                if (value != base.Title)
                {
                    base.Title = value;
                    if (OnDiagramTitleChanged != null)
                    {
                        OnDiagramTitleChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        ///     Contains the list of related shapes which emphasis shapes will be drawn around them.
        /// </summary>
        public EmphasizedShapes EmphasizedShapes
        {
            get { return _emphasizedShapes; }
        }

        protected override void InitializeResources(StyleSet classStyleSet)
        {
            base.InitializeResources(classStyleSet);

            // Themed colors are pushed in by the host, if it has any. Registering rather than subscribing is
            // deliberate: this used to hook the static VSColorTheme.ThemeChanged and could never unhook, because
            // a shape class has no teardown point. See DiagramTheme.
            DiagramTheme.Register(classStyleSet);
        }

    }
}
