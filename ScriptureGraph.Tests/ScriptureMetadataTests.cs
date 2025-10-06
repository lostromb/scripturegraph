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
    public class ScriptureMetadataTests
    {
        [TestMethod]
        public void TestParseScriptureRef()
        {
            ScriptureReference? reference;
            reference = ScriptureMetadataEnglish.TryParseScriptureReference("not a book 2:3");
            Assert.IsNull(reference);
            reference = ScriptureMetadataEnglish.TryParseScriptureReference("stone cold 3:16");
            Assert.IsNull(reference);

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("1 Nephi 3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm 1-ne 3:5", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("1ne3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm 1-ne 3:5", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("1ne 3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm 1-ne 3:5", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("moroni");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm moro", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("Genesis 39");
            Assert.IsNotNull(reference);
            Assert.AreEqual("ot gen 39", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("Gal. 5:19");
            Assert.IsNotNull(reference);
            Assert.AreEqual("nt gal 5:19", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("D&C 110:12");
            Assert.IsNotNull(reference);
            Assert.AreEqual("dc-testament dc 110:12", reference.ToString());

            // currently don't parse ranges
            reference = ScriptureMetadataEnglish.TryParseScriptureReference("Moroni 10:3-5");
            Assert.IsNull(reference);

            // And don't parse invalid values
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne -3:5"));
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne 0:5"));
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne 3:0"));
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne 3:-1"));
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne 3:205"));
            Assert.IsNull(ScriptureMetadataEnglish.TryParseScriptureReference("1ne 670:25"));

            // Catch edge cases for single-chapter books
            reference = ScriptureMetadataEnglish.TryParseScriptureReference("enos 5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm enos 1:5", reference.ToString());

            reference = ScriptureMetadataEnglish.TryParseScriptureReference("enos 1:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm enos 1:5", reference.ToString());
        }

        [TestMethod]
        [DataRow("1 nephi 3:5", false, new string[] { "bofm 1-ne 3:5" })]
        [DataRow("1 nephi 3:5-7", false, new string[] { "bofm 1-ne 3:5", "bofm 1-ne 3:6", "bofm 1-ne 3:7" })]
        [DataRow("P. of G.P., Moses 4:1-4; see also Abraham 3:27, 28.", true,
            new string[] { "pgp moses 4:1", "pgp moses 4:2", "pgp moses 4:3", "pgp moses 4:4", "pgp abr 3:27", "pgp abr 3:28" })]
        public void TestParseAllScriptureRefs(string input, bool includeExtra, string?[] expectedOutputs)
        {
            string?[] actualOutputs = ScriptureMetadataEnglish.ParseAllReferences(input, includeExtra).Select((s) => s == null ? null : s.ToString()).ToArray();
            foreach (string? actual in actualOutputs)
            {
                Debug.WriteLine(actual);
            }

            Assert.AreEqual(expectedOutputs.Length, actualOutputs.Length);
            for (int idx = 0; idx <  expectedOutputs.Length; idx++)
            {
                Assert.AreEqual(expectedOutputs[idx], actualOutputs[idx]);
            }
        }
    }
}
