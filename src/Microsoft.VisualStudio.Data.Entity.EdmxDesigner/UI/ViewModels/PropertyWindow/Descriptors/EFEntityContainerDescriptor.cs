// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFEntityContainerDescriptor : EFAnnotatableElementDescriptor<ConceptualEntityContainer>
    {
        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_GenerateUpdateViews")]
        // Localized description is returned dynamically based on the target Fx. See DescriptionGenerateUpdateViews()
        [TypeConverter(typeof(BoolConverter))]
        public bool GenerateUpdateViews
        {
            get
            {
                var ecm = TypedEFElement.GetAntiDependenciesOfType<EntityContainerMapping>().FirstOrDefault();
                if (ecm != null)
                {
                    return ecm.GenerateUpdateViews.Value;
                }

                return true;
            }
            set
            {
                var ecm = TypedEFElement.GetAntiDependenciesOfType<EntityContainerMapping>().FirstOrDefault();
                if (ecm != null)
                {
                    var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                    Command cmd = new ChangeEntityContainerMappingCommand(ecm, value);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        internal virtual string DescriptionGenerateUpdateViews()
        {
            if (!EdmFeatureManager.GetGenerateUpdateViewsFeatureState(TypedEFElement.Artifact.SchemaVersion).IsEnabled())
            {
                return String.Format(
                    CultureInfo.CurrentCulture, "({0}) {1}", EdmxDesignerResources.DisabledFeatureTooltip,
                    EdmxDesignerResources.PropertyWindow_Description_GenerateUpdateViews);
            }
            return EdmxDesignerResources.PropertyWindow_Description_GenerateUpdateViews;
        }

        internal virtual bool IsReadOnlyGenerateUpdateViews()
        {
            return (!EdmFeatureManager.GetGenerateUpdateViewsFeatureState(TypedEFElement.Artifact.SchemaVersion)
                           .IsEnabled());
        }

        [LocCategory("PropertyWindow_Category_CodeGeneration")]
        [LocDisplayName("PropertyWindow_DisplayName_Access")]
        // Localized description is returned dynamically based on the Target Fx (see DescriptionTypeAccess())
        [TypeConverter(typeof(AccessConverter))]
        public string TypeAccess
        {
            get
            {
                var concEc = TypedEFElement;
                if (concEc != null)
                {
                    return concEc.TypeAccess.Value;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                var concEc = TypedEFElement;
                if (concEc != null)
                {
                    var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                    UpdateDefaultableValueCommand<string> cmd = new UpdateDefaultableValueCommand<string>(concEc.TypeAccess, value);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        internal virtual string DescriptionTypeAccess()
        {
            if (!EdmFeatureManager.GetEntityContainerTypeAccessFeatureState(TypedEFElement.Artifact.SchemaVersion)
                       .IsEnabled())
            {
                return String.Format(
                    CultureInfo.CurrentCulture, "({0}) {1}", EdmxDesignerResources.DisabledFeatureTooltip,
                    EdmxDesignerResources.PropertyWindow_Description_EntityContainerAccess);
            }
            return EdmxDesignerResources.PropertyWindow_Description_EntityContainerAccess;
        }

        internal virtual bool IsReadOnlyTypeAccess()
        {
            return (!EdmFeatureManager.GetEntityContainerTypeAccessFeatureState(TypedEFElement.Artifact.SchemaVersion)
                           .IsEnabled());
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override object GetDescriptorDefaultValue(string propertyDescriptorMethodName)
        {
            if (propertyDescriptorMethodName.Equals("GenerateUpdateViews"))
            {
                var ecm = TypedEFElement.GetAntiDependenciesOfType<EntityContainerMapping>().FirstOrDefault();
                if (ecm != null
                    && ecm.GenerateUpdateViews != null)
                {
                    return ecm.GenerateUpdateViews.DefaultValue;
                }
            }
            if (propertyDescriptorMethodName.Equals("TypeAccess"))
            {
                return TypedEFElement.TypeAccess.DefaultValue;
            }

            return base.GetDescriptorDefaultValue(propertyDescriptorMethodName);
        }

        public override string GetClassName()
        {
            return "ConceptualEntityContainer";
        }
    }
}