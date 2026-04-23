---
applyTo: "**/*.md"
description: "Documentation formatting and style guidelines for VirtualClient"
---

# Documentation Guidelines

## Website Documentation (`website/docs/`)

The documentation site uses **Docusaurus** (`website/docusaurus.config.js`). Workload docs live
under `website/docs/workloads/{workload-name}/`.

### Workload Documentation Structure

Every new workload should have a matching doc page. Follow the existing pattern (e.g., `openssl/openssl.md`):

1. **Title** — the workload/tool name as an H1
2. **Overview** — what the tool does, with links to upstream project/docs
3. **What is Being Measured?** — metrics produced and what they represent
4. **Workload Metrics** — table of metric names, units, and relativity
5. **Supported Platforms** — which `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64` are supported
6. **Profile Parameters** — table of all profile parameters with types, defaults, descriptions

### Metric Tables

When documenting metrics, use consistent columns:

| Metric Name | Unit | Relativity | Description |
|---|---|---|---|
| `throughput` | `operations/sec` | HigherIsBetter | Operations per second |

Use the exact `MetricUnit.*` constant string values (e.g., `kilobytes/sec`, `milliseconds`).

## Code Examples in Docs

- Code examples must compile and match actual codebase patterns
- Use fenced blocks with language identifiers (` ```csharp `, ` ```json `, ` ```bash `)
- Reference the source file when citing existing patterns

## Profile Documentation

- Show the complete JSON profile structure with all parameters
- Include descriptions of parameters and their expected types/values
- Note `SupportedPlatforms` and `RecommendedMinimumExecutionTime` from profile metadata
