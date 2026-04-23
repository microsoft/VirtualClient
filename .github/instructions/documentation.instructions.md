---
applyTo: "**/*.md"
description: "Documentation formatting and style guidelines"
---

# Documentation Guidelines

## Markdown Formatting

- Use ATX-style headers (`#`, `##`, `###`) with a blank line before and after
- Use fenced code blocks with language identifiers (` ```csharp `, ` ```bash `, ` ```json `)
- Use relative links for cross-references within the repository
- Keep lines at a reasonable length for readability

## Code Examples in Documentation

- Code examples must be syntactically correct and representative of actual codebase patterns
- Include the source file reference when citing existing code patterns
- Use comments to explain non-obvious behavior

## Profile Documentation

- When documenting execution profiles, show the complete JSON structure
- Include descriptions of all parameters and their expected types/values
- Note supported platforms and minimum execution times

## API Documentation

- Document all public APIs with clear input/output descriptions
- Include error codes and their meanings from the `ErrorReason` enum
- Provide example request/response payloads where applicable

## README and Getting Started

- Keep setup instructions current with the actual build process
- Test all documented commands before committing
- Include prerequisites (e.g., .NET SDK version from `global.json`: currently 9.0.301)
