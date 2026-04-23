---
applyTo: "**/*MetricsParser.cs"
description: "Pattern for developing metric parsers with proper units and consistency"
---

# MetricsParser Development Guide

## Class Structure

Parsers inherit from `MetricsParser` → `TextParser<IList<Metric>>`:

```csharp
public class MyWorkloadMetricsParser : MetricsParser
{
    private static readonly Regex ValuePattern = new Regex(
        @"(\d+\.?\d*)\s+(ops/sec)", RegexOptions.Compiled);

    public MyWorkloadMetricsParser(string rawText)
        : base(rawText)
    {
    }

    public override IList<Metric> Parse()
    {
        try
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, "SectionHeader");
            List<Metric> metrics = new List<Metric>();
            // Parse sections and extract metrics
            return metrics;
        }
        catch (Exception exc)
        {
            throw new WorkloadResultsException(
                "Failed to parse results.", exc, ErrorReason.WorkloadResultsParsingFailed);
        }
    }

    protected override void Preprocess()
    {
        // Remove unwanted lines before parsing
        this.PreprocessedText = TextParsingExtensions.RemoveRows(this.RawText, @"^\s*$");
    }
}
```

## Standard Units (from `MetricUnit` constants)

Always use `MetricUnit.*` constants from `VirtualClient.Contracts/MetricUnit.cs` — never raw strings.
Full list includes: `Bytes`, `Kilobytes`, `Megabytes`, `Gigabytes`, `Terabytes`, `Petabytes`,
`BytesPerSecond`, `KilobytesPerSecond`, `MegabytesPerSecond`, `GigabytesPerSecond`,
`KibibytesPerSecond`, `MebibytesPerSecond`, `GibibytesPerSecond`,
`OperationsPerSec`, `RequestsPerSec`, `TransactionsPerSec`,
`Nanoseconds`, `Microseconds`, `Milliseconds`, `Seconds`, `Minutes`,
`Count`, `Operations`, `Watts`, `Amps`, `Celcius`, `Megahertz`, `BytesPerConnection`.

## MetricRelativity

Set relativity correctly on every metric:

- `MetricRelativity.HigherIsBetter` — throughput, bandwidth, operations/sec
- `MetricRelativity.LowerIsBetter` — latency, time, error counts
- `MetricRelativity.Undefined` — informational metrics (default)

```csharp
new Metric("throughput", value, MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter)
new Metric("latency_p99", value, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter)
```

## Regex Patterns

Define as `private static readonly Regex` with `RegexOptions.Compiled`:

```csharp
private static readonly Regex ThroughputRegex = new Regex(
    @"Throughput:\s+(\d+\.?\d*)\s+ops/sec", RegexOptions.Compiled);
```

## Error Handling

Wrap `Parse()` body in try-catch, throwing `WorkloadResultsException`:

```csharp
catch (Exception exc)
{
    throw new WorkloadResultsException(
        "Failed to parse MyWorkload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
}
```

## Text Parsing Utilities

- `TextParsingExtensions.Sectionize(text, pattern)` — split text into named sections
- `TextParsingExtensions.RemoveRows(text, pattern)` — strip unwanted lines in `Preprocess()`
- `this.PreprocessedText` — normalized text for parsing (set in `Preprocess()`)
- `this.Sections` — dictionary of parsed sections (set in `Parse()`)
