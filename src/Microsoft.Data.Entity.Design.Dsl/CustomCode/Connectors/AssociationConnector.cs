// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using Microsoft.Data.Entity.Design.EntityDesigner.CustomCode.Utils;
using Microsoft.Data.Entity.Design.EntityDesigner.CustomSerializer;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.UI.Views.Dialogs;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using Microsoft.VisualStudio.Modeling.Immutability;
using Microsoft.VisualStudio.PlatformUI;
using DesignRes = Microsoft.Data.Entity.Design.Resources;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    partial class AssociationConnector
    {
        public new Association ModelElement
        {
            get { return base.ModelElement as Association; }
        }

        public override bool HasToolTip
        {
            get { return true; }
        }

        /// <summary>
        ///     Are themable colors already applied?
        /// </summary>
        internal static bool IsColorThemeSet { get; set; }

        /// <summary>
        ///     Even when dark theme is selected sometimes need to draw against white background (e.g. ExportAsImage)
        /// </summary>
        internal static bool ForceDrawOnWhiteBackground { get; set; }

        protected override void InitializeDecorators(IList<ShapeField> shapeFields, IList<Decorator> decorators)
        {
            base.InitializeDecorators(shapeFields, decorators);
            // Cardinality labels should be made readonly.
            (FindDecorator(decorators, "SourceEndDisplayText").Field as TextField).DefaultFocusable = false;
            (FindDecorator(decorators, "TargetEndDisplayText").Field as TextField).DefaultFocusable = false;
        }

        public override void OnPaintShape(DiagramPaintEventArgs e)
        {
            // Check if we have to override colors.
            if (!IsColorThemeSet)
            {
                SetColorTheme();
            }

            base.OnPaintShape(e);

            // DoPaintEmphasis requires some information from canvas (for example: canvas's zoom factor), 
            // so we need to skip drawing shapes' emphasis if they are drawn outside of diagram canvas (for example: in thumbnail view).
            if (e.View != null)
            {
                // If the shape is in the EmphasizedShapes list, draw the emphasis shape around the shape.
                EntityDesignerDiagram entityDesignerDiagram = Diagram as EntityDesignerDiagram;
                if (entityDesignerDiagram.EmphasizedShapes.Contains(new DiagramItem(this)))
                {
                    ShapeGeometry.DoPaintEmphasis(e, this);
                }
            }
        }

        public override bool CanMove
        {
            get { return (Partition.GetLocks() & Locks.Properties) != Locks.Properties; }
        }

        public override bool CanManuallyRoute
        {
            get { return (Partition.GetLocks() & Locks.Properties) != Locks.Properties; }
        }

        /// <summary>
        ///     Override themable colors.
        /// </summary>
        private void SetColorTheme()
        {
            // Set the emphasis outline color for selected shapes.
            ClassStyleSet.OverridePenColor(DiagramPens.EmphasisOutline, EntityTypeShape.EmphasisShapeOutlineColor);

            // SourceEndDisplayText and TargetEndDisplayText use this brush, and we need them to be distinguisable in the background. 
            // If we are drawing shapes for ExportAsImage we'll need to override the theme to make the text (i.e. cardinalities) visible.
            var shapeTextColor = ForceDrawOnWhiteBackground
                ? Color.Black
                : VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            ClassStyleSet.OverrideBrushColor(DiagramBrushes.ShapeText, shapeTextColor);
            // Shouldn't need to do this unless user changes theme or we had been drawing on white background for ExportAsImage.
            IsColorThemeSet = true;
        }

        public override string GetToolTipText(DiagramItem item)
        {
            var tooltipText = base.GetToolTipText(item);

            var association = ModelElement;
            if (association != null)
            {
                // Source: {0} ({1})\nTarget {2} ({3})
                tooltipText = String.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.Tooltip_AssociationConnector,
                    association.SourceEntityType.Name, association.SourceMultiplicity,
                    association.TargetEntityType.Name, association.TargetMultiplicity).Replace(@"\n", "\n");
            }

            return tooltipText;
        }

        public override string AccessibleName
        {
            get
            {
                var sourceEntityTypeName = EntityDesignerRes.Acc_Unnamed;
                var targetEntityTypeName = EntityDesignerRes.Acc_Unnamed;

                if (ModelElement != null)
                {
                    if (ModelElement.SourceEntityType != null
                        && !string.IsNullOrEmpty(ModelElement.SourceEntityType.Name))
                    {
                        sourceEntityTypeName = ModelElement.SourceEntityType.Name;
                    }

                    if (ModelElement.TargetEntityType != null
                        && !string.IsNullOrEmpty(ModelElement.TargetEntityType.Name))
                    {
                        targetEntityTypeName = ModelElement.TargetEntityType.Name;
                    }
                }

                return string.Format(CultureInfo.CurrentCulture,
                    EntityDesignerRes.IsInAssociationWith,
                    sourceEntityTypeName,
                    targetEntityTypeName);
            }
        }

        public override string AccessibleDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_Association,
                    EntityDesignerRes.CompClassName_Association,
                    ModelElement.SourceEntityType.Name,
                    ModelElement.TargetEntityType.Name);
            }
        }

        protected override VGRoutingStyle DefaultRoutingStyle
        {
            get { return VGRoutingStyle.VGRouteNetwork; }
        }

        public override void OnDoubleClick(DiagramPointEventArgs e)
        {
            var association = ModelElement;
            if (association != null)
            {
                if (Diagram is EntityDesignerDiagram diagram)
                {
                    var ec = diagram.GetModel().EditingContext;
                    ModelToDesignerModelXRef xref = ModelToDesignerModelXRef.GetModelToDesignerModelXRef(ec);

                    Model.Entity.Association modelAssociation = xref.GetExisting(association) as Model.Entity.Association;
                    Debug.Assert(modelAssociation != null, "couldn't find model association for connector");
                    if (modelAssociation != null)
                    {
                        var commands = ReferentialConstraintDialog.LaunchReferentialConstraintDialog(modelAssociation);

                        CommandProcessorContext cpc = new CommandProcessorContext(
                            ec, EfiTransactionOriginator.EntityDesignerOriginatorId, DesignRes.Tx_ReferentialContraint);
                        CommandProcessor cp = new CommandProcessor(cpc);
                        foreach (var c in commands)
                        {
                            cp.EnqueueCommand(c);
                        }
                        cp.Invoke();
                    }
                }
            }
        }
    }
}
