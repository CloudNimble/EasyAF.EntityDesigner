// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{

    internal abstract class InheritanceModelChange : ViewModelChange
    {
        private readonly Inheritance _inheritance;

        protected InheritanceModelChange(Inheritance inheritance)
        {
            _inheritance = inheritance;
        }

        public Inheritance Inheritance
        {
            get { return _inheritance; }
        }
    }

}
