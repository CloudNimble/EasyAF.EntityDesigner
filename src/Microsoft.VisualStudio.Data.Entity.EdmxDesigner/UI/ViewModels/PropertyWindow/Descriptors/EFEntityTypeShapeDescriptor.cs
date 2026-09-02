// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    // We need to differentiate EFEntityTypeShapeDescriptor from EFEntityTypeDescriptor because
    // There is additional property that we want to show for Entity type shape (for example: color).
    internal class EFEntityTypeShapeDescriptor : EFEntityTypeDescriptor
    {
        private EntityTypeShape _entityTypeShape;

        internal override void Initialize(EFObject obj, EditingContext editingContext, bool runningInVS)
        {
            _entityTypeShape = obj as EntityTypeShape;

            Debug.Assert(_entityTypeShape != null, "EFObject is null or is not a type of EntityTypeShape.");

            if (_entityTypeShape != null)
            {
                var entityType = _entityTypeShape.EntityType.Target;
                Debug.Assert(entityType != null, "EntityTypeShape does not contain instance of an entity type.");
                if (entityType != null)
                {
                    base.Initialize(entityType, editingContext, runningInVS);
                }
            }
        }

        [LocCategory("PropertyWindow_Category_Diagram")]
        [LocDisplayName("PropertyWindow_DisplayName_EntityShapeColor")]
        [LocDescription("PropertyWindow_Description_EntityShapeColor")]
        [Editor(typeof(ColorEditor), typeof(UITypeEditor))]
        public Color FillColor
        {
            get { return _entityTypeShape.FillColor.Value; }
            set
            {
                // When the user clear out the property value, set it back to default color.
                if (value.IsEmpty)
                {
                    value = EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor;
                }

                if (value == FillColor)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(cpc, new UpdateDefaultableValueCommand<Color>(_entityTypeShape.FillColor, value));
            }
        }

        /// <summary>
        ///     The group this shape belongs to, used to place related shapes together.
        /// </summary>
        /// <remarks>
        ///     On the shape rather than the entity, so two diagrams over one model can group differently. A modern
        ///     layout writes its guess here for any shape that has none, and never overwrites what it finds - so
        ///     editing this is how a grouping gets corrected, and the correction survives every later layout.
        /// </remarks>
        [LocCategory("PropertyWindow_Category_Layout")]
        [LocDisplayName("PropertyWindow_DisplayName_EntityShapeGroupName")]
        [LocDescription("PropertyWindow_Description_EntityShapeGroupName")]
        public string GroupName
        {
            get { return _entityTypeShape.GroupName.Value; }
            set
            {
                var trimmed = value?.Trim() ?? string.Empty;

                if (trimmed == GroupName)
                {
                    return;
                }

                // Null, not empty: an empty value writes GroupName="", which reads as "deliberately in no group"
                // and would still send the grouping down its Explicit path. Clearing the box means clearing the
                // attribute.
                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(
                    cpc,
                    new UpdateDefaultableValueCommand<string>(
                        _entityTypeShape.GroupName, trimmed.Length == 0 ? null : trimmed));
            }
        }

        /// <summary>
        ///     Hides <see cref="GroupName" /> unless the diagram is in modern layout.
        /// </summary>
        /// <remarks>
        ///     Found by reflection on the "IsBrowsable" + property name convention. The legacy engine has no
        ///     notion of groups, so editing this there would rearrange the diagram without using the value.
        /// </remarks>
        internal bool IsBrowsableGroupName()
        {
            return (_entityTypeShape?.Diagram as Diagram)?.LayoutMode.Value == LayoutMode.Modern;
        }
    }
}
