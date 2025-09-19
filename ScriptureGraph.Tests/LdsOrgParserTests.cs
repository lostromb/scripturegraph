using Durandal.Common.Logger;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class LdsOrgParserTests
    {
        [TestMethod]
        public void TestParseScriptureRefSingleVerse()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/dc-testament/dc/98?lang=eng&id=p11#p11\">D&C 98:11</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(1, dest.Count);
            Assert.AreEqual("dc-testament dc 98:11", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefSingleVerseIntro()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/dc-testament/dc/98?lang=eng&id=intro#intro\">Intro to D&C 98</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(1, dest.Count);
            Assert.AreEqual("dc-testament dc 98:intro", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefSingleVerseIntroRange()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/bofm/4-ne/1?lang=eng&id=title1-intro1\">4 Nephi (Header)</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual("bofm 4-ne 1:title1", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
            Assert.AreEqual("bofm 4-ne 1:intro1", dest[1].ToString());
            Assert.AreEqual(false, dest[1].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefVerseRange()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/bofm/2-ne/2?lang=eng&id=p2-p4\">2 Ne. 2:2–4</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(3, dest.Count);
            Assert.AreEqual("bofm 2-ne 2:2", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 2:3", dest[1].ToString());
            Assert.AreEqual(false, dest[1].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 2:4", dest[2].ToString());
            Assert.AreEqual(false, dest[2].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefVerseRangeWithEmphasis()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/bofm/2-ne/2?lang=eng&id=p2-p4#p3\">2 Ne. 2:3 (2–4)</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(3, dest.Count);
            Assert.AreEqual("bofm 2-ne 2:2", dest[0].ToString());
            Assert.AreEqual(true, dest[0].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 2:3", dest[1].ToString());
            Assert.AreEqual(false, dest[1].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 2:4", dest[2].ToString());
            Assert.AreEqual(true, dest[2].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefTwoVersesComma()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/dc-testament/dc/128?lang=eng&id=p15,p18#p15\">D&C 128:15, 18</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual("dc-testament dc 128:15", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
            Assert.AreEqual("dc-testament dc 128:18", dest[1].ToString());
            Assert.AreEqual(true, dest[1].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefComplexRange()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/bofm/2-ne/9?lang=eng&id=p16-p18,26#p16\">2 Nephi 9:16-18,26</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(4, dest.Count);
            Assert.AreEqual("bofm 2-ne 9:16", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 9:17", dest[1].ToString());
            Assert.AreEqual(true, dest[1].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 9:18", dest[2].ToString());
            Assert.AreEqual(true, dest[2].LowEmphasis);
            Assert.AreEqual("bofm 2-ne 9:26", dest[3].ToString());
            Assert.AreEqual(true, dest[3].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefEntireChapter()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ex/18?lang=eng\">18</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(1, dest.Count);
            Assert.AreEqual("ot ex 18", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
        }

        [TestMethod]
        public void TestParseScriptureRefHugeSpan()
        {
            string html = "<a class=\"scripture-ref\" href=\"/study/scriptures/ot/ex/12?lang=eng&span=12:50-13:2#p51\">12:50–13:2</a>";
            List<ScriptureReference> dest = new List<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, dest, DebugLogger.Default);
            Assert.AreEqual(2, dest.Count);
            Assert.AreEqual("ot ex 12", dest[0].ToString());
            Assert.AreEqual(false, dest[0].LowEmphasis);
            Assert.AreEqual("ot ex 13", dest[1].ToString());
            Assert.AreEqual(false, dest[1].LowEmphasis);
        }
    }
}
