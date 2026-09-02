// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DslAssociation = Microsoft.Data.Entity.Design.Diagrams.ViewModel.Association;
using DslConnector = Microsoft.Data.Entity.Design.Diagrams.View.AssociationConnector;
using DslSurface = Microsoft.Data.Entity.Design.Diagrams.View.EntityDesignerSurface;
using DslViewModel = Microsoft.Data.Entity.Design.Diagrams.ViewModel.EntityDesignerViewModel;
using DslXRef = Microsoft.Data.Entity.Design.Diagrams.CustomSerializer.ModelToDesignerModelXRef;
using ModelConnector = Microsoft.Data.Entity.Design.Edmx.Designer.AssociationConnector;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFAssociationDescriptor :
        EFAnnotatableElementDescriptor<Association>,
        IAnnotatableDocumentableDescriptor
    {
        private AssociationEnd end1;
        private AssociationEnd end2;
        private NavigationProperty navProp1;
        private NavigationProperty navProp2;
        private ReferentialConstraintProperty _ref;

        protected override void OnTypeDescriptorInitialize()
        {
            base.OnTypeDescriptorInitialize();
            if (TypedEFElement != null)
            {
                var ends = TypedEFElement.AssociationEnds();
                if (ends.Count > 0)
                {
                    end1 = ends[0];
                    var et1 = end1.Type.Target;
                    if (et1 is ConceptualEntityType cet1)
                    {
                        navProp1 = cet1.FindNavigationPropertyForEnd(end1);
                    }
                }
                if (ends.Count > 1)
                {
                    end2 = ends[1];
                    var et2 = end2.Type.Target;

                    if (et2 is ConceptualEntityType cet2)
                    {
                        navProp2 = cet2.FindNavigationPropertyForEnd(end2);
                    }
                }
                _ref = new ReferentialConstraintProperty();
            }
        }

        [LocDescription("PropertyWindow_Descritpion_AssociationName")]
        [MergableProperty(false)]
        public override string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_AssociationSetName")]
        [LocDescription("PropertyWindow_Descritpion_AssociationSetName")]
        [MergableProperty(false)]
        public string AssociationSetName
        {
            get
            {
                var assocSet = TypedEFElement.AssociationSet;
                if (assocSet != null)
                {
                    return assocSet.LocalName.Value;
                }
                return null;
            }
            set
            {
                var assocSet = TypedEFElement.AssociationSet;
                if (assocSet != null)
                {
                    var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                    Command c = new EntityDesignRenameCommand(assocSet, value, true);
                    CommandProcessor cp = new CommandProcessor(cpc, c);
                    cp.Invoke();
                }
            }
        }

        /// <summary>
        ///     Whether the connector on the active diagram keeps a route the user drew by hand. Set it back to
        ///     <see langword="false" /> to discard that route and let the layout engine route the connector
        ///     automatically again.
        /// </summary>
        /// <remarks>
        ///     Backed by the <c>ManuallyRouted</c> attribute on the diagram's <c>AssociationConnector</c>, not on
        ///     the association: an association can appear on several diagrams, so the value is read and written on
        ///     the connector belonging to the diagram the user is looking at. Editing it rides the same in-memory
        ///     model as every other property - dirty until an explicit Save - and setting it back off clears the
        ///     saved points so the engine re-routes. See specs/diagram-layout-engines.md.
        /// </remarks>
        [LocCategory("PropertyWindow_Category_Routing")]
        [LocDisplayName("PropertyWindow_DisplayName_ConnectorManuallyRouted")]
        [LocDescription("PropertyWindow_Description_ConnectorManuallyRouted")]
        [MergableProperty(false)]
        public bool ManuallyRouted
        {
            get { return ResolveActiveConnector()?.Model.ManuallyRouted.Value ?? false; }
            set
            {
                if (ResolveActiveConnector() is not { } connector
                    || value == connector.Model.ManuallyRouted.Value)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();

                if (value)
                {
                    // Pin the route the connector is drawn with now, so "manually routed" is a real saved route
                    // rather than an empty one the loader has to fall back from.
                    var points = connector.Dsl.EdgePoints
                        .Cast<Microsoft.VisualStudio.Modeling.Diagrams.EdgePoint>()
                        .Select(point => new KeyValuePair<double, double>(point.Point.X, point.Point.Y))
                        .ToList();

                    new CommandProcessor(
                        cpc,
                        new SetConnectorPointsCommand(connector.Model, points),
                        new UpdateDefaultableValueCommand<bool>(connector.Model.ManuallyRouted, true))
                        .Invoke();
                }
                else
                {
                    // Discard the hand-drawn route so the layout engine routes the connector automatically again.
                    new CommandProcessor(
                        cpc,
                        new SetConnectorPointsCommand(connector.Model, new List<KeyValuePair<double, double>>()),
                        new UpdateDefaultableValueCommand<bool>(connector.Model.ManuallyRouted, false))
                        .Invoke();
                }
            }
        }

        /// <summary>
        ///     Hides <see cref="ManuallyRouted" /> unless the selection maps to a connector on the active diagram.
        /// </summary>
        /// <remarks>
        ///     Found by reflection on the "IsBrowsable" + property name convention. An association selected in the
        ///     Model Browser has no connector to route, so the property has nothing to act on there.
        /// </remarks>
        public bool IsBrowsableManuallyRouted()
        {
            return ResolveActiveConnector() is not null;
        }

        [LocCategory("PropertyWindow_Category_Constraint")]
        [LocDisplayName("PropertyWindow_DisplayName_RefConstraint")]
        [MergableProperty(false)]
        public ReferentialConstraintProperty ReferentialConstraint
        {
            get { return _ref; }
        }

        public bool IsBrowsableReferentialConstraint()
        {
            return TypedEFElement.EntityModel.IsCSDL;
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "Association"; // no need to localize class name
        }

        public bool IsReadOnlyAssociationSetName()
        {
            return IsReadOnly;
        }

        internal override bool IsReadOnlyName()
        {
            return IsReadOnly;
        }

        private bool IsReadOnly
        {
            get { return (TypedEFElement == null || TypedEFElement.EntityModel == null || !TypedEFElement.EntityModel.IsCSDL); }
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End1Multiplicity")]
        [LocDescription("PropertyWindow_Descritpion_Multiplicity")]
        [TypeConverter(typeof(End1MultiplicityConverter))]
        public string End1Multiplicity
        {
            get { return GetEndMultiplicity(end1); }
            set { SetEndMultiplicity(end1, value); }
        }

        public bool IsReadOnlyEnd1Multiplicity()
        {
            return IsReadOnly || end1 == null;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End2Multiplicity")]
        [LocDescription("PropertyWindow_Descritpion_Multiplicity")]
        [TypeConverter(typeof(End2MultiplicityConverter))]
        public string End2Multiplicity
        {
            get { return GetEndMultiplicity(end2); }
            set { SetEndMultiplicity(end2, value); }
        }

        public bool IsReadOnlyEnd2Multiplicity()
        {
            return IsReadOnly || end2 == null;
        }

        private static string GetEndMultiplicity(AssociationEnd end)
        {
            if (end == null)
            {
                return String.Empty;
            }
            return end.Multiplicity.Value;
        }

        private static void SetEndMultiplicity(AssociationEnd end, string value)
        {
            var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
            Command c = new ChangeAssociationEndCommand(end, value, null);
            CommandProcessor cp = new CommandProcessor(cpc, c);
            cp.Invoke();
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End1Role")]
        [LocDescription("PropertyWindow_Descritpion_Role")]
        [MergableProperty(false)]
        public string End1Role
        {
            get { return GetEndRole(end1); }
            set { SetEndRole(end1, value); }
        }

        public bool IsReadOnlyEnd1Role()
        {
            return IsReadOnly || end1 == null;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End2Role")]
        [LocDescription("PropertyWindow_Descritpion_Role")]
        [MergableProperty(false)]
        public string End2Role
        {
            get { return GetEndRole(end2); }
            set { SetEndRole(end2, value); }
        }

        public bool IsReadOnlyEnd2Role()
        {
            return IsReadOnly || end2 == null;
        }

        private static string GetEndRole(AssociationEnd end)
        {
            if (end == null)
            {
                return String.Empty;
            }
            return end.Role.Value;
        }

        private static void SetEndRole(AssociationEnd end, string value)
        {
            var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
            Command c = new ChangeAssociationEndCommand(end, null, value);
            CommandProcessor cp = new CommandProcessor(cpc, c);
            cp.Invoke();
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End1NavigationProperty")]
        [LocDescription("PropertyWindow_Descritpion_EndNavigationProperty")]
        [MergableProperty(false)]
        public string End1NavigationProperty
        {
            get { return GetEndNavigationProperty(navProp1); }
            set { SetEndNavigationProperty(navProp1, value); }
        }

        public bool IsReadOnlyEnd1NavigationProperty()
        {
            return IsReadOnly || navProp1 == null;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End2NavigationProperty")]
        [LocDescription("PropertyWindow_Descritpion_EndNavigationProperty")]
        [MergableProperty(false)]
        public string End2NavigationProperty
        {
            get { return GetEndNavigationProperty(navProp2); }
            set { SetEndNavigationProperty(navProp2, value); }
        }

        public bool IsReadOnlyEnd2NavigationProperty()
        {
            return IsReadOnly || navProp2 == null;
        }

        private static string GetEndNavigationProperty(NavigationProperty navProp)
        {
            if (navProp == null)
            {
                return String.Empty;
            }
            return navProp.LocalName.Value;
        }

        private static void SetEndNavigationProperty(NavigationProperty navProp, string value)
        {
            var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
            Command c = new EntityDesignRenameCommand(navProp, value, true);
            CommandProcessor cp = new CommandProcessor(cpc, c);
            cp.Invoke();
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End1OnDelete")]
        [LocDescription("PropertyWindow_Descritpion_EndOnDelete")]
        [TypeConverter(typeof(OnDeleteActionConverter))]
        [MergableProperty(false)]
        public string End1OnDelete
        {
            get { return GetEndOnDelete(end1); }
            set { SetEndOnDelete(end1, value); }
        }

        public bool IsReadOnlyEnd1OnDelete()
        {
            return IsReadOnly || end1 == null;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_End2OnDelete")]
        [LocDescription("PropertyWindow_Descritpion_EndOnDelete")]
        [TypeConverter(typeof(OnDeleteActionConverter))]
        [MergableProperty(false)]
        public string End2OnDelete
        {
            get { return GetEndOnDelete(end2); }
            set { SetEndOnDelete(end2, value); }
        }

        public bool IsReadOnlyEnd2OnDelete()
        {
            return IsReadOnly || end2 == null;
        }

        private static string GetEndOnDelete(AssociationEnd end)
        {
            if (end != null
                && end.OnDeleteAction != null)
            {
                return end.OnDeleteAction.Action.Value;
            }
            return ModelConstants.OnDeleteAction_None;
        }

        private static void SetEndOnDelete(AssociationEnd end, string value)
        {
            var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
            if (end.OnDeleteAction != null
                && value == ModelConstants.OnDeleteAction_None)
            {
                DeleteEFElementCommand.DeleteInTransaction(cpc, end.OnDeleteAction);
            }
            else if (end.OnDeleteAction == null
                     && value == ModelConstants.OnDeleteAction_Cascade)
            {
                CommandProcessor.InvokeSingleCommand(cpc, new CreateOnDeleteActionCommand(end, value));
            }
        }

        public override object GetDescriptorDefaultValue(string propertyDescriptorMethodName)
        {
            if (propertyDescriptorMethodName.Equals("End1Role"))
            {
                return end1.Role.DefaultValue;
            }
            else if (propertyDescriptorMethodName.Equals("End2Role"))
            {
                return end2.Role.DefaultValue;
            }
            if (propertyDescriptorMethodName.Equals("End1OnDelete")
                || propertyDescriptorMethodName.Equals("End2OnDelete"))
            {
                return ModelConstants.OnDeleteAction_None;
            }
            return null;
        }

        /// <summary>
        ///     Finds the <see cref="ModelConnector" /> for this association on the active diagram, paired with its
        ///     DSL shape. Returns <see langword="null" /> when the association is not shown on an active diagram.
        /// </summary>
        /// <remarks>
        ///     The association maps to one DSL view element per open diagram; the one whose surface is the live
        ///     view is the connector the user selected. Resolving through the shape (rather than searching the
        ///     model's diagrams) is what keeps a multi-diagram model unambiguous.
        /// </remarks>
        private (ModelConnector Model, DslConnector Dsl)? ResolveActiveConnector()
        {
            if (TypedEFElement is not { } association
                || EditingContext is null)
            {
                return null;
            }

            // The association maps to one connector per open diagram. Collect the candidates, then prefer the one
            // on the diagram whose view is live; fall back to the only candidate when a single diagram is open and
            // its view has not reported active yet, so the property never vanishes in the common case.
            var candidates = new List<(ModelConnector Model, DslConnector Dsl, bool IsActive)>();

            foreach (var dslElement in DslXRef.GetExisting(EditingContext, association))
            {
                if (dslElement is not DslAssociation dslAssociation)
                {
                    continue;
                }

                var dslConnector = Microsoft.VisualStudio.Modeling.Diagrams.PresentationViewsSubject
                    .GetPresentation(dslAssociation)
                    .OfType<DslConnector>()
                    .FirstOrDefault();

                if (dslConnector?.Diagram is DslSurface surface
                    && surface.ModelElement is DslViewModel viewModel
                    && viewModel.ModelXRef.GetExisting(dslConnector) is ModelConnector modelConnector)
                {
                    candidates.Add((modelConnector, dslConnector, surface.ActiveDiagramView is not null));
                }
            }

            var chosen = candidates.FirstOrDefault(candidate => candidate.IsActive);
            if (chosen.Model is null && candidates.Count == 1)
            {
                chosen = candidates[0];
            }

            return chosen.Model is null ? null : (chosen.Model, chosen.Dsl);
        }
    }
}
