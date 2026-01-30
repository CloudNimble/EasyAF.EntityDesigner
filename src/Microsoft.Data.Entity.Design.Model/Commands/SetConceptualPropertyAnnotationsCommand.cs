// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    /// <summary>
    ///     Conceptual Properties have some optional annotations that can be set and you use this command to set them.
    /// </summary>
    internal class SetConceptualPropertyAnnotationsCommand : Command
    {
        private ConceptualProperty _property;
        private readonly string _storeGeneratedPattern;

        /// <summary>
        ///     Sets annotations on the property being created by the passed in command.
        /// </summary>
        /// <param name="prereq">Must be non-null command creating the conceptual property</param>
        /// <param name="storeGeneratedPattern">Optional annotation</param>
        internal SetConceptualPropertyAnnotationsCommand(CreatePropertyCommand prereq, string storeGeneratedPattern)
        {
            ValidatePrereqCommand(prereq);

            _storeGeneratedPattern = storeGeneratedPattern;

            AddPreReqCommand(prereq);
        }

        internal ConceptualProperty Property
        {
            get { return _property; }
        }

        protected override void ProcessPreReqCommands()
        {
            if (_property == null)
            {
                CreatePropertyCommand prereq = GetPreReqCommand(CreatePropertyCommand.PrereqId) as CreatePropertyCommand;
                Debug.Assert(null != prereq, "Pre-req CreatePropertyCommand is not present for PrereqId " + CreatePropertyCommand.PrereqId);
                if (null != prereq)
                {
                    var prop = prereq.CreatedProperty;
                    Debug.Assert(null != prop, "Pre-req Command returned null Property");
                    if (null != prop)
                    {
                        _property = prop as ConceptualProperty;
                        Debug.Assert(
                            null != _property,
                            "Pre-req Command returned Property of type " + prop.GetType().FullName + ". Should be ConceptualProperty");
                    }
                }
            }
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            Debug.Assert(Property != null, "InvokeInternal is called when Property is null.");
            if (null == Property)
            {
                throw new InvalidOperationException("InvokeInternal is called when Property is null");
            }

            if (!string.IsNullOrEmpty(_storeGeneratedPattern))
            {
                Property.StoreGeneratedPattern.Value = _storeGeneratedPattern;
            }
        }
    }
}
