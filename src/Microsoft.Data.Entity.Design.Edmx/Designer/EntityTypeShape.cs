// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Tools.Model.Diagram;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{
    internal class EntityTypeShape : BaseDiagramObject
    {
        internal static readonly string ElementName = "EntityTypeShape";
        internal static readonly string AttributeEntityType = "EntityType";
        internal static readonly string AttributePointX = "PointX";
        internal static readonly string AttributePointY = "PointY";
        internal static readonly string AttributeWidth = "Width";
        internal static readonly string AttributeIsExpanded = "IsExpanded";
        internal static readonly string AttributeFillColor = "FillColor";
        internal static readonly string AttributeGroupName = "GroupName";

        private SingleItemBinding<EntityType> _entityTypeBinding;
        private DefaultableValue<double> _pointXAttr;
        private DefaultableValue<double> _pointYAttr;
        private DefaultableValue<double> _widthAttr;
        private DefaultableValue<bool> _isExpandedAttr;
        private DefaultableValue<Color> _fillColorAttr;
        private DefaultableValue<string> _groupNameAttr;

        internal EntityTypeShape(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        #region BaseDiagramObject override

        internal override EFObject ModelItem
        {
            get
            {
                if (EntityType.Status == BindingStatus.Known)
                {
                    return EntityType.SafeTarget;
                }
                return null;
            }
        }

        #endregion

        /// <summary>
        ///     A bindable reference to the EntityType for this shape
        /// </summary>
        internal SingleItemBinding<EntityType> EntityType
        {
            get
            {
                _entityTypeBinding ??= new SingleItemBinding<EntityType>(
                        this,
                        AttributeEntityType,
                        EFNormalizableItemDefaults.DefaultNameNormalizerForDesigner);

                return _entityTypeBinding;
            }
        }

        internal DefaultableValue<double> PointX
        {
            get
            {
                _pointXAttr ??= new ConnectorPoint.PointXDefaultableValue(this);
                return _pointXAttr;
            }
        }

        internal DefaultableValue<double> PointY
        {
            get
            {
                _pointYAttr ??= new ConnectorPoint.PointYDefaultableValue(this);
                return _pointYAttr;
            }
        }

        internal DefaultableValue<Color> FillColor
        {
            get
            {
                _fillColorAttr ??= new FillColorDefaultableValue(this);
                return _fillColorAttr;
            }
        }

        /// <summary>
        ///     The group this shape belongs to, used to place related shapes together.
        /// </summary>
        /// <remarks>
        ///     Per shape rather than per entity, so two diagrams over one model can group differently. Empty until
        ///     something fills it in: a modern layout writes its detected groups here on the first pass and never
        ///     overwrites a value that is already present, which is what lets the names be edited by hand and
        ///     honoured from then on. See specs/diagram-layout-engines.md.
        /// </remarks>
        internal DefaultableValue<string> GroupName
        {
            get
            {
                _groupNameAttr ??= new GroupNameDefaultableValue(this);
                return _groupNameAttr;
            }
        }

        private class WidthDefaultableValue : DefaultableValue<double>
        {
            internal WidthDefaultableValue(EFElement parent)
                : base(parent, AttributeWidth)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeWidth; }
            }

            public override double DefaultValue
            {
                get { return 0.0; }
            }
        }

        internal override DeleteEFElementCommand GetDeleteCommand()
        {
            return new DeleteEntityTypeShapeCommand(this);
        }

        private class IsExpandedDefaultableValue : DefaultableValue<bool>
        {
            internal IsExpandedDefaultableValue(EFElement parent)
                : base(parent, AttributeIsExpanded)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeIsExpanded; }
            }

            public override bool DefaultValue
            {
                get { return EntityDesignerDiagramConstant.IsDefaultShapeExpanded; }
            }
        }

        internal DefaultableValue<double> Width
        {
            get
            {
                _widthAttr ??= new WidthDefaultableValue(this);
                return _widthAttr;
            }
        }

        internal DefaultableValue<bool> IsExpanded
        {
            get
            {
                _isExpandedAttr ??= new IsExpandedDefaultableValue(this);
                return _isExpandedAttr;
            }
        }

        // Should return the EntityType's DisplayName.
        internal override string DisplayName
        {
            get
            {
                if (EntityType != null
                    && EntityType.Target != null)
                {
                    return EntityType.Target.DisplayName;
                }
                return String.Empty;
            }
        }

        #region overrides

        // we unfortunately get a warning from the compiler when we use the "base" keyword in "iterator" types generated by using the
        // "yield return" keyword.  By adding this method, I was able to get around this.  Unfortunately, I wasn't able to figure out
        // a way to implement this once and have derived classes share the implementation (since the "base" keyword is resolved at 
        // compile-time and not at runtime.
        private IEnumerable<EFObject> BaseChildren
        {
            get { return base.Children; }
        }

        internal override IEnumerable<EFObject> Children
        {
            get
            {
                foreach (var efobj in BaseChildren)
                {
                    yield return efobj;
                }

                yield return EntityType;
                yield return PointX;
                yield return PointY;
                yield return Width;
                yield return IsExpanded;
                yield return FillColor;
                yield return GroupName;
            }
        }

