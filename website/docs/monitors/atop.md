# Atop
Atop is a toolset that enables support for capturing performance information on Unix/Linux systems. This information is used in the Virtual Client to
to formulate a performance counter base.

* [Atop Documentation](https://manpages.debian.org/testing/atop/atop.1.en.html)
* [Counters Captured](./perf-counter-metrics.md)

### Supported Platforms
* linux-x64
* linux-arm64

### Atop Output Description
The following section describes the various counters/metrics that are available with the Atop toolset. Note that for consistency with other performance
counter toolsets used by the Virtual Client across Windows and Unix/Linux systems, these performance metrics are mapped to performance counter names 
that are similar in format to those captured on Windows systems. See the link at the top for more details on the exact performance counters that are captured.

https://manpages.debian.org/testing/atop/atop.1.en.html#OUTPUT_DESCRIPTION_-_SYSTEM_LEVEL

| Metric Name | Description |
|-------------|-------------|
| PRC-sys | Total cpu time consumed in system mode ('sys') |
| PRC-user | Total cpu time consumed in user mode ('user') |
| PRC-#proc | The total number of processes present at this moment ('#proc') |
| PRC-#trun | The total number of threads present at this moment in state 'running' ('#trun') |
| PRC-#tslpi | 'sleeping interruptible' ('#tslpi') |
| PRC-#tslpu | 'sleeping uninterruptible' ('#tslpu') |
| PRC-#zombie | The number of zombie processes ('#zombie') |
| PRC-clones | The number of clone system calls ('clones') |
| PRC-#zombie | The number of zombie processes ('#zombie') |
| PRC-#exit | The number of processes that ended during the interval ('#exit') when process accounting is used. Instead of '#exit' the last column may indicate that process accounting could not be activated ('no procacct').|
| \Processor Information(_Total)\% System Time | The percentage of cpu time spent in kernel mode by all active processes ('sys') |
| \Processor Information(_Total)\% User Time | The percentage of cpu time consumed in user mode ('user') for all active processes (including processes running with a nice value larger than zero) |
| \Processor Information(_Total)\% IRQ Time | The percentage of cpu time spent for interrupt handling ('irq') including softirq |
| \Processor Information(_Total)\% Idle Time | The percentage of unused cpu time while no processes were waiting for disk I/O ('idle') |
| CPU-wait | The percentage of unused cpu time while at least one process was waiting for disk I/O ('wait').In case of per-cpu occupation, the cpu number and the wait percentage ('w') for that cpu. The number of lines showing the per-cpu occupation can be limited. |
| CPU-steal | For virtual machines, the steal-percentage ('steal') shows the percentage of cpu time stolen by other virtual machines running on the same hardware. |
| CPU-guest | For physical machines hosting one or more virtual machines, the guest-percentage ('guest') shows the percentage of cpu time used by the virtual machines. Notice that this percentage overlaps the user percentage! |
| CPU-ipc | When PMC performance monitoring counters are supported by the CPU and the kernel (and atop runs with root privileges), the number of instructions per CPU cycle ('ipc') is shown. The first sample always shows the value 'initial', because the counters are just activated at the moment that atop is started.|
| CPU-cycl | per CPU the effective number of cycles ('cycl') is shown. This value can reach the current CPU frequency if such CPU is 100% busy. When an idle CPU is halted, the number of effective cycles can be (considerably) lower than the current frequency. |
| CPU-avgf | In case that the kernel module 'cpufreq_stats' is active (after issueing 'modprobe cpufreq_stats'), the average frequency ('avgf') is shown. |
| CPU-avgscal | In case that the kernel module 'cpufreq_stats' is active (after issueing 'modprobe cpufreq_stats'), the average scaling percentage ('avgscal') is shown. |
| CPU-curf | Otherwise the current frequency ('curf') is shown at the moment that the sample is taken. |
| CPU-curscal | Otherwise the current scaling percentage ('curscal') is shown at the moment that the sample is taken. |
| CPL-avg1 | The load average figures reflecting the number of threads that are available to run on a CPU (i.e. part of the runqueue) or that are waiting for disk I/O, over 1 minutes. |
| CPL-avg5 | The load average figures reflecting the number of threads that are available to run on a CPU (i.e. part of the runqueue) or that are waiting for disk I/O, over 5 minutes. |
| CPL-avg15 | The load average figures reflecting the number of threads that are available to run on a CPU (i.e. part of the runqueue) or that are waiting for disk I/O, over 15 minutes. |
| \Processor Information(_Total)\CSwitches | The number of context switches ('csw') |
| CPL-intr | The number of serviced interrupts ('intr') |
| CPL-numcpu | The number of available CPUs |
| GPU-gpubusy | The subsequent columns show the percentage of time that one or more kernels were executing on the GPU ('gpubusy') |
| GPU-membusy | The percentage of time that global (device) memory was being read or written ('membusy') |
| GPU-memocc | The occupation percentage of memory ('memocc') |
| GPU-total | The total memory ('total') |
| GPU-used | The memory being in use at the moment of the sample ('used') |
| GPU-usavg | The average memory being in use during the sample time ('usavg') |
| GPU-#proc | The number of processes being active on the GPU at the moment of the sample ('#proc') |
| GPU-type | The type of GPU |
| \Memory\Total Byte | The total amount of physical memory ('tot') |
| \Memory\Free Byte | The amount of memory which is currently free ('free') |
| \Memory\Cached Byte | The amount of memory in use as page cache including the total resident shared memory ('cache') |
| MEM-dirty | The amount of memory within the page cache that has to be flushed to disk ('dirty') |
| \Memory\Buffers Byte | The amount of memory used for filesystem meta data ('buff') |
| MEM-slab | The amount of memory being used for kernel mallocs ('slab') |
| MEM-slrec | The amount of slab memory that is reclaimable ('slrec') |
| MEM-shmem | The resident size of shared memory including tmpfs ('shmem') |
| MEM-shrss | The resident size of shared memory ('shrss') |
| MEM-shswp | The amount of shared memory that is currently swapped ('shswp') |
| MEM-vmbal | The amount of memory that is currently claimed by vmware's balloon driver ('vmbal') |
| MEM-zfarc | The amount of memory that is currently claimed by the ARC (cache) of ZFSonlinux ('zfarc') |
| MEM-hptot | The amount of memory that is claimed for huge pages ('hptot') |
| MEM-hpuse | The amount of huge page memory that is really in use ('hpuse') |
| SWP-tot | The total amount of swap space on disk ('tot') |
| SWP-free | The amount of free swap space ('free') |
| SWP-swcac | The size of the swap cache ('swcac') |
| SWP-vmcom | The committed virtual memory space ('vmcom') |
| SWP-vmlim | The maximum limit of the committed space ('vmlim', which is by default swap size plus 50% of memory size) is shown. |
| PAG-scan | The number of scanned pages ('scan') due to the fact that free memory drops below a particular threshold |
| PAG-stall | The number times that the kernel tries to reclaim pages due to an urgent need ('stall'). |
| PAG-swin | The number of memory pages the system read from swap space ('swin') |
| PAG-swout | The number of memory pages the system wrote to swap space ('swout') are shown |
| PSI | Pressure Stall Information. - **NOT ENABLED** |
| LVM/MDD/DSK | Logical volume/multiple device/disk utilization. |
| LVM/MDD/DSK-busy | The busy percentage i.e. the portion of time that the unit was busy handling requests ('busy') |
| LVM/MDD/DSK-read | The number of read requests issued ('read') |
| LVM/MDD/DSK-write | The number of write requests issued ('write') |
| LVM/MDD/DSK-KiB/r | The number of KiBytes per read ('KiB/r') |
| LVM/MDD/DSK-KiB/w | The number of KiBytes per write ('KiB/w') |
| LVM/MDD/DSK-MBr/s | The number of MiBytes per second throughput for reads ('MBr/s') |
| LVM/MDD/DSK-MBw/s | The number of MiBytes per second throughput for writes ('MBw/s') |
| LVM/MDD/DSK-avq | The average queue depth ('avq') |
| LVM/MDD/DSK-avio | The average number of milliseconds needed by a request ('avio') for seek, latency and data transfer. |
| NFM | Network Filesystem (NFS) mount at the client side. |
| NFM-srv | The mounted server directory, the name of the server ('srv') |
| NFM-read | The total number of bytes physically read from the server ('read') |
| NFM-write | The total number of bytes physically written to the server ('write') |
| NFM-nread | Data transfer is subdivided in the number of bytes read via normal read() system calls ('nread') |
| NFM-nwrit | The number of bytes written via normal read() system calls ('nwrit') |
| NFM-dread | The number of bytes read via direct I/O ('dread') |
| NFM-dwrit | The number of bytes written via direct I/O ('dwrit') |
| NFM-mread | The number of bytes read via memory mapped I/O pages ('mread') |
| NFM-mwrit | The number of bytes written via memory mapped I/O pages ('mwrit') |
| NFC | Network Filesystem (NFS) client side counters. - **NOT ENABLED** |
| NFC-rpc | The number of RPC calls issues by local processes ('rpc') |
| NFC-rpwrite | The number of read RPC calls ('read') and write RPC calls ('rpwrite') issued to the NFS server |
| NFC-retxmit | The number of RPC calls being retransmitted ('retxmit') |
| NFC-autref | The number of authorization refreshes ('autref'). |
| NFS | Network Filesystem (NFS) server side counters. - **NOT ENABLED** |
| NFS-rpc | The number of RPC calls received from NFS clients ('rpc') |
| NFS-cwrit | The number of read RPC calls received ('cread'), the number of write RPC calls received ('cwrit') |
| NFS-MBcr/s | The number of Megabytes/second returned to read requests by clients ('MBcr/s') |
| NFS-MBcw/s | The number of Megabytes/second passed in write requests by clients ('MBcw/s') |
| NFS-nettcp | The number of network requests handled via TCP ('nettcp') |
| NFS-netudp | The number of network requests handled via UDP ('netudp') |
| NFS-rcmiss | The number of reply cache hits ('rchits'), the number of reply cache misses ('rcmiss') |
| NFS-rcnoca | The number of uncached requests ('rcnoca') |
| NFS-badfmt | The number of requests with a bad format ('badfmt') |
| NFS-badaut | Bad authorization ('badaut') |
| NFS-badcln | The number of bad clients ('badcln'). |
| NET-tcpi | The number of received TCP segments including those received in error ('tcpi') |
| NET-tcpo | The number of transmitted TCP segments excluding those containing only retransmitted octets ('tcpo') |
| NET-udpi | The number of UDP datagrams received ('udpi') |
| NET-udpo | The number of UDP datagrams transmitted ('udpo') |
| NET-tcpao | The number of active TCP opens ('tcpao') |
| NET-tcppo | The number of passive TCP opens ('tcppo') |
| NET-tcprs | The number of TCP output retransmissions ('tcprs') |
| NET-tcpie | The number of TCP input errors ('tcpie') |
| NET-tcpor | The number of TCP output resets ('tcpor') |
| NET-udpnp | The number of UDP no ports ('udpnp') |
| NET-udpie | The number of UDP input errors ('udpie') |
| NET-ipi | The number of IP datagrams received from interfaces, including those received in error ('ipi') |
| NET-ipo | The number of IP datagrams that local higher-layer protocols offered for transmission ('ipo') |
| NET-ipfrw | The number of received IP datagrams which were forwarded to other interfaces ('ipfrw') |
| NET-deliv | The number of IP datagrams which were delivered to local higher-layer protocols ('deliv') |
| NET-icmpi | The number of received ICMP datagrams ('icmpi') |
| NET-icmpo | The number of transmitted ICMP datagrams ('icmpo') |
| NET-pcki | The number of received packets ('pcki') |
| NET-pcko | The number of transmitted packets ('pcko') |
| NET-sp | The line speed of the interface ('sp') |
| NET-si | The effective amount of bits received per second ('si') |
| NET-so | The effective amount of bits transmitted per second ('so') |
| NET-coll | The number of collisions ('coll') |
| NET-mlti | The number of received multicast packets ('mlti') |
| NET-erri | The number of errors while receiving a packet ('erri') |
| NET-erro | The number of errors while transmitting a packet ('erro') |
| NET-drpi | The number of received packets dropped ('drpi') |
| NET-drpo | The number of transmitted packets dropped ('drpo') |
| IFB | Infiniband utilization. - **NOT ENABLED** |

# Platform Integration
The Atop toolset is integrated into the Virtual Client platform as a background monitor called the PerfCounterMonitor and is part of the default monitors for the
Virtual Client. Performance counters are sampled every 1 second during the monitor frequency interval noted in the parameters below. At the 
end of this monitor frequency interval, the performance counter samples are aggregated and then emitted. Most of the aggregations are averages;
however, there are a few counters that track the min, max and median of the samples during the frequency interval. The monitor frequency can be
adjusted to change the precision of the counter samples window.

See the link at the top for more details on the exact performance counters that are captured.

[Monitor Profiles](./monitor-profiles.md)

### Monitor Parameters
The following parameters are available on the monitor component .

| Parameter                 | Purpose                                                                         | Default value |
|---------------------------|---------------------------------------------------------------------------------|---------------|
| Scenario                  | Optional. A description of the purpose of the monitor within the overall profile workflow. |    |
| MonitorFrequency          | Optional. Defines the frequency (timespan) at which performance counters will be captured/emitted (e.g. 00:01:00). | 00:05:00 |
| MonitorWarmupPeriod       | Optional. Defines a period of time (timespan) to wait before starting to track/capture performance counters (e.g. 00:03:00). This allows the system to get to a more typical operational state and generally results better representation for the counters captured. | 00:05:00 |
| MetricFilter              | Optional. A comma-delimited list of performance counter names to capture. The default behavior is to capture/emit all performance counters (e.g. \Processor Information(_Total)\% System Time,\Processor Information(_Total)\% User Time). This allows the profile author to focus on a smaller/specific subset of the counters. This is typically used when a lower monitor frequency is required for higher sample precision to keep the size of the data sets emitted by the Virtual Client to a minimum. | |
