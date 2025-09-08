using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ScriptureGraph.App.Schemas
{
    internal class ReadingPane
    {
        public required Guid PanelId;
        public required Grid Container;
        public required FlowDocumentScrollViewer DocumentViewer;
        public required TextBlock Header;
        public required string HeaderText;
        public KnowledgeGraphNodeId? CurrentDocumentEntity;
        public GospelDocument? CurrentDocument;
    }
}
