# Custom Execution Order Components

Virtual Client supports advanced execution order controls for workload actions and dependencies. This allows the user to define how multiple components are executed: in parallel, sequentially, or in repeated parallel loops. 
This guide summarizes the available execution order components, their parameters, and provides example profile snippets for each.

---

## ParallelExecution

Executes all child components **in parallel**. Each component starts at the same time and runs independently.
The Parallel execution will complete when all components have finished exactly one iteration.

### Example
Here, the two child components , ScenarioA and ScenarioB, will start simultaneously and run in parallel. The execution will complete when both scenarios have finished one run.

```json
{ 
    "Type": "ParallelExecution", 
    "Components": [ 
        { 
            "Type": "TestExecutor", 
            "Parameters": { 
                "Scenario": "ScenarioA" 
            } 
        }, 
        { 
            "Type": "TestExecutor", 
            "Parameters": { 
                "Scenario": "ScenarioB" 
            } 
        } 
    ] 
}
```


---

## SequentialExecution

Executes all child components **one after another** in the order they are listed. Each component starts only after the previous one completes.

### Parameters

| Parameter   | Purpose                                      | Acceptable Range | Default Value |
|-------------|----------------------------------------------|------------------|---------------|
| LoopCount      | Specifies the number of times to repeat the execution of each child component under the sequential execution | Positive integer | 1 |

### Example
Here, the components will be executed sequentially, and each component will run twice due to the `LoopCount` parameter. ScenarioA will run first, followed by ScenarioB, and this sequence will repeat twice.

```json
{ 
    "Type": "SequentialExecution",
    "Parameters": {
        "LoopCount": 2
    },
    "Components": [ 
        { 
            "Type": "TestExecutor1", 
            "Parameters": { 
                "Scenario": "ScenarioA" 
            } 
        }, 
        { 
            "Type": "TestExecutor2", 
            "Parameters": { 
                "Scenario": "ScenarioB" 
            } 
        } 
    ] 
}
```

---

## ParallelLoopExecution

Executes all child components **in parallel**, and **repeats** this execution for a specified duration or minimum number of iterations. Each component runs in its own loop, independently, until the overall duration or the minimum iteration count is reached.

### Parameters

| Parameter         | Purpose                                                      | Acceptable Range         | Default Value |
|-------------------|-------------------------------------------------------------|--------------------------|---------------|
| Duration          | Maximum time to run the parallel loop (hh:mm:ss format)      | > 0                     | -1 (no limit) |
| MinimumIterations  | Minimum number of times each child component should run            | >= 0                    | 0           |

### Example
Here, the child components will start execution parallely, and each component will run at least three times due to the `MinimumIterations` parameter. They will complete after 10 minutes if each component has completed the run three times, or else it will wait for three runs of each component. Thus, the MinimumIterations parameter supersedes the Duration parameter if the latter is not met. 

Note that By default, the MinimumIterations parameter is set to 0, meaning that the components will run until the Duration is met. If you want to ensure that each component runs at least once, you can set MinimumIterations to 1 or more.

```json
{ 
    "Type": "ParallelLoopExecution",
    "Parameters": {
        "Duration": "00:10:00",
        "MinimumIterations": 3
    },
    "Components": [ 
        { 
            "Type": "TestExecutor1", 
            "Parameters": { 
                "Scenario": "ScenarioA" 
            } 
        }, 
        { 
            "Type": "TestExecutor2", 
            "Parameters": { 
                "Scenario": "ScenarioB" 
            } 
        } 
    ] 
}
```

---

## Usage Notes

- For more details on writing and structuring profiles, see [Profile Authoring Guide](./0011-profiles.md).
