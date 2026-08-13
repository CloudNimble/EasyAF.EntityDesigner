// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package
{
    internal class ModelGenErrorCache
    {
        private readonly Dictionary<string, List<EdmSchemaError>> _errors;

        internal ModelGenErrorCache()
        {
            _errors = [];
        }

        // virtual to allow mocking
        internal virtual void AddErrors(string fileName, List<EdmSchemaError> errors)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(fileName), "invalid file name");
            Debug.Assert(errors != null && errors.Any(), "expected non-empty error collection");

            _errors[fileName] = errors;
        }

        // virtual to allow mocking
        internal virtual void RemoveErrors(string fileName)
        {
            _errors.Remove(fileName);
        }

        internal List<EdmSchemaError> GetErrors(string fileName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(fileName), "invalid file name");

            _errors.TryGetValue(fileName, out List<EdmSchemaError> errors);
            return errors;
        }
    }
}
