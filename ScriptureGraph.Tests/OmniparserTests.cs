using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class OmniparserTests
    {
        [TestMethod]
        public void TestOmniparser_PlainScripture_SingleVerse1() => TestParserOutput(
            "D & C 20:5",
            FeatureToNodeMapping.ScriptureVerse("dc", 20, 5));

        [TestMethod]
        public void TestOmniparser_PlainScripture_SingleVerse2() => TestParserOutput(
            "(Ps. 107:20.)",
            FeatureToNodeMapping.ScriptureVerse("ps", 107, 20));

        [TestMethod]
        public void TestOmniparser_PlainScripture_BookNameBoundaries() => TestParserOutput("joseph 1:5");

        [TestMethod]
        public void TestOmniparser_Edersheim() => TestParserOutput("Edersheim 1:422-23.");

        [TestMethod]
        public void TestOmniparser_PlainScripture_SeveralVerses1() => TestParserOutput(
            "(John 4:4-6; JST John 4:2, 6-7)",
            FeatureToNodeMapping.ScriptureVerse("john", 4, 4),
            FeatureToNodeMapping.ScriptureVerse("john", 4, 5),
            FeatureToNodeMapping.ScriptureVerse("john", 4, 6),
            FeatureToNodeMapping.ScriptureVerse("john", 4, 7),
            FeatureToNodeMapping.ScriptureVerse("john", 4, 2));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_SingleVerse1() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/nt/2-pet/1?lang=eng&id=p21#p21\">2 Pet. 1:21</a>",
            FeatureToNodeMapping.ScriptureVerse("2-pet", 1, 21));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_SingleVerse2() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/dc-testament/dc/5?lang=eng&id=p2#p2\">D&C 5:2</a>",
            FeatureToNodeMapping.ScriptureVerse("dc", 5, 2));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_VerseRange() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ezek/37?lang=eng&id=p15-p16#p15\">Ezek. 37:15–16</a>",
            FeatureToNodeMapping.ScriptureVerse("ezek", 37, 15),
            FeatureToNodeMapping.ScriptureVerse("ezek", 37, 16));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_MultiChapterSpan() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ex/12?lang=eng&span=12:50-13:2#p37\">12:50-13:2</a>",
            FeatureToNodeMapping.ScriptureVerse("ex", 12, 50),
            FeatureToNodeMapping.ScriptureVerse("ex", 12, 51),
            FeatureToNodeMapping.ScriptureVerse("ex", 13, 1),
            FeatureToNodeMapping.ScriptureVerse("ex", 13, 2));

        [TestMethod]
        public void TestOmniparser_ScriptureUrlOldFormat_VerseRange() => TestParserOutput(
            "https://www.churchofjesuschrist.org/scriptures/dc-testament/dc/128.22-23?lang=eng",
            FeatureToNodeMapping.ScriptureVerse("dc", 128, 22),
            FeatureToNodeMapping.ScriptureVerse("dc", 128, 23));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_GSTopic() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/gs/apocrypha?lang=eng\">Apocrypha</a>",
            FeatureToNodeMapping.GuideToScripturesTopic("apocrypha"));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_GSTopic2() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/scriptures/gs/ephraim?lang=eng#sec_the_stick_of_ephraim_or_joseph\">Ephraim—The stick of Ephraim or Joseph</a>",
            FeatureToNodeMapping.GuideToScripturesTopic("ephraim"));

        [TestMethod]
        public void TestOmniparser_Hymn1() => TestParserOutput("“Praise to the Man,” Hymns, no. 27", FeatureToNodeMapping.Hymn("praise-to-the-man"));
        [TestMethod]
        public void TestOmniparser_Hymn2() => TestParserOutput("\"Hymns, no. 153\"", FeatureToNodeMapping.Hymn("lord-we-ask-thee-ere-we-part"));
        [TestMethod]
        public void TestOmniparser_Hymn3() => TestParserOutput("Hymns, 1988, no 115", FeatureToNodeMapping.Hymn("come-ye-disconsolate"));

        [TestMethod]
        public void TestOmniparser_LivingChrist1() => TestParserOutput("/study/scriptures/the-living-christ-the-testimony-of-the-apostles/the-living-christ-the-testimony-of-the-apostles?lang=eng", FeatureToNodeMapping.Proclamation("lc"));
        [TestMethod]
        public void TestOmniparser_LivingChrist2() => TestParserOutput("The Living Christ, The Testimony of the Apostles", FeatureToNodeMapping.Proclamation("lc"));
        [TestMethod]
        public void TestOmniparser_LivingChrist3() => TestParserOutput("The Living Christ Testimony of the Apostles", FeatureToNodeMapping.Proclamation("lc"));
        [TestMethod]
        public void TestOmniparser_LivingChrist4() => TestParserOutput("The Living Christ declaration", FeatureToNodeMapping.Proclamation("lc"));
        [TestMethod]
        public void TestOmniparser_LivingChrist5() => TestParserOutput("The Living Christ proclamation", FeatureToNodeMapping.Proclamation("lc"));
        [TestMethod]
        public void TestOmniparser_LivingChrist6() => TestParserOutput("The Living Christ testimony", FeatureToNodeMapping.Proclamation("lc"));

        [TestMethod]
        public void TestOmniparser_FamilyProc1() => TestParserOutput("The Family: A Proclamation to the World", FeatureToNodeMapping.Proclamation("fam"));
        [TestMethod]
        public void TestOmniparser_FamilyProc2() => TestParserOutput("/study/scriptures/the-family-a-proclamation-to-the-world/the-family-a-proclamation-to-the-world?lang=eng", FeatureToNodeMapping.Proclamation("fam"));
        [TestMethod]
        public void TestOmniparser_FamilyProc3() => TestParserOutput("The family proclamation", FeatureToNodeMapping.Proclamation("fam"));
        [TestMethod]
        public void TestOmniparser_FamilyProc4() => TestParserOutput("The Family: Proclamation to the World", FeatureToNodeMapping.Proclamation("fam"));
        [TestMethod]
        public void TestOmniparser_FamilyProc5() => TestParserOutput("Proclamation on The Family", FeatureToNodeMapping.Proclamation("fam"));


        [TestMethod]
        public void TestOmniparser_ScriptureUrl_ConferenceLink() => TestParserOutput(
            "<a class=\"scripture-ref\" href=\"/study/general-conference/2021/04/49nelson?lang=eng\">Russell M. Nelson</a>",
            FeatureToNodeMapping.ConferenceTalk(2021, Core.Schemas.ConferencePhase.April, "49nelson"));

        [TestMethod]
        public void TestOmniparser_ScriptureUrl_LiahonaLink() => TestParserOutput(
            "https://www.churchofjesuschrist.org/study/liahona/2021/05/49nelson?lang=eng",
            FeatureToNodeMapping.ConferenceTalk(2021, Core.Schemas.ConferencePhase.April, "49nelson"));

        private static void TestParserOutput(string input, params KnowledgeGraphNodeId[] expectedOutput)
        {
            ILogger logger = new ConsoleLogger();
            OmniParserOutput[] output = OmniParser.ParseHtml(input, logger, LanguageCode.ENGLISH).ToArray();
            Assert.AreEqual(expectedOutput.Length, output.Length);
            foreach (KnowledgeGraphNodeId expected in expectedOutput)
            {
                OmniParserOutput match = output.FirstOrDefault(s => s.Node.Equals(expected));
                Assert.AreNotEqual(match, default(OmniParserOutput), $"Did not find {expected.ToString()} in output");
                Assert.AreEqual(expected, match.Node, $"Did not find {expected.ToString()} in output");
            }
        }
    }
}
