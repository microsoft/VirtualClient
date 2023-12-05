# Wrathmark

Wrathmark is an Othello program written by Rico Mariani for a project at Waterloo computer science course in 1987 to run on a VAX 780.

* [Wrath-Othello GitHub](https://github.com/ricomariani/wrath-othello/tree/main)
* [Wrathmark: An Interesting Compute Workload (Part 1)](https://ricomariani.medium.com/wrathmark-an-interesting-compute-workload-part-1-47d61e0bea43)
* [Wrathmark: An Interesting Compute Workload (Part 2)](https://ricomariani.medium.com/wrathmark-an-interesting-compute-workload-part-2-bac27c7f0c7d)

## What is Being Measured

Wrathmark plays the game Othello, evaluating boards for an optimal move. The entire workload is just processing of in-memory data. In short, it's a good test of basic operations without being an arbitrary microbenchmark. It actually plays the game, so there is meaningfulness to its calculations. The benchmark result is completely deterministic, it evaluates exactly 1,474,559,545 boards every time. The same boards. And it allocates nothing except some fixed size stuff at startup, so there is zero GC overhead. The benchmark in managed code will allocate exactly one timer object dynamically during the run. It’s quite fast so it can do the board evaluation in a few minutes. 

There are different folders in the repository with varying methods of data storage and evaluation. The Virtual Client uses the folder `wrath-sharp-std-arrays`, which uses `byte[]` as the backing store for the game boards. The workload is deterministic loading state from `endgame.txt` found in the root of the repository.

```
- - B B - W - - 
B - B B B B - B 
B B W W W W B B 
B W B W W B W B 
- W W B B W W - 
W W W W W W W W 
- - - W B B - - 
- - W - B B - - 

b to play

This is an interesting endgame situation useful for benchmarking
```

## Workload Metrics

A single metric is emitted by the benchmark captured by Virtual Client when running the Wrathmark workload.

### Example Metrics

| Name            | Example  |
|-----------------|----------|
| BoardsPerSecond | 10920640 | 

The boards per second is calcualted from the timer's elapsed milliseconds and divided against the number of boards evaluated:

`BoardsPerSecond = boards / duration`
[Code](https://github.com/ricomariani/wrath-othello/blob/de91c7ea4d2480101dfb1534861d762dde7fb6e3/wrath-sharp-std-arrays/WrathStdArrays.cs#L1138C1-L1146C25)

## Packaging and Setup

Virtual client

1. Installs the DotNet SDK specified in the profile
2. Clones the `wrath-othello` GitHub repository from the manifest
3. `dotnet publish` the subdirectory specified in the profile (default `wrath-sharp-std-arrays`)
4. Run the resulting wrath mark program loading the [`endgame.txt`](https://github.com/ricomariani/wrath-othello/blob/main/endgame.txt) game state

```
wrath-sharp-std-arrays.exe xlt ..\endgame.txt
```
