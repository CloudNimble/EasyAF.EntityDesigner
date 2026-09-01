// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFDiagramDescriptor : EFAnnotatableElementDescriptor<Diagram>
    {
        [LocDescription("PropertyWindow_Description_DiagramName")]
        public override string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }

        /// <summary>
        ///     How this diagram's connectors are drawn.
        /// </summary>
        /// <remarks>
        ///     Reports <see cref="Edmx.Designer.ConnectorMode.Orthogonal" /> rather than the stored
        ///     <see cref="Edmx.Designer.ConnectorMode.Legacy" /> while the diagram is in modern layout, because
        ///     that is what the layout will actually do. Showing the stored value would offer the user a choice
        ///     the converter beside it has already ruled out.
        /// </remarks>
        [LocCategory("PropertyWindow_Category_ModernLayoutOptions")]
        [LocDisplayName("PropertyWindow_DisplayName_ConnectorMode")]
        [LocDescription("PropertyWindow_Description_ConnectorMode")]
        [TypeConverter(typeof(ConnectorModeConverter))]
        public ConnectorMode ConnectorMode
        {
            get
            {
                var stored = TypedEFElement.ConnectorMode.Value;

                return stored == ConnectorMode.Legacy ? ConnectorMode.Orthogonal : stored;
            }
            set
            {
                if (value == ConnectorMode.Legacy || value == TypedEFElement.ConnectorMode.Value)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(
                    cpc, new UpdateDefaultableValueCommand<ConnectorMode>(TypedEFElement.ConnectorMode, value));
            }
        }

        /// <summary>
        ///     Which engine arranges this diagram.
        /// </summary>
        [LocCategory("PropertyWindow_Category_Layout")]
        [LocDisplayName("PropertyWindow_DisplayName_LayoutMode")]
        [LocDescription("PropertyWindow_Description_LayoutMode")]
        public LayoutMode LayoutMode
        {
            get { return TypedEFElement.LayoutMode.Value; }
            set
            {
                if (value == TypedEFElement.LayoutMode.Value)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(
                    cpc, new UpdateDefaultableValueCommand<LayoutMode>(TypedEFElement.LayoutMode, value));
            }
        }

        /// <summary>
        ///     Whether a modern layout clusters this diagram's shapes into groups.
        /// </summary>
        [LocCategory("PropertyWindow_Category_ModernLayoutOptions")]
        [LocDisplayName("PropertyWindow_DisplayName_EnableGrouping")]
        [LocDescription("PropertyWindow_Description_EnableGrouping")]
        public bool EnableGrouping
        {
            get { return TypedEFElement.EnableGrouping.Value; }
            set
            {
                if (value == TypedEFElement.EnableGrouping.Value)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(
                    cpc, new UpdateDefaultableValueCommand<bool>(TypedEFElement.EnableGrouping, value));
            }
        }

        /// <summary>
        ///     Whether a modern layout writes a detected group name onto a shape that has none.
        /// </summary>
        [LocCategory("PropertyWindow_Category_ModernLayoutOptions")]
        [LocDisplayName("PropertyWindow_DisplayName_GenerateGroupNames")]
        [LocDescription("PropertyWindow_Description_GenerateGroupNames")]
        public bool GenerateGroupNames
        {
            get { return TypedEFElement.GenerateGroupNames.Value; }
            set
            {
                if (value == TypedEFElement.GenerateGroupNames.Value)
                {
                    return;
                }

                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                CommandProcessor.InvokeSingleCommand(
                    cpc, new UpdateDefaultableValueCommand<bool>(TypedEFElement.GenerateGroupNames, value));
            }
        }

        /// <summary>
        ///     Hides <see cref="ConnectorMode" /> unless the diagram is in modern layout.
        /// </summary>
        /// <remarks>
        ///     Found by reflection on the "IsBrowsable" + property name convention, the same way
        ///     <c>IsBrowsableDocumentation</c> is. The legacy engine draws the only connectors it knows how to
        ///     draw, so offering a choice there would be offering one that does nothing.
        /// </remarks>
        internal bool IsBrowsableConnectorMode()
        {
            return TypedEFElement.LayoutMode.Value == LayoutMode.Modern;
        }

        /// <summary>
        ///     Hides <see cref="EnableGrouping" /> unless the diagram is in modern layout.
        /// </summary>
        /// <remarks>
        ///     The legacy engine has no notion of groups. Found by the "IsBrowsable" + property name convention.
        /// </remarks>
        internal bool IsBrowsableEnableGrouping()
        {
            return TypedEFElement.LayoutMode.Value == LayoutMode.Modern;
        }

        /// <summary>
        ///     Hides <see cref="GenerateGroupNames" /> unless the diagram is in modern layout.
        /// </summary>
        /// <remarks>
        ///     Shown independently of <see cref="EnableGrouping" />: the two are separate switches, and with
        ///     grouping off nothing is generated regardless of this value.
        /// </remarks>
        internal bool IsBrowsableGenerateGroupNames()
        {
            return TypedEFElement.LayoutMode.Value == LayoutMode.Modern;
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "Diagram";
        }
    }
}
