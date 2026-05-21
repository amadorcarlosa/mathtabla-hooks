using MathTabla.AgentHooks.Knowledge.Domain;
using MathTabla.AgentHooks.Knowledge.Search;

namespace MathTabla.AgentHooks.Tests.Knowledge.Search;

public sealed class KnowledgeSourceClassifierTests
{
    [Test]
    [Arguments("/knowledge/sources/blogs/ffitts.html", "", KnowledgeSourceKinds.Sources)]
    [Arguments("/knowledge/docs/frontend/drop-zone.html", "", KnowledgeSourceKinds.Migrated)]
    [Arguments("", "frontend/components/drop-zone.html", KnowledgeSourceKinds.Migrated)]
    [Arguments("/knowledge/plans/mobile-dnd.html", "", KnowledgeSourceKinds.Curated)]
    public async Task Classify_ReturnsSourceKind(string path, string displayPath, string expected)
    {
        var note = new KnowledgeNoteMetadata
        {
            Path = path,
            DisplayPath = displayPath
        };

        var sourceKind = KnowledgeSourceClassifier.Classify(note);

        await Assert.That(sourceKind).IsEqualTo(expected);
    }
}
