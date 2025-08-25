using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public enum TrainingFeatureType
    {
        Unknown,

        /// <summary>
        /// Generally low weight. Relations of words and ngrams to entities and to each other.
        /// </summary>
        WordAssociation,

        /// <summary>
        /// Used for words specifically that directly designate an entity. Think like the name of an entity as opposed
        /// to just random words that describe it (which would be word associations)
        /// </summary>
        WordDesignation,

        /// <summary>
        /// Unique references directly between entities (in this case, like a scripture having a footnote linking to another)
        /// </summary>
        EntityReference
    }
}
