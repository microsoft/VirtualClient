---
applyTo: "**/*.cs"
description: "C# coding standards and conventions for VirtualClient"
---

# C# Coding Standards

## File Header

Every `.cs` file must start with:
```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
```

## Namespace and Using Style

- `using` statements go **inside** the namespace block (not at file top) — enforced by StyleCop SA1200
- Ordering: `System.*` → `Microsoft.*` → `Newtonsoft.*` → `VirtualClient.*`
- Namespace matches folder structure: `VirtualClient.Actions`, `VirtualClient.Contracts`, etc.

```csharp
namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
}
```

## Naming Conventions

- **Classes**: PascalCase, suffixed by role (`OpenSslExecutor`, `DiskSpdMetricsParser`)
- **Properties**: PascalCase (`CommandLine`, `MetricScenario`)
- **Private fields**: camelCase, no prefix (`private IFileSystem fileSystem;` — not `_fileSystem`)
- **Member access**: Always use `this.` prefix (`this.fileSystem`, `this.Parameters`, `this.Logger`)
- **Constants**: PascalCase (`private const string CoreMarkOutputFile1 = "run1.log";`)
- **Parameters keys**: PascalCase, accessed case-insensitively via `StringComparer.OrdinalIgnoreCase`
- **Async methods**: Suffix with `Async` (`ExecuteAsync`, `InitializeAsync`, `CleanupAsync`)

## Profile Parameter Properties

Properties reading from the `Parameters` dictionary use this pattern:

```csharp
public string CommandArguments
{
    get { return this.Parameters.GetValue<string>(nameof(this.CommandArguments)); }
}

// With default value:
public string CompilerName
{
    get { return this.Parameters.GetValue<string>(nameof(this.CompilerName), string.Empty); }
}
```

## XML Documentation

All public members require XML doc comments with `<summary>`, `<param>`, `<returns>` tags:

```csharp
/// <summary>
/// Constructor
/// </summary>
/// <param name="dependencies">Provides required dependencies to the component.</param>
/// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
```

## Code Quality Rules

- **StyleCop.Analyzers** enforces style (suppressed: SA1204 static element ordering)
- **AsyncFixer** validates async patterns (suppressed: AZCA1002 async method naming)
- NuGet versions must be in `Directory.Packages.props`, never in individual `.csproj` files

## Exception Handling

Use the project's exception hierarchy — never throw raw `Exception` or `InvalidOperationException`:

- `WorkloadException` — workload failures, validation errors (with `ErrorReason.InvalidProfileDefinition`)
- `DependencyException` — dependency resolution failures
- `ProcessException` — process execution failures
- `MonitorException` — monitor failures
- `WorkloadResultsException` — parsing failures
