# Script to analyze test execution times from TRX files
# This script will parse all TRX files and create charts for test execution times

param(
    [string]$TrxPath = ".\src\VirtualClient\**\TestResults\*.trx",
    [int]$SlowTestThreshold = 5
)

Write-Host "Analyzing test execution times..." -ForegroundColor Cyan

# Find all TRX files
$trxFiles = Get-ChildItem -Path $TrxPath -Recurse -ErrorAction SilentlyContinue

if ($trxFiles.Count -eq 0) {
    Write-Host "No TRX files found at path: $TrxPath" -ForegroundColor Yellow
    Write-Host "Please run tests with --logger trx first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found $($trxFiles.Count) TRX file(s)" -ForegroundColor Green

# Parse all test results
$allTests = @()

foreach ($trxFile in $trxFiles) {
    Write-Host "Processing: $($trxFile.FullName)" -ForegroundColor Gray
    
    try {
        [xml]$trx = Get-Content $trxFile.FullName
        $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
        $ns.AddNamespace("ns", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
        
        $testResults = $trx.SelectNodes("//ns:UnitTestResult", $ns)
        
        foreach ($result in $testResults) {
            $duration = $result.duration
            if ($duration) {
                # Parse duration (format: HH:MM:SS.mmmmmmm)
                $timeSpan = [TimeSpan]::Parse($duration)
                $seconds = $timeSpan.TotalSeconds
                
                $allTests += [PSCustomObject]@{
                    TestName = $result.testName
                    Duration = $seconds
                    Outcome = $result.outcome
                    TestFile = $trxFile.Name
                }
            }
        }
    }
    catch {
        Write-Host "Error processing $($trxFile.Name): $_" -ForegroundColor Red
    }
}

if ($allTests.Count -eq 0) {
    Write-Host "No test results found in TRX files" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nTotal tests analyzed: $($allTests.Count)" -ForegroundColor Green

# Sort tests by duration
$sortedTests = $allTests | Sort-Object -Property Duration -Descending

# Identify slow tests (> threshold seconds)
$slowTests = $sortedTests | Where-Object { $_.Duration -gt $SlowTestThreshold }

# Generate summary statistics
$stats = $allTests | Measure-Object -Property Duration -Average -Maximum -Minimum -Sum

Write-Host "`n========== TEST EXECUTION TIME STATISTICS ==========" -ForegroundColor Cyan
Write-Host "Total Execution Time: $([Math]::Round($stats.Sum, 2)) seconds" -ForegroundColor White
Write-Host "Average Test Time: $([Math]::Round($stats.Average, 3)) seconds" -ForegroundColor White
Write-Host "Fastest Test: $([Math]::Round($stats.Minimum, 3)) seconds" -ForegroundColor Green
Write-Host "Slowest Test: $([Math]::Round($stats.Maximum, 3)) seconds" -ForegroundColor Red
Write-Host "Tests > ${SlowTestThreshold}s: $($slowTests.Count)" -ForegroundColor Yellow

# Display top 20 slowest tests
Write-Host "`n========== TOP 20 SLOWEST TESTS ==========" -ForegroundColor Cyan
$sortedTests | Select-Object -First 20 | ForEach-Object {
    $color = if ($_.Duration -gt $SlowTestThreshold) { "Red" } else { "Yellow" }
    Write-Host "$([Math]::Round($_.Duration, 3))s - $($_.TestName)" -ForegroundColor $color
}

# Display tests slower than threshold
if ($slowTests.Count -gt 0) {
    Write-Host "`n========== TESTS TAKING LONGER THAN ${SlowTestThreshold} SECONDS ==========" -ForegroundColor Red
    $slowTests | ForEach-Object {
        Write-Host "$([Math]::Round($_.Duration, 3))s - $($_.TestName)" -ForegroundColor Red
    }
    
    # Export slow tests to CSV
    $slowTestsCsv = "slow-tests-over-${SlowTestThreshold}s.csv"
    $slowTests | Select-Object TestName, Duration, Outcome, TestFile | 
        Export-Csv -Path $slowTestsCsv -NoTypeInformation
    Write-Host "`nSlow tests exported to: $slowTestsCsv" -ForegroundColor Green
}

# Export all tests to CSV
$allTestsCsv = "all-test-times.csv"
$sortedTests | Select-Object TestName, Duration, Outcome, TestFile | 
    Export-Csv -Path $allTestsCsv -NoTypeInformation
Write-Host "All test times exported to: $allTestsCsv" -ForegroundColor Green

# Create a simple text-based chart for duration distribution
Write-Host "`n========== TEST DURATION DISTRIBUTION ==========" -ForegroundColor Cyan

$buckets = @{
    "0-0.1s" = 0
    "0.1-0.5s" = 0
    "0.5-1s" = 0
    "1-2s" = 0
    "2-5s" = 0
    "5-10s" = 0
    "10s+" = 0
}

foreach ($test in $allTests) {
    $dur = $test.Duration
    if ($dur -le 0.1) { $buckets["0-0.1s"]++ }
    elseif ($dur -le 0.5) { $buckets["0.1-0.5s"]++ }
    elseif ($dur -le 1) { $buckets["0.5-1s"]++ }
    elseif ($dur -le 2) { $buckets["1-2s"]++ }
    elseif ($dur -le 5) { $buckets["2-5s"]++ }
    elseif ($dur -le 10) { $buckets["5-10s"]++ }
    else { $buckets["10s+"]++ }
}

foreach ($bucket in $buckets.GetEnumerator() | Sort-Object Name) {
    $barLength = [Math]::Min(50, [Math]::Floor($bucket.Value / $allTests.Count * 100))
    $bar = "█" * $barLength
    $percentage = [Math]::Round($bucket.Value / $allTests.Count * 100, 1)
    Write-Host ("{0,-10} {1,5} tests ({2,5}%) {3}" -f $bucket.Key, $bucket.Value, $percentage, $bar)
}

Write-Host "`n========== ANALYSIS COMPLETE ==========" -ForegroundColor Green
