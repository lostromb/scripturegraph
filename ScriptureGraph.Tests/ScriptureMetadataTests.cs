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
    public class ScriptureMetadataTests
    {
        [TestMethod]
        public void TestParseScriptureRef()
        {
            ScriptureReference? reference;
            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("not a book 2:3");
            Assert.IsNull(reference);
            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("stone cold 3:16");
            Assert.IsNull(reference);

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("1 Nephi 3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm", reference.Canon);
            Assert.AreEqual("1-ne", reference.Book);
            Assert.AreEqual(3, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(5, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm", reference.Canon);
            Assert.AreEqual("1-ne", reference.Book);
            Assert.AreEqual(3, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(5, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 3:5");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm", reference.Canon);
            Assert.AreEqual("1-ne", reference.Book);
            Assert.AreEqual(3, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(5, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("moroni");
            Assert.IsNotNull(reference);
            Assert.AreEqual("bofm", reference.Canon);
            Assert.AreEqual("moro", reference.Book);
            Assert.AreEqual(-1, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(-1, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("Genesis 39");
            Assert.IsNotNull(reference);
            Assert.AreEqual("ot", reference.Canon);
            Assert.AreEqual("gen", reference.Book);
            Assert.AreEqual(39, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(-1, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("Gal. 5:19");
            Assert.IsNotNull(reference);
            Assert.AreEqual("nt", reference.Canon);
            Assert.AreEqual("gal", reference.Book);
            Assert.AreEqual(5, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(19, reference.Verse.GetValueOrDefault(-1));

            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("D&C 110:12");
            Assert.IsNotNull(reference);
            Assert.AreEqual("dc-testament", reference.Canon);
            Assert.AreEqual("dc", reference.Book);
            Assert.AreEqual(110, reference.Chapter.GetValueOrDefault(-1));
            Assert.AreEqual(12, reference.Verse.GetValueOrDefault(-1));

            // currently don't parse ranges
            reference = ScriptureMetadata.TryParseScriptureReferenceEnglish("Moroni 10:3-5");
            Assert.IsNull(reference);

            // And don't parse invalid values
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne -3:5"));
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 0:5"));
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 3:0"));
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 3:-1"));
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 3:205"));
            Assert.IsNull(ScriptureMetadata.TryParseScriptureReferenceEnglish("1ne 670:25"));
        }
    }
}
