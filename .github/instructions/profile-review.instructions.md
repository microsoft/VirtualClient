---
applyTo: "**/*.json"
description: "Execution profile review rules for VirtualClient JSON profiles"
---

# Profile Review Guidelines

## Required Structure

Every profile must contain:
- `"Description"` — human-readable description of the workload
- `"Metadata"` — with `SupportedPlatforms`, `SupportedOperatingSystems`, `RecommendedMinimumExecutionTime`
- `"Parameters"` — global parameters referenced by actions/dependencies
- `"Actions"` — array of workload executors to run
- `"Dependencies"` — array of prerequisite installers

## Action Definition

Each action must have:
- `"Type"` — exact C# class name (e.g., `"OpenSslExecutor"`, `"FioExecutor"`)
- `"Parameters"` with at minimum:
  - `"Scenario"` — unique identifier for this action step
  - `"PackageName"` — name of the dependency package (if applicable)

## Parameter Referencing

- Global parameters referenced via JSONPath: `"Duration": "$.Parameters.Duration"`
- Expression placeholders in commands: `"speed -seconds {Duration.TotalSeconds} md5"`
- Calculated expressions: `"{calculate({LogicalCoreCount}/2)}"`
- Conditional expressions: `"{calculate(\"{Platform}\".StartsWith(\"linux\") ? \"libaio\" : \"windowsaio\")}"`

## Metadata Fields

```json
"Metadata": {
    "RecommendedMinimumExecutionTime": "01:00:00",
    "SupportedPlatforms": "linux-x64,linux-arm64,win-x64",
    "SupportedOperatingSystems": "AzureLinux,CentOS,Debian,RedHat,Suse,Ubuntu,Windows"
}
```

## Client/Server Profiles

For multi-VM workloads, actions specify roles:

```json
{ "Type": "ServerExecutor", "Parameters": { "Scenario": "Start", "Role": "Server", "Port": 6379 } },
{ "Type": "ClientExecutor", "Parameters": { "Scenario": "Bench", "Role": "Client", "ServerPort": "$.Parameters.ServerPort" } }
```

## Dependencies

- Must run before actions; define package installations, compiler setup, disk initialization
- Common types: `DependencyPackageInstallation`, `LinuxPackageInstallation`, `CompilerInstallation`
- Use `"Extract": true` for ZIP packages, `"BlobContainer": "packages"` for blob storage

## Review Checklist

- [ ] `"Type"` values match actual C# class names
- [ ] All `$.Parameters.*` references resolve to defined global parameters
- [ ] `{Placeholder}` expressions reference valid properties
- [ ] `SupportedPlatforms` lists only platforms the executor actually supports
- [ ] `Scenario` values are unique within the profile
- [ ] Dependencies are ordered correctly (base packages before workload packages)
