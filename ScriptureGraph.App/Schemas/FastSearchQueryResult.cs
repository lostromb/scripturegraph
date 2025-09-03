﻿using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App.Schemas
{
    internal class FastSearchQueryResult
    {
        internal required KnowledgeGraphNodeId[] EntityIds;
        internal SearchResultEntityType EntityType;
        internal required string DisplayName;
    }
}
