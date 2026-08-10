// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Dsl.Rules;
using Microsoft.Data.Entity.Design.Dsl.ViewModel;

namespace Microsoft.Data.Entity.Design.Dsl.ModelChanges
{
    internal abstract class PropertyModelChange : ViewModelChange
    {
        private readonly Property _property;

        protected PropertyModelChange(Property property)
        {
            _property = property;
        }

        public Property Property
        {
            get { return _property; }
        }
    }
}
