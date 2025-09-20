using Durandal.Common.Logger;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class CommonParserTests
    {
        [TestMethod]
        public void TestParseScriptureRef_SingleVerse()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/nt/2-pet/1?lang=eng&id=p21#p21\">2 Pet. 1:21</a>", refs, logger);
            Assert.AreEqual(1, refs.Count);
            Assert.AreEqual("nt", refs[0].Canon);
            Assert.AreEqual("2-pet", refs[0].Book);
            Assert.AreEqual(1, refs[0].Chapter);
            Assert.AreEqual(21, refs[0].Verse);
        }

        [TestMethod]
        public void TestParseScriptureRef_GSTopic()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/gs/apocrypha?lang=eng\">Apocrypha</a>", refs, logger);
            Assert.AreEqual(1, refs.Count);
            Assert.AreEqual("gs", refs[0].Canon);
            Assert.AreEqual("apocrypha", refs[0].Book);
            Assert.AreEqual(null, refs[0].Chapter);
            Assert.AreEqual(null, refs[0].Verse);
        }
        
        [TestMethod]
        public void TestParseScriptureRef_VerseRange()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ezek/37?lang=eng&id=p15-p16#p15\">Ezek. 37:15–16</a>", refs, logger);
            Assert.AreEqual(2, refs.Count);
            Assert.AreEqual("ot", refs[0].Canon);
            Assert.AreEqual("ezek", refs[0].Book);
            Assert.AreEqual(37, refs[0].Chapter);
            Assert.AreEqual(15, refs[0].Verse);
            Assert.AreEqual("ot", refs[1].Canon);
            Assert.AreEqual("ezek", refs[1].Book);
            Assert.AreEqual(37, refs[1].Chapter);
            Assert.AreEqual(16, refs[1].Verse);
        }

        [TestMethod]
        public void TestParseScriptureRef_SingleVerseEmphasis()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/dc-testament/dc/5?lang=eng&id=p2#p2\">D&C 5:2</a>", refs, logger);
            Assert.AreEqual(1, refs.Count);
            Assert.AreEqual("dc-testament", refs[0].Canon);
            Assert.AreEqual("dc", refs[0].Book);
            Assert.AreEqual(5, refs[0].Chapter);
            Assert.AreEqual(2, refs[0].Verse);
        }

        [TestMethod]
        public void TestParseScriptureRef_Section()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/gs/ephraim?lang=eng#sec_the_stick_of_ephraim_or_joseph\">Ephraim—The stick of Ephraim or Joseph</a>", refs, logger);
            Assert.AreEqual(1, refs.Count);
            Assert.AreEqual("gs", refs[0].Canon);
            Assert.AreEqual("ephraim", refs[0].Book);
            Assert.AreEqual(null, refs[0].Chapter);
            Assert.AreEqual(null, refs[0].Verse);
            //Assert.AreEqual("sec_the_stick_of_ephraim_or_joseph", refs[0].Paragraph);
        }

        [TestMethod]
        public void TestParseScriptureRef_MultiChapterRange()
        {
            ILogger logger = new ConsoleLogger();
            List<ScriptureReference> refs = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences("<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ex/12?lang=eng&span=12:37-13:16#p37\">12:37–13:16</a>", refs, logger);
            Assert.AreEqual(31, refs.Count);
            Assert.AreEqual("ot", refs[0].Canon);
            Assert.AreEqual("ex", refs[0].Book);
            Assert.AreEqual(12, refs[0].Chapter);
            Assert.AreEqual(37, refs[0].Verse);
        }
    }
}
