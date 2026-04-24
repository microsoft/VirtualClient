---
applyTo: "**/*Tests/**/*.cs"
description: "Unit test patterns, naming conventions, mock setup, and assertion rules"
---

# Unit Testing Patterns

## Framework and Attributes

- **NUnit 3** with `[TestFixture]`, `[Test]`, `[SetUp]`, `[OneTimeSetUp]`
- **Moq** for mocking interfaces
- **AutoFixture** via `MockFixture` base class
- **Required**: `[Category("Unit")]` or `[Category("Functional")]` — tests without a category
  are silently skipped by CI (`build-test.sh` filters on `Category=Unit`)

## Test Class Structure

- Test classes **must inherit `MockFixture`** (from `VirtualClient.TestFramework`)
- Class name: `{ComponentName}Tests` (e.g., `FioExecutorTests`)
- Method names: Descriptive with underscores (e.g., `FioExecutorSelectsTheExpectedDisks_RemoteDiskScenario`)

```csharp
[TestFixture]
[Category("Unit")]
public class FioExecutorTests : MockFixture
{
    private IDictionary<string, IConvertible> profileParameters;
    private string mockResults;

    [OneTimeSetUp]
    public void SetupFixture()
    {
        this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "FIO", "Results_FIO.json");
    }

    [SetUp]
    public void SetupTest()
    {
        this.Setup(PlatformID.Unix);
        this.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
        {
            return new InMemoryProcess
            {
                OnHasExited = () => true,
                ExitCode = 0,
                StartInfo = new ProcessStartInfo { FileName = command, Arguments = arguments },
                StandardOutput = new ConcurrentBuffer(new StringBuilder(this.mockResults))
            };
        };
    }

    [Test]
    public void FioExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
    {
        // Arrange, Act, Assert
    }
}
```

## MockFixture Provides

- Pre-configured mocks: `ApiClient`, `DiskManager`, `FileSystem`, `File`, `Directory`, `ProcessManager`
- `Setup(PlatformID platform, Architecture arch)` — configure platform-specific behavior
- `MockFixture.ReadFile(...)` — load example output from `Examples/` directories
- Test doubles: `InMemoryProcess`, `InMemoryFile`, `InMemoryDirectory`

## Parser Tests

- Load real example output from `Examples/` directories (not inline strings)
- Run the parser against actual benchmark output
- Assert specific metric names, values, and units:

```csharp
[Test]
public void MyParserParsesMetricsCorrectly()
{
    string output = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "MyWorkload", "results.txt");
    MyWorkloadMetricsParser parser = new MyWorkloadMetricsParser(output);
    IList<Metric> metrics = parser.Parse();

    Assert.IsNotEmpty(metrics);
    MetricAssert.Exists(metrics, "throughput", 12345.67, MetricUnit.OperationsPerSec);
    MetricAssert.Exists(metrics, "latency_p99", 1.23, MetricUnit.Milliseconds);
}
```

## Process Mocking

Use `InMemoryProcess` via `ProcessManager.OnCreateProcess`:
- Set `ExitCode`, `OnHasExited`, `StandardOutput`, `StandardError`
- `StandardOutput` uses `ConcurrentBuffer(new StringBuilder(content))`
