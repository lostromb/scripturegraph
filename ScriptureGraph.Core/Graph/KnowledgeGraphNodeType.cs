using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public enum KnowledgeGraphNodeType : ushort
    {
        /// <summary>
        /// Default value, usually indicates error
        /// </summary>
        Unknown,

        /// <summary>
        /// A platonic ideal entity, designated by language-agnostic name
        /// </summary>
        Entity,

        /// <summary>
        /// A single word with a language code attached (in an invariant form such as lower case)
        /// </summary>
        Word,

        /// <summary>
        /// A series of N words with a language code attached
        /// </summary>
        NGram,

        /// <summary>
        /// A single designated book + chapter + verse within scripture
        /// Multiple verses will be designated by multiple nodes or features.
        /// </summary>
        ScriptureVerse,

        /// <summary>
        /// A reference to the topical guide, designated by URL keyword (e.g. scriptures/tg/sobriety where "sobriety" is the node name)
        /// </summary>
        TopicalGuideKeyword,

        /// <summary>
        /// A chapter of a book of scripture, e.g. "Hebrews 10"
        /// </summary>
        ScriptureChapter,

        /// <summary>
        /// An entire book of scripture, e.g. "Hebrews"
        /// </summary>
        ScriptureBook,

        /// <summary>
        /// A reference to the bible dictionary, designated by URL keyword (e.g. scriptures/bg/abaddon where "abaddon" is the node name)
        /// </summary>
        BibleDictionaryTopic,

        /// <summary>
        /// A numbered paragraph within a bible dictionary entry
        /// </summary>
        BibleDictionaryParagraph,

        /// <summary>
        /// A reference to the Guide to the Scriptures, designated by URL keyword (e.g. scriptures/gs/abraham where "abraham" is the node name)
        /// </summary>
        GuideToScripturesTopic,

        /// <summary>
        /// A reference to the Index to Triple Combination, designated by URL keyword (e.g. scriptures/triple-index/elias where "elias" is the node name)
        /// </summary>
        TripleIndexTopic,

        /// <summary>
        /// An entire talk given in general conference
        /// </summary>
        ConferenceTalk,

        /// <summary>
        /// A single paragraph within a conference talk
        /// </summary>
        ConferenceTalkParagraph,

        /// <summary>
        /// A speaker at conference
        /// </summary>
        ConferenceSpeaker,

        /// <summary>
        /// A series of N characters in any language
        /// </summary>
        CharNGram,

        /// <summary>
        /// A paragraph in scripture that does not have a verse number. This includes study summary headers,
        /// introductions (common in D&C), and in-text editorial notes ("The commandments of Alma to his son Shiblon...");
        /// </summary>
        ScriptureSupplementalPara,

        BookChapter,

        BookParagraph,

        Conference,

        Year,

        ByuSpeech,

        ByuSpeechParagraph,

        Hymn,

        HymnVerse,

        Proclamation,

        ProclamationParagraph,

        // Sentence-level entities

        ScriptureSentence, // ScriptureVerse
        BibleDictionarySentence, // BibleDictionaryParagraph
        ConferenceTalkSentence, // ConferenceTalkParagraph
        ScriptureSupplementalParaSentence, // ScriptureSupplementalPara
        BookSentence, // BookParagraph
        ByuSpeechSentence, // ByuSpeechParagraph
        ProclamationSentence, // ProclamationParagraph
    }
}
