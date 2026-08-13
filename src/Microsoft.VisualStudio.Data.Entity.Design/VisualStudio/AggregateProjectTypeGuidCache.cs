// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using EnvDTE;

namespace Microsoft.VisualStudio.Data.Entity.Design.VisualStudio
{
    internal class AggregateProjectTypeGuidCache
    {
        private readonly Dictionary<Project, string> _cache;

        internal AggregateProjectTypeGuidCache()
        {
            _cache = [];
        }

        internal void Add(Project project, string guids)
        {
            if (_cache.ContainsKey(project) == false)
            {
                _cache.Add(project, guids);
            }
        }

        internal void Remove(Project project)
        {
            if (_cache.ContainsKey(project))
            {
                _cache.Remove(project);
            }
        }

        internal string GetGuids(Project project)
        {
            return _cache.ContainsKey(project) ? _cache[project] : null;
        }
    }
}
