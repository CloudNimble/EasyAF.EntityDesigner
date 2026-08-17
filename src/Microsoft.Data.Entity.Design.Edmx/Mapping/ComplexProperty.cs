// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Mapping
{
    internal class ComplexProperty : EFElement
    {
        internal static readonly string ElementName = "ComplexProperty";
        internal static readonly string AttributeName = "Name";
        internal static readonly string AttributeTypeName = "TypeName";
        internal static readonly string AttributeIsPartial = "IsPartial";

        private SingleItemBinding<ComplexConceptualProperty> _name;
        private SingleItemBinding<ComplexType> _typeName;
        private DefaultableValue<bool> _isPartial;

        private readonly List<ScalarProperty> _scalarProperties = [];
        private readonly List<ComplexProperty> _complexProperties = [];
        private readonly List<ComplexTypeMapping> _complexTypeMappings = [];
        private readonly List<Condition> _conditions = [];

        internal ComplexProperty(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        internal SingleItemBinding<ComplexConceptualProperty> Name
        {
            get
            {
                _name ??= new SingleItemBinding<ComplexConceptualProperty>(
                        this,
                        AttributeName,
                        ProperyMappingNameNormalizer.NameNormalizer
                        );
                return _name;
            }
        }

        internal SingleItemBinding<ComplexType> TypeName
        {
            get
            {
                _typeName ??= new SingleItemBinding<ComplexType>(
                        this, AttributeTypeName, EFNormalizableItemDefaults.DefaultNameNormalizerForMSL);
                return _typeName;
            }
        }

        internal DefaultableValue<bool> IsPartial
        {
            get
            {
                _isPartial ??= new IsPartialDefaultableValue(this);
                return _isPartial;
            }
        }

        private class IsPartialDefaultableValue : DefaultableValue<bool>
        {
            internal IsPartialDefaultableValue(EFElement parent)
                : base(parent, AttributeIsPartial)
            {
            }

            internal override string AttributeName
            {
                get { return AttributeIsPartial; }
            }

            public override bool DefaultValue
            {
                get { return false; }
            }
        }

        internal IList<ScalarProperty> ScalarProperties()
        {
            return _scalarProperties.AsReadOnly();
        }

        internal void AddScalarProperty(ScalarProperty prop)
        {
            _scalarProperties.Add(prop);
        }

        internal IList<ComplexProperty> ComplexProperties()
        {
            return _complexProperties.AsReadOnly();
        }

        internal void AddComplexProperty(ComplexProperty prop)
        {
            _complexProperties.Add(prop);
        }

        // returns the list of ScalarProperties including those in ComplexProperty children
        internal IEnumerable<ScalarProperty> AllScalarProperties()
        {
            foreach (var sp in _scalarProperties)
            {
                yield return sp;
            }

            foreach (var cp in _complexProperties)
            {
                foreach (var sp in cp.AllScalarProperties())
                {
                    yield return sp;
                }
            }
        }

        internal IList<ComplexTypeMapping> ComplexTypeMappings()
        {
            return _complexTypeMappings.AsReadOnly();
        }

        internal IList<Condition> Conditions()
        {
            return _conditions.AsReadOnly();
        }

        internal ScalarProperty FindScalarProperty(Property property, Property column)
        {
            foreach (var sp in _scalarProperties)
            {
                if (sp.Name.Target == property
                    && sp.ColumnName.Target == column)
                {
                    return sp;
                }
            }

            return null;
        }

        internal ComplexProperty FindComplexProperty(Property property)
        {
            foreach (var cp in _complexProperties)
            {
                if (cp.Name.Target == property)
                {
                    return cp;
                }
            }

            return null;
        }

        internal MappingFragment MappingFragment
        {
            get { return GetParentOfType(typeof(MappingFragment)) as MappingFragment; }
        }

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

                foreach (var child in ScalarProperties())
                {
                    yield return child;
                }

                foreach (var child2 in ComplexProperties())
                {
                    yield return child2;
                }

                foreach (var child3 in ComplexTypeMappings())
                {
                    yield return child3;
                }

                foreach (var child4 in Conditions())
                {
                    yield return child4;
                }

                yield return Name;
                yield return TypeName;
                yield return IsPartial;
            }
        }

        protected override void OnChildDeleted(EFContainer efContainer)
        {
            if (efContainer is ScalarProperty sp)
            {
                _scalarProperties.Remove(sp);
                return;
            }

            if (efContainer is ComplexProperty cp)
            {
                _complexProperties.Remove(cp);
                return;
            }

            if (efContainer is ComplexTypeMapping ctm)
            {
                _complexTypeMappings.Remove(ctm);
                return;
            }

            if (efContainer is Condition cond)
            {
                _conditions.Remove(cond);
                return;
            }

            base.OnChildDeleted(efContainer);
        }

#if DEBUG
        internal override ICollection<string> MyAttributeNames()
        {
            var s = base.MyAttributeNames();
            s.Add(AttributeName);
            s.Add(AttributeTypeName);
            s.Add(AttributeIsPartial);
            return s;
        }
#endif

#if DEBUG
        internal override ICollection<string> MyChildElementNames()
        {
            var s = base.MyChildElementNames();
            s.Add(ScalarProperty.ElementName);
            s.Add(ElementName);
            s.Add(ComplexTypeMapping.ElementName);
            s.Add(Condition.ElementName);
            return s;
        }
#endif

        protected override void PreParse()
        {
            Debug.Assert(State != EFElementState.Parsed, "this object should not already be in the parsed state");

            ClearEFObject(_name);
            _name = null;

            ClearEFObject(_typeName);
            _typeName = null;

            ClearEFObject(_isPartial);
            _isPartial = null;

            ClearEFObjectCollection(_scalarProperties);
            ClearEFObjectCollection(_complexProperties);
            ClearEFObjectCollection(_complexTypeMappings);
            ClearEFObjectCollection(_conditions);

            base.PreParse();
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            if (elem.Name.LocalName == ScalarProperty.ElementName)
            {
                ScalarProperty prop = new ScalarProperty(this, elem);
                _scalarProperties.Add(prop);
                prop.Parse(unprocessedElements);
            }
            else if (elem.Name.LocalName == ElementName)
            {
                ComplexProperty complexProperty = new ComplexProperty(this, elem);
                _complexProperties.Add(complexProperty);
                complexProperty.Parse(unprocessedElements);
            }
            else if (elem.Name.LocalName == ComplexTypeMapping.ElementName)
            {
                ComplexTypeMapping complexTypeMapping = new ComplexTypeMapping(this, elem);
                _complexTypeMappings.Add(complexTypeMapping);
                complexTypeMapping.Parse(unprocessedElements);
            }
            else if (elem.Name.LocalName == Condition.ElementName)
            {
                Condition cond = new Condition(this, elem);
                _conditions.Add(cond);
                cond.Parse(unprocessedElements);
            }
            else
            {
                return base.ParseSingleElement(unprocessedElements, elem);
            }
            return true;
        }

        protected override void DoResolve(EFArtifactSet artifactSet)
        {
            Name.Rebind();
            TypeName.Rebind();
            // TypeName attribute is optional so its status might be undefined
            if (Name.Status == BindingStatus.Known
                && (TypeName.Status == BindingStatus.Known || TypeName.Status == BindingStatus.Undefined))
            {
                State = EFElementState.Resolved;
            }
        }

        internal override DeleteEFElementCommand GetDeleteCommand()
        {
            return new DeleteComplexPropertyCommand(this);
        }

        internal override void GetXLinqInsertPosition(EFElement child, out XNode insertAt, out bool insertBefore)
        {
            if (child is ScalarProperty)
            {
                insertAt = FirstChildXElementOrNull();
                insertBefore = true;
            }
            else
            {
                base.GetXLinqInsertPosition(child, out insertAt, out insertBefore);
            }
        }
    }
}
