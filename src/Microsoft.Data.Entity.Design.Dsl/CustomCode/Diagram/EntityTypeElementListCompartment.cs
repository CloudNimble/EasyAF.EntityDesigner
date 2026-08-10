// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    [DomainObjectId("53d36908-9892-4495-8754-da5f4d7969d4")]
    internal class EntityTypeElementListCompartment : ElementListCompartment
    {
        private bool _isScalarPropertiesCompartment;

        /// <summary>
        ///     EntityTypeElementListCompartment Constructor
        /// </summary>
        /// <param name="store">Store where new element is to be created.</param>
        /// <param name="propertyAssignments">List of domain property id/value pairs to set once the element is created.</param>
        public EntityTypeElementListCompartment(Store store,
             bool isScalarPropertiesCompartment,
             params PropertyAssignment[] propertyAssignments)
            : this(store?.DefaultPartitionForClass(DomainClassId),
                  isScalarPropertiesCompartment,
                  propertyAssignments)
        {
            Initialize();
        }

        /// <summary>
        ///     EntityTypeElementListCompartment Constructor
        /// </summary>
        /// <param name="partition">Partition where new element is to be created.</param>
        /// <param name="propertyAssignments">List of domain property id/value pairs to set once the element is created.</param>
        public EntityTypeElementListCompartment(Partition partition,
             bool isScalarPropertiesCompartment,
             params PropertyAssignment[] propertyAssignments)
            : base(partition, propertyAssignments)
        {
            _isScalarPropertiesCompartment = isScalarPropertiesCompartment;

            Initialize();
        }

        private void Initialize()
        {
            ListField.AlternateFontId = new StyleSetResourceId(string.Empty, "ShapeTextBoldUnderline");
            //Debug.WriteLine($"[EasyAF.EntityDesigner] ListField.DefaultItemIndent: {ListField.DefaultItemIndent}");
        }

        protected override void InitializeResources(StyleSet classStyleSet)
        {
            base.InitializeResources(classStyleSet);

            // Custom Font Settings.
            // Create a diagram shape text that has been bolded and underlined.
            var diagram = Diagram;
            Debug.Assert(diagram is not null, "Unable to find instance of Diagram from CompartmentList instance.");
            if (diagram is not null)
            {
                var fontSettings = diagram.StyleSet.GetOverriddenFontSettings(DiagramFonts.ShapeText);
                Debug.Assert(fontSettings is not null, "Why Diagram doesn't contains FontSettings for ShapeText?");
                if (fontSettings is not null)
                {
                    fontSettings.Bold = true;
                    fontSettings.Underline = true;
                    classStyleSet.AddFont(
                        new StyleSetResourceId(string.Empty, "ShapeTextBoldUnderline"), DiagramFonts.ShapeText, fontSettings);
                }
            }
        }

        /// <summary>
        ///     Gets drawing information for a single list item in the list field.
        /// </summary>
        /// <param name="listField">The child list field requesting the drawing information.</param>
        /// <param name="row">The zero-based row number of the list item to draw.</param>
        /// <param name="itemDrawInfo">An ItemDrawInfo that receives the drawing information.</param>
        public override void GetItemDrawInfo(ListField listField, int row, ItemDrawInfo itemDrawInfo)
        {
            base.GetItemDrawInfo(listField, row, itemDrawInfo);
            //Debug.WriteLine($"[EasyAF.EntityDesigner] ItemDrawInfo.Indent: {itemDrawInfo.Indent}");
            itemDrawInfo.Indent = 0.02f;

            Debug.Assert(ParentShape is not null, "ElementListCompartment should be contained in another shape.");
            if (ParentShape is not null)
            {
                EntityTypeShape ets = ParentShape as EntityTypeShape;
                Debug.Assert(
                    ets is not null, "Expected ElementListCompartment's parent type:EntityTypeShape , Actual:" + ParentShape.GetType().Name);

                if (ets?.Diagram is not null)
                {
                    //  if the compartment list item is in the EmphasizedShapes list, then set the flag so that the item will be drawn in alternate font.
                    // (The list item's font will be bolded and underlined).
                    if (ets.Diagram.EmphasizedShapes.Contains(new DiagramItem(this, ListField, new ListItemSubField(row))))
                    {
                        itemDrawInfo.AlternateFont = true;
                    }
                }
            }
        }

        public override string AccessibleHelp
        {
            get
            {
                if (_isScalarPropertiesCompartment)
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        EntityDesignerRes.AccHelp_EntityTypeScalarPropertyCompartment,
                        IsExpanded ? EntityDesignerRes.ExpandedStateExpanded : EntityDesignerRes.ExpandedStateCollapsed);
                }

                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccHelp_EntityTypeNavigationPropertyCompartment,
                    IsExpanded ? EntityDesignerRes.ExpandedStateExpanded : EntityDesignerRes.ExpandedStateCollapsed);
            }
        }
    }
}
