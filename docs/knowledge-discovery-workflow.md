# Knowledge Discovery Workflow

## Goal

`kb-discover` gives any agent a deterministic way to retrieve curated MathTabla knowledge context before proposing implementation work.

The command reads existing knowledge indexes and emits JSON grouped by purpose:

- `research`: external references under `/knowledge/sources/`
- `implementation`: codebase documentation under `/knowledge/docs/` or docs-style display paths
- `related`: curated or nearby notes that may add context
- `recommendedNextSearches`: tags and keywords worth searching next

## Command

```powershell
dotnet run --project src\MathTabla.AgentHooks -- kb-discover `
  --root C:\Users\amado\code\MathTabla `
  --query "mobile drag and drop" `
  --depth 2 `
  --max-results 10
```

`--root` may point either to a repository containing `static/knowledge/` or directly to a knowledge folder containing `metadata.json`.

## Current implementation

The first version implements:

- metadata loading from `metadata.json`
- relationship loading from `graph.json`
- query tokenization
- tag, keyword, title, and searchable metadata scoring
- source classification into `sources`, `migrated`, and `curated`
- bidirectional graph traversal from research hits
- JSON output using camelCase property names

## Search model

The search model is intentionally simple and explainable:

- tag match: high signal
- keyword match: strong signal
- title match: medium signal
- general metadata text match: low signal
- graph neighbor: context boost

This mirrors the current knowledge-base search constraints: tags and extracted keywords are the most important search surface, while paragraph body text is not searched directly.

## Agent usage

An agent should call `kb-discover` before giving implementation advice when the user asks to improve or research a MathTabla feature.

The agent should then synthesize:

```text
Research says X.
Current implementation does Y.
The gap is Z.
Recommended implementation steps are A, B, C.
```

The C# command should not perform that synthesis. Keeping synthesis in the model layer preserves the project boundary: deterministic retrieval in C#, judgment and writing in the agent.
