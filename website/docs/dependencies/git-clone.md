---
id: git-clone
---

# Git Clone
Virtual Client git clones a public repository into VC `packages` directory.

:::info
Some Windows versions don't have `git` installed. You need to install `git` first via `chocolatey` first.
Checkout [`PERF-ASPNETBENCH.json`](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-ASPNETBENCH.json) as an exmaple.
:::

## Parameters
| **Parameter** | **Required** | **Description**                                                                                                 |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------|
| PackageName   | Yes          | Directory name to clone into, also the name to be reference by other components.                                |
| RepoUri       | Yes          | Git Repository Uri.                                                                                             |
| Scenario      | No           | Name for telemetry purpose. Does not change functionality.                                                      |


## Examples
In this example, VC clones https://github.com/eembc/coremark.git into `./packages/coremark` directory
```json
{
    "Type": "GitRepoClone",
    "Parameters": {
        "RepoUri": "https://github.com/eembc/coremark.git",
        "PackageName": "coremark"
    }
}
```


### Supported runtimes
win-x64, win-arm64, linux-x64, linux-arm64