using MathTabla.AgentHooks.Knowledge.Domain;
using MathTabla.AgentHooks.Knowledge.Search;

namespace MathTabla.AgentHooks.Tests.Knowledge.Search;

public sealed class KnowledgeSearchEngineTests
{
    [Test]
    public async Task Discover_WhenResearchMatchesQuery_ReturnsResearchAndConnectedImplementation()
    {
        var index = new KnowledgeIndex(
            [
                new KnowledgeNoteMetadata
                {
                    Path = "/knowledge/sources/blogs/bubble-cursor.html",
                    Title = "Bubble Cursor",
                    Type = "reference",
                    Subtype = "research-paper",
                    Status = "active",
                    Tags = ["drag-and-drop", "touch-input", "target-acquisition"],
                    Keywords = ["Voronoi activation", "FractionFrameDropZone"],
                    DisplayPath = "sources/blogs/bubble-cursor.html"
                },
                new KnowledgeNoteMetadata
                {
                    Path = "/knowledge/docs/frontend/FractionFrameDropZone.html",
                    Title = "FractionFrameDropZone",
                    Type = "implementation-note",
                    Status = "active",
                    Tags = ["drop-zone", "drag-and-drop"],
                    Keywords = ["FractionFrameDropZone"],
                    DisplayPath = "docs/frontend/FractionFrameDropZone.html"
                },
                new KnowledgeNoteMetadata
                {
                    Path = "/knowledge/decisions/theme.html",
                    Title = "Theme Decision",
                    Type = "decision",
                    Status = "active",
                    Tags = ["theme"],
                    DisplayPath = "decisions/theme.html"
                }
            ],
            [
                new KnowledgeGraphEdge(
                    "/knowledge/sources/blogs/bubble-cursor.html",
                    "/knowledge/docs/frontend/FractionFrameDropZone.html",
                    "relates_to",
                    Resolved: true)
            ]);

        var result = KnowledgeSearchEngine.Discover(index, "mobile drag drop FractionFrameDropZone", graphDepth: 2, maxResults: 10);

        await Assert.That(result.QueryTerms).Contains("mobile");
        await Assert.That(result.Research).Count().IsEqualTo(1);
        await Assert.That(result.Research[0].Path).IsEqualTo("/knowledge/sources/blogs/bubble-cursor.html");
        await Assert.That(result.Implementation).Count().IsEqualTo(1);
        await Assert.That(result.Implementation[0].Path).IsEqualTo("/knowledge/docs/frontend/FractionFrameDropZone.html");
        await Assert.That(result.Implementation[0].GraphDistance).IsEqualTo(1);
    }

    [Test]
    public async Task Discover_WhenMaxResultsIsOne_LimitsEachResultGroup()
    {
        var index = new KnowledgeIndex(
            [
                CreateResearchNote("/knowledge/sources/a.html", "A", "drag-and-drop"),
                CreateResearchNote("/knowledge/sources/b.html", "B", "drag-and-drop")
            ],
            []);

        var result = KnowledgeSearchEngine.Discover(index, "drag-and-drop", graphDepth: 0, maxResults: 1);

        await Assert.That(result.Research).Count().IsEqualTo(1);
    }

    private static KnowledgeNoteMetadata CreateResearchNote(string path, string title, params string[] tags) =>
        new()
        {
            Path = path,
            Title = title,
            Type = "reference",
            Status = "active",
            Tags = tags,
            DisplayPath = path.Replace("/knowledge/", "")
        };
}
