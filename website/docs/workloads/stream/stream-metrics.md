# STREAM Workload Metrics

The following document illustrates the type of results that are emitted by the STREAM workload and captured by the
Virtual Client for net impact analysis.

### System Metrics
Different metrics are captured from the system depending upon which monitor profiles are used. If a monitor profile is not
defined, the default MONITORS-DEFAULT.json profile is used. See the following documentation to determine monitor profiles
that are available.

* [Monitor Profiles](https://github.com/microsoft/VirtualClient/blob/main/website/docs/monitors/monitor-profiles.md)
* [Monitor Profiles (internal only)](../../monitors/monitor-profiles.md)

### Workload-Specific Metrics

The following metrics are emitted by the STREAM workload itself.

| Metric Name            | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|------------------------|---------------------|---------------------|---------------------|------|
| Best Rate Add | 8635.5 | 327893.5 | 42849.75067544962 | MBps |
| Best Rate Copy | 6787.4 | 346279.0 | 30720.395126646046 | MBps |
| Best Rate Scale | 6747.1 | 320023.2 | 30578.698884020574 | MBps |
| Best Rate Triad | 10141.2 | 305781.6 | 42735.667183905876 | MBps |

The following metrics are emitted by the MSFT STREAM 

| MetricName        | Example Value(max) | Example Value (avg) | Example Value (min) | MetricUnit |
|-------------------|--------------------|---------------------|---------------------|------------|
| Min Rate Read     | 50057              | 49066.33333         | 47600               | MBps       |
| Min Rate Copy     | 72302              | 71153.5             | 70192               | MBps       |
| Avg Rate Read     | 50433              | 49814.33333         | 47981               | MBps       |
| Min Rate Triad    | 55407              | 54307.66667         | 53180               | MBps       |
| Min Rate Scale    | 72308              | 71063.83333         | 70110               | MBps       |
| Avg Rate Scale    | 73315              | 72425.5             | 71056               | MBps       |
| Best Rate Copy    | 74208              | 73171.83333         | 72073               | MBps       |
| Min Rate Add      | 53658              | 52981.16667         | 52199               | MBps       |
| Avg Rate Add      | 54074              | 53553.16667         | 52848               | MBps       |
| Best Rate Scale   | 74990              | 73716               | 72486               | MBps       |
| Best Rate Write   | 133326             | 116689.6667         | 87466               | MBps       |
| Best Rate Add     | 54544              | 54011.5             | 53351               | MBps       |
| Avg Rate Triad    | 55720              | 55032.5             | 53849               | MBps       |
| Min Rate Write    | 118017             | 96797.83333         | 84744               | MBps       |
| Avg Rate Write    | 124882             | 105829              | 86252               | MBps       |
| Best Rate Triad   | 56567              | 55725.5             | 54780               | MBps       |
| Avg Rate Copy     | 73323              | 72106.66667         | 71016               | MBps       |
| Best Rate Read    | 51087              | 50461               | 48497               | MBps       |
| Min Latency Scale | 159                | 145.5               | 137                 | ns         |
| Avg Latency Write | 377                | 261.1666667         | 196                 | ns         |
| Min Latency Read  | 135                | 128                 | 121                 | ns         |
| Min Latency Add   | 145                | 134.5               | 126                 | ns         |
| Max Latency Read  | 181                | 168.5               | 156                 | ns         |
| Avg Latency Triad | 163                | 151.6666667         | 141                 | ns         |
| Max Latency Add   | 189                | 172.6666667         | 158                 | ns         |
| Min Latency Copy  | 169                | 151.6666667         | 143                 | ns         |
| Avg Latency Read  | 152                | 147.5               | 144                 | ns         |
| Avg Latency Copy  | 183                | 164.8333333         | 155                 | ns         |
| Max Latency Copy  | 203                | 183.8333333         | 173                 | ns         |
| Avg Latency Add   | 161                | 151.1666667         | 144                 | ns         |
| Max Latency Write | 446                | 299.1666667         | 220                 | ns         |
| Min Latency Triad | 151                | 135.1666667         | 128                 | ns         |
| Min Latency Write | 319                | 224                 | 175                 | ns         |
| Max Latency Scale | 205                | 178.6666667         | 169                 | ns         |
| Max Latency Triad | 193                | 171                 | 157                 | ns         |
| Avg Latency Scale | 179                | 161.8333333         | 155                 | ns         |


Msft Stream output explained:

Function    Best Rate MB/s
Read:      18095    17922    17677    110    110    111
Copy:      28378    28330    28302    113    107    115
Scale:     28363    27853    27129    114    112    116
Add:       19826    19788    19700    113    112    113
Triad:     20457    20197    19829    113    112    113
Write:     47899    47881    47872    117    113    120

Column 1 : Best Rate
Column 2 : Avg Rate
Column 3 : Min Rate
Column 4 : Avg Latency
Column 5 : Min Latency
Column 6 : Max Latency