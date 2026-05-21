using MathTabla.AgentHooks.Knowledge.Indexing;

namespace MathTabla.AgentHooks.Tests.Knowledge.Indexing;

public sealed class KnowledgeIndexLoaderTests
{
    [Test]
    public async Task LoadAsync_WhenRootContainsStaticKnowledge_LoadsMetadataAndGraph()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var knowledgeRoot = Path.Combine(root, "static", "knowledge");
            Directory.CreateDirectory(knowledgeRoot);

            await File.WriteAllTextAsync(
                Path.Combine(knowledgeRoot, "metadata.json"),
                """
                [
                  {
                    "path": "/knowledge/sources/a.html",
                    "id": "a",
                    "title": "A",
                    "type": "reference",
                    "status": "active",
                    "tags": ["drag-and-drop"],
                    "keywords": ["FractionFrameDropZone"],
                    "displayPath": "sources/a.html"
                  }
                ]
                """);

            await File.WriteAllTextAsync(
                Path.Combine(knowledgeRoot, "graph.json"),
                """
                {
                  "edges": [
                    {
                      "sourcePath": "/knowledge/sources/a.html",
                      "targetPath": "/knowledge/docs/b.html",
                      "relationshipType": "relates_to",
                      "resolved": true
                    }
                  ]
                }
                """);

            var index = await KnowledgeIndexLoader.LoadAsync(root);

            await Assert.That(index.Notes).Count().IsEqualTo(1);
            await Assert.That(index.Edges).Count().IsEqualTo(1);
            await Assert.That(index.Edges[0].TargetPath).IsEqualTo("/knowledge/docs/b.html");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task LoadAsync_WhenMetadataIsMissing_ThrowsFileNotFound()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            await Assert.That(async () => await KnowledgeIndexLoader.LoadAsync(root))
                .Throws<FileNotFoundException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mathtabla-hooks-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
