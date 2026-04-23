---
applyTo: "**/*.cs"
description: "PR review rules: required fixes vs suggestions for C# code changes"
---

# PR Review Guidelines

## Required Fixes (flag these — they break things)

1. **Component must inherit `VirtualClientComponent`.** Profile `"Type"` resolution casts via
   `Activator.CreateInstance` to `VirtualClientComponent`. Wrong base class → `InvalidCastException`.

2. **Constructor must be `(IServiceCollection, IDictionary<string, IConvertible>)`.** `ComponentFactory`
   uses reflection with this exact signature. Wrong constructor → `MissingMethodException`.

3. **Assembly must have `[assembly: VirtualClientComponentAssembly]`.** Without this attribute,
   `ComponentTypeCache` skips the assembly during type discovery → `TypeLoadException`.

4. **Profile `"Type"` must exactly match the C# class name.** `ComponentTypeCache.TryGetComponentType`
   matches on type name. Typo → `TypeLoadException` at profile load.

5. **Parser must extend `MetricsParser` and implement `Parse()`.** `Parse()` is abstract — missing
   override is a compile error. Wrong return type breaks `LogMetrics`.

6. **NuGet versions in `Directory.Packages.props` only.** Adding `Version=` in a `.csproj` causes
   build error `NU1008` due to central package management.

7. **`using` statements inside `namespace` block.** StyleCop SA1200 enforced repo-wide — top-level
   usings fail CI.

8. **Use project exception hierarchy.** Raw `Exception`/`InvalidOperationException` breaks error
   routing on `ErrorReason`. Use `WorkloadException`, `DependencyException`, `ProcessException`, etc.

9. **Tests must have `[TestFixture]` and `[Category("Unit")]`.** Build scripts filter by category.
   Missing category → tests silently never run in CI.

10. **Copyright header on every `.cs` file.** StyleCop SA1633 requires the standard two-line header.

## Suggestions (flag these — won't break but inconsistent)

1. **Use `this.Parameters.GetValue<T>(nameof(...))` for profile parameters** — not direct
   dictionary access.

2. **Add `[SupportedPlatforms("...")]` to executor classes** — omitting means workload attempts
   to run on all platforms.

3. **Test classes should inherit `MockFixture`** — not create mocks from scratch.

4. **Using ordering: `System.*` → `Microsoft.*` → `Newtonsoft.*` → `VirtualClient.*`.**

5. **Private fields: camelCase, no prefix** (`fileSystem` not `_fileSystem`).

6. **XML doc comments on all public members.**

7. **Parser tests should load real output from `Examples/`** — not inline strings.

8. **`Validate()` should throw `WorkloadException` with `ErrorReason.InvalidProfileDefinition`.**

9. **Async methods suffixed with `Async`.**
