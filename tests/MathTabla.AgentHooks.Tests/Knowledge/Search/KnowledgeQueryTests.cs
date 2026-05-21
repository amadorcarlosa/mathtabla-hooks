using MathTabla.AgentHooks.Knowledge.Search;

namespace MathTabla.AgentHooks.Tests.Knowledge.Search;

public sealed class KnowledgeQueryTests
{
    [Test]
    public async Task Tokenize_WhenQueryContainsRepeatedTerms_ReturnsDistinctLowercaseTerms()
    {
        var terms = KnowledgeQuery.Tokenize("Mobile drag-and-drop mobile FractionFrameDropZone");

        await Assert.That(terms).IsEquivalentTo(["mobile", "drag-and-drop", "fractionframedropzone"]);
    }
}