#if DEBUG
        internal override ICollection<string> MyAttributeNames()
        {
            var s = base.MyAttributeNames();
            s.Add(AttributeEntityType);
            s.Add(AttributePointX);
            s.Add(AttributePointY);
            s.Add(AttributeWidth);
            s.Add(AttributeIsExpanded);
            s.Add(AttributeFillColor);
            s.Add(AttributeGroupName);
            return s;
        }
#endif

        protected override void PreParse()
        {
            Debug.Assert(State != EFElementState.Parsed, "this object should not already be in the parsed state");

            ClearEFObject(_entityTypeBinding);
            _entityTypeBinding = null;
            ClearEFObject(_pointXAttr);
            _pointXAttr = null;
            ClearEFObject(_pointYAttr);
            _pointYAttr = null;
            ClearEFObject(_widthAttr);
            _widthAttr = null;
            ClearEFObject(_isExpandedAttr);
            _isExpandedAttr = null;
            ClearEFObject(_fillColorAttr);
            _fillColorAttr = null;
            ClearEFObject(_groupNameAttr);
            _groupNameAttr = null;
            base.PreParse();
        }

        protected override void DoResolve(EFArtifactSet artifactSet)
        {
            EntityType.Rebind();
            if (EntityType.Status == BindingStatus.Known)
            {
                State = EFElementState.Resolved;
            }
        }

        #endregion

        #region Helper Classes

        private class GroupNameDefaultableValue : DefaultableValue<string>
        {
            internal GroupNameDefaultableValue(EFElement parent)
                : base(parent, AttributeGroupName)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeGroupName; }
            }

            public override string DefaultValue
            {
                get { return String.Empty; }
            }
        }

        private class FillColorDefaultableValue : DefaultableValue<Color>
        {
            internal FillColorDefaultableValue(EFElement parent)
                : base(parent, AttributeFillColor)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeFillColor; }
            }

            public override Color DefaultValue
            {
                get { return EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor; }
            }

            protected internal override string ConvertValueToString(Color val)
            {
                var converter = TypeDescriptor.GetConverter(typeof(Color));
                return converter.ConvertToInvariantString(val);
            }

            protected internal override Color ConvertStringToValue(string stringVal)
            {
                // Check for TypeConverter
                var converter = TypeDescriptor.GetConverter(typeof(Color));
                if (converter != null
                    && converter.CanConvertFrom(typeof(string)))
                {
                    // Convert to culture invariant string.
                    return (Color)converter.ConvertFromInvariantString(stringVal);
                }
                else
                {
                    Debug.Fail("Unable to convert string value:" + stringVal + " to a Color type.");
                    return DefaultValue;
                }
            }
        }

        #endregion
    }
}
