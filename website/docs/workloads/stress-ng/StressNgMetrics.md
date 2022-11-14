# StressNg Workload Metrics
The following document illustrates the type of results that are emitted by the StressNg workload and captured by the
Virtual Client for net impact analysis.

  

### Workload-Specific Metrics
The following metrics are captured during the operations of the StressNg workload.

#### Metrics
[Bogo Ops Explanation](https://wiki.ubuntu.com/Kernel/Reference/stress-ng#:~:text=Bogo%20Ops)

| Name                                 | Unit           | Description                                             |
|--------------------------------------|----------------|---------------------------------------------------------|
| cpu-bogo-ops                         | BogoOps        | Bogus operations in total, with CPU stressor           |
| cpu-bogo-ops-per-second-usr-sys-time | BogoOps/s      | Bogus operations per second on user and system, with CPU stressor           |
| cpu-bogo-ops-per-second-real-time    | BogoOps/s      | Bogus operations per second on real time, with CPU stressor                 |
| cpu-wall-clock-time                  | second         | Total time passed in real world, with CPU stressor           |
| cpu-user-time                        | second         | Total time passed in user mode on every CPU, with CPU stressor           |
| cpu-system-time                      | second         | Total time passed in system mode, with CPU stressor           |

