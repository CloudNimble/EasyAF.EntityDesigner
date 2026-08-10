// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using Microsoft.VisualStudio.Modeling.Immutability;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    partial class InheritanceConnector
    {
        public new Inheritance ModelElement
        {
            get { return base.ModelElement as Inheritance; }
        }

        public override string AccessibleName
        {
            get
            {
                var baseName = EntityDesignerRes.Acc_Unnamed;
                var derivedName = EntityDesignerRes.Acc_Unnamed;

                if (ModelElement != null)
                {
                    if (ModelElement.TargetEntityType != null
                        && !string.IsNullOrEmpty(ModelElement.TargetEntityType.Name))
                    {
                        baseName = ModelElement.TargetEntityType.Name;
                    }

                    if (ModelElement.SourceEntityType != null
                        && !string.IsNullOrEmpty(ModelElement.SourceEntityType.Name))
                    {
                        derivedName = ModelElement.SourceEntityType.Name;
                    }
                }

                return string.Format(CultureInfo.CurrentCulture, EntityDesignerRes.IsInheritedFrom, baseName, derivedName);
            }
        }

        public override string AccessibleDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.AccDesc_Inheritance,
                    EntityDesignerRes.CompClassName_Inheritance,
                    ModelElement.SourceEntityType.Name,
                    ModelElement.TargetEntityType.Name);
            }
        }

        protected override VGRoutingStyle DefaultRoutingStyle
        {
            get { return VGRoutingStyle.VGRouteOrgChartNS; }
        }

        public override bool CanMove
        {
            get { return (Partition.GetLocks() & Locks.Properties) != Locks.Properties; }
        }

        public override bool CanManuallyRoute
        {
            get { return (Partition.GetLocks() & Locks.Properties) != Locks.Properties; }
        }
    }
}
