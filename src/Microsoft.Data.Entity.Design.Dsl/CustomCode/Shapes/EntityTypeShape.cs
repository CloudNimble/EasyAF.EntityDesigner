// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.Entity.Design.EntityDesigner.CustomCode.Utils;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Immutability;
using Microsoft.VisualStudio.PlatformUI;
using Diagram = Microsoft.Data.Entity.Design.Model.Designer.Diagram;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal partial class EntityTypeShape
    {
        internal static readonly string PropertiesCompartmentName = "Properties";
        internal static readonly string NavigationCompartmentName = "Navigation";
        internal static readonly Color TransparentColor = Color.Magenta;

        /// <summary>
        /// Corner radius in pixels for entity shapes.
        /// Used by both the designer and SVG export.
        /// </summary>
        internal const double CornerRadiusPixels = 3.0;

        /// <summary>
        /// Corner radius in world units (inches).
        /// Matches the SVG export CornerRadius of 3 pixels at 96 DPI.
        /// 3 pixels / 96 DPI = 0.03125 inches
        /// </summary>
        private const double CornerRadiusInches = CornerRadiusPixels / 96.0;

        /// <summary>
        /// Singleton instance of the rounded rectangle geometry.
        /// </summary>
        private static readonly ShapeGeometry RoundedGeometry =
            new RoundedRectangleShapeGeometry(CornerRadiusInches);

        /// <summary>
        /// Gets the geometry for this shape with rounded corners.
        /// </summary>
        public override ShapeGeometry ShapeGeometry
        {
            get { return RoundedGeometry; }
        }

        private bool _isAdjustedForFillColor;

        internal class EntityChevronButtonField : ChevronButtonField
        {
            internal EntityChevronButtonField(string fieldName)
                : base(fieldName)
            {
            }

            protected override Image GetButtonImage(ShapeElement parentShape)
            {
                var state = IsExpanded(parentShape);
                if (state.HasValue
                    && parentShape is EntityTypeShape entityShape)
                {
                    return state.Value
                               ? CachedFillColorAppearance(entityShape.FillColor).HeaderIcons.ChevronExpanded
                               : CachedFillColorAppearance(entityShape.FillColor).HeaderIcons.ChevronCollapsed;
                }
                return null;
            }

            internal bool? IsExpanded(ShapeElement parentShape)
            {
                return GetValue(parentShape) as bool?;
            }
        }

        internal class EntityImageField : ImageField
        {
            private readonly Func<FillColorAppearance, Bitmap> _bitmapAccessor;

            internal EntityImageField(string fieldName, Func<FillColorAppearance, Bitmap> bitmapAccessor)
                : base(fieldName)
            {
                _bitmapAccessor = bitmapAccessor;
            }

            public override Image GetDisplayImage(ShapeElement parentShape)
            {
                return parentShape is EntityTypeShape entityShape
                           ? _bitmapAccessor(CachedFillColorAppearance(entityShape.FillColor))
                           : DefaultImage;
            }
        }

        internal static Color EmphasisShapeOutlineColor
        {
            get { return VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerEmphasisBorderColorKey); }
        }

        /// <summary>
        ///     Setup initial shape size and position
        /// </summary>
        public override void OnInitialize()
        {
            base.OnInitialize();
            //
            // The following code was taken from the Dsl's DslDesigner code to fix up a pathological perf problem
            // when all shapes are in the same region.  
            //
            // Seed shapes along a gradient so that there is no overlap, favor vertical over horizontal scrolling
            // Done in the constructor so it happens before GraphObject initialization
            PointD randomized = new PointD(
                (EntityDesignerViewModel.EntityShapeLocationSeed % 7) * 3.0, EntityDesignerViewModel.EntityShapeLocationSeed * 1.5);
            var bounds = AbsoluteBounds;
            bounds.Location = randomized;
            AbsoluteBounds = bounds;
            EntityDesignerViewModel.EntityShapeLocationSeed++;

            // Provide keyboard shortcuts for collapse and expand of this EntityType shape
            this.KeyUp += EntityTypeShape_KeyUp;
        }

        /// <summary>
        ///     Were theme colors already applied?
        /// </summary>
        internal static bool IsColorThemeSet { get; set; }

        /// <summary>
        ///     Paint emphasis for selected shape, but first check if we need to update colors.
        /// </summary>
        public override void OnPaintEmphasis(DiagramPaintEventArgs e)
        {
            if (!IsColorThemeSet)
            {
                SetColorTheme();
            }
            base.OnPaintEmphasis(e);
        }

        private void SetColorTheme()
        {
            ClassStyleSet.OverridePenColor(DiagramPens.EmphasisOutline, EmphasisShapeOutlineColor);
            // We shouldn't need to do this again uless the user changes the theme.
            IsColorThemeSet = true;
        }

        public EntityType TypedModelElement
        {
            get { return ModelElement as EntityType; }
        }

        public new EntityDesignerDiagram Diagram
        {
            get { return base.Diagram as EntityDesignerDiagram; }
        }

        public override bool HasToolTip
        {
            get { return true; }
        }

        public string Name
        {
            get
            {
                TextField headerTitleTextField = FindShapeField(ShapeFields, "Name") as TextField;
                return headerTitleTextField?.GetDisplayText(this);
            }
        }

        public override void OnPaintShape(DiagramPaintEventArgs e)
        {
            // Have we adjusted the other colors in the shape for this fill color?
            if (!_isAdjustedForFillColor)
            {
                AdjustForFillColor();
            }

            base.OnPaintShape(e);
            // DoPaintEmphasis requires some information from canvas (for example: zoom factor), 
            // so we need to skip drawing shapes' emphasis if they are drawn outside of diagram canvas (for example: in thumbnail view).
            if (e.View != null)
            {
                // If the shape is in the EmphasizedShapes list, draw the emphasis shape around the shape.
                if (Diagram.EmphasizedShapes.Contains(new DiagramItem(this)))
                {
                    ShapeGeometry.DoPaintEmphasis(e, this);
                }
            }
        }

        /// <summary>
        ///     If the fill color changes we need to adjust other colors again.
        /// </summary>
        protected override void OnFillColorChanged(Color newValue)
        {
            // Users should never be able to set transparent color.
            if (newValue == Color.Transparent)
            {
                FillColor = EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor;
                return;
            }
            // We adjust colors in the shape on the next painting.
            _isAdjustedForFillColor = false;
        }

        private void AdjustForFillColor()
        {
            // If the shape is very dark, we make the title text white, and viceversa.
            StyleSet.OverrideBrushColor(DiagramBrushes.ShapeText, CachedFillColorAppearance(FillColor).TextColor);
            // We draw a thin outline of a sligtly different color to improve distinguishability when shape color looks like background (and because it looks good).
            StyleSet.OverridePenColor(OutlinePenId, CachedFillColorAppearance(FillColor).OutlineColor);
            // We shouldn't need to do this again unless the user changes the color for this shape.
            _isAdjustedForFillColor = true;
        }

        /// <summary>
        ///     We override default shape highlighting to make it subtler.
        /// </summary>
        protected override int ModifyLuminosity(int currentLuminosity, DiagramClientView view)
        {
            if (view.HighlightedShapes.Contains(new DiagramItem(this)))
            {
                return GetHighlightLuminosity(currentLuminosity);
            }
            return currentLuminosity;
        }

        /// <summary>
        ///     Common part of formula to calculate outline and highlight colors.
        /// </summary>
        private static int GetHighlightLuminosity(int currentLuminosity)
        {
            return currentLuminosity < 120 ? currentLuminosity + 40 : currentLuminosity - 40;
        }

        internal struct FillColorAppearance
        {
            public Color TextColor;
            public Color OutlineColor;

            /// <summary>
            /// Gets the header icons for this appearance. Accessed dynamically from DiagramImageHelper
            /// to avoid holding references to icons that may be disposed on theme change.
            /// </summary>
            public HeaderIconSet HeaderIcons => DiagramImageHelper.Instance.GetHeaderIcons(TextColor);
        }

        /// <summary>
        ///     Calculates appropriate colors and icons for a shape's fill color.
        /// </summary>
        private static FillColorAppearance CalculateFillColorAppearance(Color fillColor)
        {
            HslColor hslColor = HslColor.FromRgbColor(fillColor);
            var textColor = GetTextColor(fillColor);
            return new FillColorAppearance
                {
                    TextColor = textColor,
                    OutlineColor
                        = new HslColor
                            {
                                Hue = hslColor.Hue,
                                Saturation = hslColor.Saturation * 3 / 5,
                                Luminosity = GetHighlightLuminosity(hslColor.Luminosity)
                            }.ToRgbColor()
                    // HeaderIcons is a computed property that dynamically fetches from DiagramImageHelper
                };
        }

        private static readonly double _blackBrightness = GetRelativeBrightness(Color.Black);
        private static readonly double _whiteBrightness = GetRelativeBrightness(Color.White);

        private static Color GetTextColor(Color fillColor)
        {
            var fillBrightness = GetRelativeBrightness(fillColor);
            return GetContrast(_blackBrightness, fillBrightness)
                   > GetContrast(_whiteBrightness, fillBrightness)
                ? Color.Black
                : Color.White;
        }

        private static double GetContrast(double b1, double b2)
        {
            return b1 > b2
                ? (b1 + 0.05) / (b2 + 0.05)
                : (b2 + 0.05) / (b1 + 0.05);
        }

        private static double GetRelativeBrightness(Color color)
        {
            return 0.2126 * GetRelativeColorPart(color.R)
                   + 0.7152 * GetRelativeColorPart(color.G)
                   + 0.0722 * GetRelativeColorPart(color.B);
        }

        private static double GetRelativeColorPart(byte colorPart)
        {
            var part = (colorPart / 255.0);
            return part <= 0.03928 ? part / 12.92 : Math.Pow((part + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        ///     This method is used to cache the results of a function so the funcion is ever at most invoked
        ///     once for each possible value of the argument and only one copy of the result for every value
        ///     of the argument is ever allocated in memory
        /// </summary>
        /// <typeparam name="TArg">the type of the argument of the function</typeparam>
        /// <typeparam name="TResult">the type of the result of the function</typeparam>
        /// <param name="func">original function</param>
        /// <returns>"memoized" version of the original function</returns>
        /// <remarks>
        ///     The correct usage of the Memoize method is to only call it once passing as an argument the
        ///     function to memoize. The returned "memoized" function can be invoked many times to take
        ///     advantage of the cached results. This is a good general reference on memoization:
        ///     http://en.wikipedia.org/wiki/Memoization
        /// </remarks>
        private static Func<TArg, TResult> Memoize<TArg, TResult>(Func<TArg, TResult> func)
        {
            Dictionary<TArg, TResult> map = new Dictionary<TArg, TResult>();
            return arg =>
                {
                    if (!map.TryGetValue(arg, out TResult result))
                    {
                        result = func(arg);
                        map.Add(arg, result);
                    }
                    return result;
                };
        }

        private static readonly Func<Color, FillColorAppearance> CachedFillColorAppearance
            = Memoize<Color, FillColorAppearance>(CalculateFillColorAppearance);

        public override bool CanMove
        {
            get { return (Partition.GetLocks() & Locks.Properties) != Locks.Properties; }
        }

        public override NodeSides ResizableSides
        {
            get
            {
                if ((Partition.GetLocks() & Locks.Properties) != Locks.Properties)
                {
                    return base.ResizableSides;
                }
                return NodeSides.None;
            }
        }

        public EntityTypeElementListCompartment PropertiesCompartment
        {
            get { return FindCompartment(PropertiesCompartmentName) as EntityTypeElementListCompartment; }
        }

        public EntityTypeElementListCompartment NavigationCompartment
        {
            get { return FindCompartment(NavigationCompartmentName) as EntityTypeElementListCompartment; }
        }

        /// <summary>
        ///     Provides an image to display to the left Property elements in the EntityShape.
        ///     Uses pre-cached icons from DiagramImageHelper for performance.
        /// </summary>
        private Image PropertyDisplayImageGetter(ModelElement element)
        {
            if (element is ScalarProperty scalarProperty)
            {
                bool isAt100Zoom = IsAt100PercentZoom();
                return scalarProperty.EntityKey
                    ? (isAt100Zoom ? DiagramImageHelper.Instance.PropertyPKIcon100 : DiagramImageHelper.Instance.PropertyPKIconNormal)
                    : (isAt100Zoom ? DiagramImageHelper.Instance.PropertyIcon100 : DiagramImageHelper.Instance.PropertyIconNormal);
            }

            if (element is ComplexProperty)
            {
                bool isAt100Zoom = IsAt100PercentZoom();
                return isAt100Zoom ? DiagramImageHelper.Instance.ComplexPropertyIcon100 : DiagramImageHelper.Instance.ComplexPropertyIconNormal;
            }

            return null;
        }

        /// <summary>
        ///     Checks if the diagram is currently at 100% zoom.
        /// </summary>
        private bool IsAt100PercentZoom()
        {
            const float zoomTolerance = 0.001f;
            var zoomFactor = Diagram?.ActiveDiagramView?.DiagramClientView?.ZoomFactor ?? 1.0f;
            return Math.Abs(zoomFactor - 1.0f) < zoomTolerance;
        }

        /// <summary>
        ///     Provides an image to display to the left NavigationProperty elements in the EntityShape.
        ///     Uses pre-cached icons from DiagramImageHelper for performance.
        /// </summary>
        private Image NavigationDisplayImageGetter(ModelElement element)
        {
            if (element is NavigationProperty)
            {
                bool isAt100Zoom = IsAt100PercentZoom();
                return isAt100Zoom ? DiagramImageHelper.Instance.NavigationPropertyIcon100 : DiagramImageHelper.Instance.NavigationPropertyIconNormal;
            }
            return null;
        }

        // DDBugs 56442: Keep long class names from overlapping the expand/collapse button
        protected override void InitializeDecorators(IList<ShapeField> shapeFields, IList<Decorator> decorators)
        {
            // Call the base class FIRST because it wipes all the anchoring information
            base.InitializeDecorators(shapeFields, decorators);

            TextField headerTitleTextField = FindShapeField(shapeFields, "Name") as TextField;
            var headerExpandCollapseButtonField = FindShapeField(shapeFields, "ExpandCollapse");

            Debug.Assert(headerTitleTextField != null, "headerTitleTextField != null");
            // DDBugs 56442: Keep long class names from overlapping the expand/collapse button
            headerTitleTextField.AnchoringBehavior.SetRightAnchor(headerExpandCollapseButtonField, AnchoringBehavior.Edge.Left, 0.05);
        }

        protected override void InitializeShapeFields(IList<ShapeField> shapeFields)
        {
            base.InitializeShapeFields(shapeFields);
            TextField headerTitleTextField = FindShapeField(shapeFields, "Name") as TextField;

            Debug.Assert(headerTitleTextField != null, "headerTitleTextField != null");

            // DD 40501: Specify the MSAA name/description for the class title
            headerTitleTextField.DefaultAccessibleName = EntityDesignerRes.AccName_EntityTypeHeader;
            headerTitleTextField.DefaultAccessibleDescription = EntityDesignerRes.AccDesc_EntityTypeHeader;

            // replace default glyphs in header with ones that respond to filler color and scale with the UX
            ReplaceField(
                shapeFields, "ExpandCollapse",
                fieldName => new EntityChevronButtonField(fieldName)
                                {
                                    DefaultSelectable = false,
                                    DefaultFocusable = false
                                });
            ReplaceField(
                shapeFields, "IconDecorator",
                fieldName => new EntityImageField(fieldName, appearance => appearance.HeaderIcons.EntityGlyph));
            ReplaceField(
                shapeFields, "BaseTypeIconDecorator",
                fieldName => new EntityImageField(fieldName, appearance => appearance.HeaderIcons.BaseTypeIcon));
        }

        private static TField ReplaceField<TField>(IList<ShapeField> shapeFields, string fieldName, Func<string, TField> constructor)
            where TField : ShapeField
        {
            Debug.Assert(shapeFields != null, "shapeFields != null");
            Debug.Assert(fieldName != null, "fieldName != null");
            Debug.Assert(constructor != null, "constructor != null");
            var originalField = FindShapeField(shapeFields, fieldName);
            Debug.Assert(originalField != null, "originalField != null");
            shapeFields.Remove(originalField);
            var newField = constructor(fieldName);
            Debug.Assert(newField != null, "newField != null");
            shapeFields.Add(newField);
            return newField;
        }

        public override void EnsureCompartments()
        {
            base.EnsureCompartments();

            SetAccessibilityOnCompartment(PropertiesCompartment, true);
            SetAccessibilityOnCompartment(NavigationCompartment, false);

            // need to add our own handler for KeyUp event in order to disable Insert key for Navigation Compartment
            // and provide keyboard shortcuts for expand and collapse.
            NavigationCompartment.KeyUp += NavigationCompartmentKeyUpHandler;

            // since Properties compartment contains now two DomainClasses (Scalar and Complex) we need to handle Insert key on our own
            // and provide keyboard shortcuts for expand and collapse.
            PropertiesCompartment.KeyUp += PropertiesCompartmentKeyUpHandler;
        }

        private void PropertiesCompartmentKeyUpHandler(object sender, DiagramKeyEventArgs e)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            // on Insert add new ScalarProperty to the compartment
            if (e != null
                && e.KeyCode == Keys.Insert)
            {
                if (sender is ElementListCompartment compartment)
                {
                    var domainClassInfo = compartment.Partition.DomainDataDirectory.GetDomainClass(ScalarProperty.DomainClassId);
                    compartment.HandleNewListItemInsertion(e.DiagramClientView, domainClassInfo);
                    e.Handled = true;
                }
            }

            // Provide keyboard shortcuts for expand and collapse.
            if (e.Control && e.KeyCode == Keys.Up)
            {
                PropertiesCompartment.EnsureExpandedState(false);
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.Down)
            {
                PropertiesCompartment.EnsureExpandedState(true);
                e.Handled = true;
            }
        }

        // disable default ElementListCompartment action for KeyUp event if it's Insert key
        // and provide keyboard shortcuts for expand and collapse.
        private void NavigationCompartmentKeyUpHandler(object sender, DiagramKeyEventArgs e)
        {
            if ((Partition.GetLocks() & Locks.Add) == Locks.Add)
            {
                return;
            }

            if (e.KeyCode == Keys.Insert)
            {
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.Up)
            {
                NavigationCompartment.EnsureExpandedState(false);
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.Down)
            {
                NavigationCompartment.EnsureExpandedState(true);
                e.Handled = true;
            }
        }

        private static void SetAccessibilityOnCompartment(ElementListCompartment compartment, bool scalarProperty)
        {
            if (compartment != null)
            {
                compartment.ItemAccessibleNameGetter = AccessibilityNameGetterMethod;
                compartment.ItemAccessibleDescriptionGetter = AccessibilityDescriptionGetterMethod;
                //ListCompartment.MainListFieldName

                if (compartment.FindShapeField("HdrText") is TextField hdrField
                    && compartment.FindShapeField("MainListField") is ListField listField)
                {
                    string aaName;
                    string aaDescription;

                    if (scalarProperty)
                    {
                        aaName = EntityDesignerRes.AccName_EntityTypeScalarPropertyCompartment;
                        aaDescription = EntityDesignerRes.AccDesc_EntityTypeScalarPropertyCompartment;
                    }
                    else
                    {
                        aaName = EntityDesignerRes.AccName_EntityTypeNavigationPropertyCompartment;
                        aaDescription = EntityDesignerRes.AccDesc_EntityTypeNavigationPropertyCompartment;
                    }

                    hdrField.DefaultAccessibleName = aaName;
                    hdrField.DefaultAccessibleDescription = aaDescription;
                    listField.DefaultAccessibleName = aaName;
                    listField.DefaultAccessibleDescription = aaDescription;
                }
            }
        }

        internal static string AccessibilityNameGetterMethod(ModelElement element)
        {
            if (element is NameableItem named)
            {
                return named.Name;
            }
            return "??";
        }

        internal static string AccessibilityDescriptionGetterMethod(ModelElement element)
        {
            if (element is Property)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_ScalarProperty,
                    EntityDesignerRes.CompClassName_ScalarProperty);
            }
            if (element is NavigationProperty)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_NavigationProperty,
                    EntityDesignerRes.CompClassName_NavigationProperty);
            }
            return "??";
        }

        protected override CompartmentMapping[] GetCompartmentMappings(Type melType)
        {
            var baseMappings = base.GetCompartmentMappings(melType);

            foreach (var mapping in baseMappings)
            {
                if (mapping is ElementListCompartmentMapping elementMap)
                {
                    if (elementMap.CompartmentId.Equals(PropertiesCompartmentName, StringComparison.OrdinalIgnoreCase))
                    {
                        elementMap.ImageGetter = PropertyDisplayImageGetter;
                    }
                    else if (elementMap.CompartmentId.Equals(NavigationCompartmentName, StringComparison.OrdinalIgnoreCase))
                    {
                        elementMap.ImageGetter = NavigationDisplayImageGetter;
                    }
                }
            }

            return baseMappings;
        }

        public override SizeD MinimumResizableSize
        {
            get
            {
                var size = base.MinimumResizableSize;
                size.Width = 0.6;

                return size;
            }
        }

        public override string AccessibleName
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_EntityType,
                    TypedModelElement != null
                    && !string.IsNullOrWhiteSpace(TypedModelElement.Name)
                        ? TypedModelElement.Name
                        : EntityDesignerRes.Acc_Unnamed);
            }
        }

        public override string AccessibleDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_EntityType,
                    EntityDesignerRes.CompClassName_EntityType);
            }
        }

        public override string AccessibleHelp
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccHelp_EntityType,
                    IsExpanded ? EntityDesignerRes.ExpandedStateExpanded : EntityDesignerRes.ExpandedStateCollapsed);
            }
        }

        private void EntityTypeShape_KeyUp(object sender, DiagramKeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Up)
            {
                this.EnsureExpandedState(false);
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.Down)
            {
                this.EnsureExpandedState(true);
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Remove shadow from the entity type shape.
        /// </summary>
        public override bool HasShadow
        {
            get { return false; }
        }

        /// <summary>
        ///     Gets an array of CompartmentDescription for all compartments shown on this shape
        ///     The method will replace ElementListCompartmentDescription instances with EntityTypeElementListCompartmentDescription instances.
        /// </summary>
        public override CompartmentDescription[] GetCompartmentDescriptions()
        {
            var compartmentDescription = base.GetCompartmentDescriptions();

            Debug.Assert(compartmentDescription.Length == 2, "There should be 2 compartment descriptions.");
            if (compartmentDescription.Length == 2)
            {
                ListCompartmentDescription originalElementListCompartmentDescription = compartmentDescription[0] as ListCompartmentDescription;
                Debug.Assert(
                    originalElementListCompartmentDescription != null,
                    "The compartment description is not type of ListCompartmentDescription. Actual type: "
                    + compartmentDescription[0].GetType().Name);
                if (originalElementListCompartmentDescription != null)
                {
                    compartmentDescription[0] = new EntityTypeElementListCompartmentDescription(
                        originalElementListCompartmentDescription,
                        !EntityDesignerDiagramConstant.IsDefaultShapeExpanded,
                        true);
                }

                originalElementListCompartmentDescription = compartmentDescription[1] as ListCompartmentDescription;
                Debug.Assert(
                    originalElementListCompartmentDescription != null,
                    "The compartment description is not type of ListCompartmentDescription. Actual type: "
                    + compartmentDescription[1].GetType().Name);
                if (originalElementListCompartmentDescription != null)
                {
                    compartmentDescription[1] = new EntityTypeElementListCompartmentDescription(
                        originalElementListCompartmentDescription,
                        !EntityDesignerDiagramConstant.IsDefaultShapeExpanded,
                        false);
                }
            }
            return compartmentDescription;
        }

        /// <summary>
        ///     Return DiagramItem object for a given EntityType's Property.
        /// </summary>
        public DiagramItem GetDiagramItemForProperty(PropertyBase propertyBase)
        {
            Debug.Assert(propertyBase != null, "propertyBase is null.");
            if (propertyBase != null)
            {
                Property property = propertyBase as Property;
                NavigationProperty navigationProperty = propertyBase as NavigationProperty;
                Debug.Assert(
                    property != null || navigationProperty != null,
                    "Unexpected property type. Property Type: " + propertyBase.GetType().Name);
                if (property != null
                    || navigationProperty != null)
                {
                    var compartment = navigationProperty != null ? NavigationCompartment : PropertiesCompartment;
                    var propertyIndex = compartment.Items.IndexOf(propertyBase);
                    Debug.Assert(
                        propertyIndex != -1, "Could not find property: " + propertyBase.Name + " in entity-type: " + AccessibleName);

                    if (propertyIndex != -1)
                    {
                        return new DiagramItem(compartment, compartment.ListField, new ListItemSubField(propertyIndex));
                    }
                }
            }
            return null;
        }
    }
}
