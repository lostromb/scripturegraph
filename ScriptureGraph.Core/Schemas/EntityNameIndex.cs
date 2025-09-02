using Durandal.Common.Collections;
using ScriptureGraph.Core.Graph;

namespace ScriptureGraph.Core.Schemas
{
    public class EntityNameIndex
    {
        public IDictionary<KnowledgeGraphNodeId, string> Mapping { get; }

        public EntityNameIndex()
        {
            Mapping = new FastConcurrentDictionary<KnowledgeGraphNodeId, string>();
        }

        public void Serialize(Stream outStream)
        {
            using (BinaryWriter writer = new BinaryWriter(outStream))
            {
                writer.Write(Mapping.Count);
                foreach (var kvp in Mapping)
                {
                    writer.Write((ushort)kvp.Key.Type);
                    writer.Write(kvp.Key.Name);
                    writer.Write(kvp.Value);
                }
            }
        }

        public static EntityNameIndex Deserialize(Stream inStream)
        {
            EntityNameIndex returnVal = new EntityNameIndex();
            using (BinaryReader reader = new BinaryReader(inStream))
            {
                int mappingCount = reader.ReadInt32();
                for (int c = 0; c < mappingCount; c++)
                {
                    KnowledgeGraphNodeType nodeType = (KnowledgeGraphNodeType)reader.ReadUInt16();
                    string nodeName = reader.ReadString();
                    string value = reader.ReadString();
                    returnVal.Mapping.Add(new KnowledgeGraphNodeId(nodeType, nodeName), value);
                }
            }

            return returnVal;
        }
    }
}
