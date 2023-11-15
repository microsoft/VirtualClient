# Wrathmark

Wrathmark is an Othello program written by Rico Mariani for a project at Waterloo computer science course in 1987 to run on a VAX 780.

* [Wrath-Othello GitHub](https://github.com/ricomariani/wrath-othello/tree/main)
* [Wrathmark: An Interesting Compute Workload (Part 1)](https://ricomariani.medium.com/wrathmark-an-interesting-compute-workload-part-1-47d61e0bea43)
* [Wrathmark: An Interesting Compute Workload (Part 2)](https://ricomariani.medium.com/wrathmark-an-interesting-compute-workload-part-2-bac27c7f0c7d)

## Packaging and Setup

Virtual client

1. Installs the DotNet SDK specified in the profile
2. Clones the `wrath-othello` GitHub repository from the manifest
3. `dotnet publish` the subdirectory specified in the profile (default `wrath-sharp-std-arrays`)
4. Run the resulting wrath mark program loading the [`endgame.txt`](https://github.com/ricomariani/wrath-othello/blob/main/endgame.txt) game state

```
wrath-sharp-std-arrays.exe xlt ..\endgame.txt
```