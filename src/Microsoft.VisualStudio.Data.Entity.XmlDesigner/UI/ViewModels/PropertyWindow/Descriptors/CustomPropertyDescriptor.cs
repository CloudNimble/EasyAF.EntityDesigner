// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    /// <summary>
    ///     base class for a PropertyDescriptor that describes a property of an EFElement
    /// </summary>
    internal abstract class CustomPropertyDescriptor : PropertyDescriptor
    {
        /// <summary>
        ///     the component that owns the property
        /// </summary>
        private readonly object _component;

        private readonly EditingContext _editingContext;

        protected CustomPropertyDescriptor(EditingContext editingContext, object component, string name, Attribute[] attrs)
            : base(name, attrs)
        {
            _component = component;
            _editingContext = editingContext;
        }

        internal object Component
        {
            get { return _component; }
        }

        internal EditingContext EditingContext
        {
            get { return _editingContext; }
        }

        #region PropertyDescriptor implementation

        public override Type ComponentType
        {
            get { return _component.GetType(); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return typeof(string); }
        }

        public override bool IsBrowsable
        {
            get { return true; }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override void ResetValue(object component)
        {
            // reset the property value within a transaction context
            UpdatePropertyValue(ResetEFElementValue, UndoString);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override object GetValue(object component)
        {
            return GetEFElementValue();
        }

        public override void SetValue(object component, object value)
        {
            // set the new property value within a transaction context
            UpdatePropertyValue(delegate { SetEFElementValue(value); }, UndoString);
        }

        #endregion

        protected internal abstract string UndoString { get; }

        private delegate void UpdatePropertyValueCallback();

        /// <summary>
        ///     Update the property value within a transaction context
        /// </summary>
        /// <param name="updateCallback"></param>
        private void UpdatePropertyValue(UpdatePropertyValueCallback updatePropertyValueCallback, string txName)
        {
            try
            {
                PropertyWindowViewModelHelper.CreateCommandProcessorContext(_editingContext, txName);
                updatePropertyValueCallback();
            }
            finally
            {
                PropertyWindowViewModelHelper.RemoveCommandProcessorContext();
            }
        }

        protected abstract object GetEFElementValue();

        protected virtual void SetEFElementValue(object value)
        {
            Debug.Fail(
                "EFPropertyDescriptor.SetEFElementValue should never be invoked. Either it should be overriden in the derived class, or IsReadOnly should return true.");
        }

        protected virtual void ResetEFElementValue()
        {
            Debug.Fail(
                "EFPropertyDescriptor.ResetEFElementValue should never be invoked. Either it should be overriden in the derived class, or CanResetValue should return false.");
        }
    }

}
