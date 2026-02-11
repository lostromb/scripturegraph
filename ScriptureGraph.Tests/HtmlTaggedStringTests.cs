using ScriptureGraph.Core.Schemas;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class HtmlTaggedStringTests
    {
        [TestMethod]
        public void Test_HtmlTaggedString_BasicTag()
        {
            string inputHtml = "This is <b>a test</b> of HTML parsing.";
            HtmlTaggedString parsedVal = HtmlTaggedString.Parse(inputHtml);
            Assert.IsNotNull(parsedVal);
            Assert.AreEqual("This is a test of HTML parsing.", parsedVal.Text);
            Assert.AreEqual(2, parsedVal.Tags.Count);
            Assert.AreEqual("b", parsedVal.Tags[0].Tag.TagName);
            Assert.IsFalse(parsedVal.Tags[0].Tag.IsClosing);
            Assert.AreEqual(8, parsedVal.Tags[0].Index);
            Assert.AreEqual("b", parsedVal.Tags[1].Tag.TagName);
            Assert.IsTrue(parsedVal.Tags[1].Tag.IsClosing);
            Assert.AreEqual(14, parsedVal.Tags[1].Index);
        }

        [TestMethod]
        public void Test_HtmlTaggedString_SelfClosingTag()
        {
            string inputHtml = "This is a test.<br/>";
            HtmlTaggedString parsedVal = HtmlTaggedString.Parse(inputHtml);
            Assert.IsNotNull(parsedVal);
            Assert.AreEqual("This is a test.", parsedVal.Text);
            Assert.AreEqual(2, parsedVal.Tags.Count);
            Assert.AreEqual("br", parsedVal.Tags[0].Tag.TagName);
            Assert.IsFalse(parsedVal.Tags[0].Tag.IsClosing);
            Assert.AreEqual(15, parsedVal.Tags[0].Index);
            Assert.AreEqual("br", parsedVal.Tags[1].Tag.TagName);
            Assert.IsTrue(parsedVal.Tags[1].Tag.IsClosing);
            Assert.AreEqual(15, parsedVal.Tags[1].Index);
        }

        [TestMethod]
        public void Test_HtmlTaggedString_TagAttributes()
        {
            string inputHtml = "<a href=\"link\" target=\"_blank\">This</a> is a test.";
            HtmlTaggedString parsedVal = HtmlTaggedString.Parse(inputHtml);
            Assert.IsNotNull(parsedVal);
            Assert.AreEqual("This is a test.", parsedVal.Text);
            Assert.AreEqual(2, parsedVal.Tags.Count);
            Assert.AreEqual("a", parsedVal.Tags[0].Tag.TagName);
            Assert.IsFalse(parsedVal.Tags[0].Tag.IsClosing);
            Assert.AreEqual(0, parsedVal.Tags[0].Index);
            Assert.AreEqual("a", parsedVal.Tags[1].Tag.TagName);
            Assert.IsTrue(parsedVal.Tags[1].Tag.IsClosing);
            Assert.AreEqual(4, parsedVal.Tags[1].Index);
            Assert.AreEqual(2, parsedVal.Tags[0].Tag.Attributes.Count);
            Assert.AreEqual("link", parsedVal.Tags[0].Tag.Attributes["href"]);
            Assert.AreEqual("_blank", parsedVal.Tags[0].Tag.Attributes["target"]);
        }

        [TestMethod]
        public void Test_HtmlTaggedString_NestedTags()
        {
            string inputHtml = "<div>This is a <a href=\"link\">test.</a></div>";
            HtmlTaggedString parsedVal = HtmlTaggedString.Parse(inputHtml);
            Assert.IsNotNull(parsedVal);
            Assert.AreEqual("This is a test.", parsedVal.Text);
            Assert.AreEqual(4, parsedVal.Tags.Count);

            Assert.AreEqual("div", parsedVal.Tags[0].Tag.TagName);
            Assert.AreEqual("a", parsedVal.Tags[1].Tag.TagName);
            Assert.AreEqual("a", parsedVal.Tags[2].Tag.TagName);
            Assert.AreEqual("div", parsedVal.Tags[3].Tag.TagName);

            Assert.IsFalse(parsedVal.Tags[0].Tag.IsClosing);
            Assert.IsFalse(parsedVal.Tags[1].Tag.IsClosing);
            Assert.IsTrue(parsedVal.Tags[2].Tag.IsClosing);
            Assert.IsTrue(parsedVal.Tags[3].Tag.IsClosing);

            Assert.AreEqual(0, parsedVal.Tags[0].Index);
            Assert.AreEqual(10, parsedVal.Tags[1].Index);
            Assert.AreEqual(15, parsedVal.Tags[2].Index);
            Assert.AreEqual(15, parsedVal.Tags[3].Index);
        }

        [TestMethod]
        public void Test_HtmlTaggedString_ScriptureVerse()
        {
            string inputHtml = "<span class=\"verse-number\">4 </span>That thine <a class=\"study-note-ref\" href=\"/study/scriptures/nt/matt/6?lang=eng#note4a\" data-scroll-id=\"note4a\"><sup class=\"marker\" data-value=\"a\"></sup>alms</a> may be in secret: and thy Father which seeth in secret himself shall <a class=\"study-note-ref\" href=\"/study/scriptures/nt/matt/6?lang=eng#note4b\" data-scroll-id=\"note4b\"><sup class=\"marker\" data-value=\"b\"></sup>reward</a> thee openly.";
            HtmlTaggedString parsedVal = HtmlTaggedString.Parse(inputHtml);
            Assert.IsNotNull(parsedVal);
            Assert.AreEqual("4 That thine alms may be in secret: and thy Father which seeth in secret himself shall reward thee openly.", parsedVal.Text);
            Assert.AreEqual(10, parsedVal.Tags.Count);
        }
    }
}
