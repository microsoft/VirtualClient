# Performance Counter Metrics
The following sections describe the various types of performance counters that are captured by the Virtual Client while running any of the
various workloads supported. This is a standard set of performance counters captured in both the Guest/VM as well as the Azure Host scenarios.

## Capture Intervals
Performance counters are captured on Windows systems every 1 second and are aggregated/averaged out on 10 minute intervals by default. This allows
the Virtual Client to have a large number of samples over the interval of time when calculating averages. This in turn increases the accuracy and
validity of the performance measurements.

## Guest/VM Counters (Windows Systems)
The following performance counters are captured during the duration of the Virtual Client execution on Azure VMs running a Windows operating system. 
These counters are tracked the entire time the Virtual Client is running on the intervals noted above.

Counters are captured on Windows systems using the out-of-box support in the .NET SDK for the performance counter sub-system.

| Counter Name | Example Value (min) | Example Value (max) | Example Value (avg) |
|--------------|---------------------|---------------------|---------------------|
| \IPv4\Datagrams Received/sec | 1.9394553899765015 | 523.2702026367188 | 7.459091585073898 |
| \IPv4\Datagrams Sent/sec | 1.4957220554351807 | 59.85932540893555 | 2.4022125797220039 |
| \IPv4\Datagrams/sec | 3.435187339782715 | 583.6214599609375 | 9.862171525062344 |
| \Memory\Available Bytes | 137898049536.0 | 254628593664.0 | 173305532521.59568 |
| \Memory\Cache Bytes | 65195400.0 | 117965808.0 | 98183972.33649932 |
| \Memory\Cache Faults/sec | 6.581874847412109 | 2068.153564453125 | 47.935744725541798 |
| \Memory\Committed Bytes | 24908865536.0 | 142634614784.0 | 106716024126.17639 |
| \Memory\Demand Zero Faults/sec | 58.619815826416019 | 43696.21484375 | 2503.7350609105189 |
| \Memory\Page Faults/sec | 88.71714782714844 | 43664.671875 | 3343.535956591085 |
| \Memory\Page Reads/sec | 0.06315436959266663 | 200.9219207763672 | 8.28668747248894 |
| \Memory\Page Writes/sec | 0.0 | 0.0 | 0.0 |
| \Memory\Pages Input/sec | 3.1273350715637209 | 739.6574096679688 | 34.66781720506935 |
| \Memory\Pages Output/sec | 0.0 | 0.0 | 0.0 |
| \Memory\Pages/sec | 3.127346992492676 | 739.6596069335938 | 34.62828928862161 |
| \Memory\Transition Faults/sec | 26.764577865600587 | 15638.5693359375 | 1067.3553226527884 |
| \PhysicalDisk(_Total)\% Disk Read Time | 0.00955707672983408 | 29.516769409179689 | 0.8819494984394478 |
| \PhysicalDisk(_Total)\% Disk Time | 51.839927673339847 | 166866.28125 | 99921.0147311186 |
| \PhysicalDisk(_Total)\% Disk Write Time | 51.685264587402347 | 166867.515625 | 99920.55515474607 |
| \PhysicalDisk(_Total)\% Idle Time | 60.85190963745117 | 79.03485870361328 | 65.25808287249169 |
| \PhysicalDisk(_Total)\Avg. Disk Queue Length | 2.591966390609741 | 8343.2822265625 | 4995.99608238096 |
| \PhysicalDisk(_Total)\Avg. Disk Read Queue Length | 0.000477842811960727 | 1.4758336544036866 | 0.04409263106464223 |
| \PhysicalDisk(_Total)\Avg. Disk Write Queue Length | 2.584221124649048 | 8343.2177734375 | 4995.97360443713 |
| \PhysicalDisk(_Total)\Avg. Disk sec/Read | 0.00040244602132588625 | 0.03853315860033035 | 0.00549412016542641 |
| \PhysicalDisk(_Total)\Avg. Disk sec/Transfer | 0.0033158184960484506 | 0.43584492802619936 | 0.26414447839326368 |
| \PhysicalDisk(_Total)\Avg. Disk sec/Write | 0.0033161116298288109 | 0.43584486842155459 | 0.2641487668149598 |
| \PhysicalDisk(_Total)\Disk Bytes/sec | 57389760.0 | 219434048.0 | 189502656.385346 |
| \PhysicalDisk(_Total)\Disk Read Bytes/sec | 15437.5947265625 | 10281823.0 | 378520.7838961266 |
| \PhysicalDisk(_Total)\Disk Reads/sec | 0.08935023844242096 | 160.9096221923828 | 6.552046077555928 |
| \PhysicalDisk(_Total)\Disk Transfers/sec | 745.4456176757813 | 20848.3984375 | 17039.31111275454 |
| \PhysicalDisk(_Total)\Disk Write Bytes/sec | 57268104.0 | 219379984.0 | 189124374.94708277 |
| \PhysicalDisk(_Total)\Disk Writes/sec | 743.5797119140625 | 20848.20703125 | 17036.208734941352 |
| \Processor(_Total)\% Idle Time | 5.271390438079834 | 91.41498565673828 | 71.42914129983441 |
| \Processor(_Total)\% Interrupt Time | 0.04631827771663666 | 0.8421745896339417 | 0.1747746500300198 |
| \Processor(_Total)\% Privileged Time | 1.960703730583191 | 8.808183670043946 | 2.981151686756388 |
| \Processor(_Total)\% Processor Time | 7.651301383972168 | 93.05585479736328 | 25.865997984742536 |
| \Processor(_Total)\% User Time | 5.649149417877197 | 84.65630340576172 | 22.879608902872986 |
| \Processor(_Total)\Interrupts/sec | 28545.078125 | 224772.9375 | 87820.70197485584 |
| \System\Context Switches/sec | 50054.0703125 | 637204.4375 | 182084.66010854819 |
| \System\Processes | 64.98281860351563 | 86.70774841308594 | 70.201976011116 |
| \System\System Calls/sec | 82747.8828125 | 1651668.5 | 366946.69737957938 |
| \System\Threads | 5358.17138671875 | 6486.013671875 | 5983.371521823128 |

## Guest/VM Counters (Linux Systems)
The following performance counters are captured during the duration of the Virtual Client execution on Azure VMs running a Linux operating system. 
These counters are tracked the entire time the Virtual Client is running on the intervals noted above.

Counters are captured on Linux systems using the Atop toolset/application. Atop is a package that can be installed on any Linux distribution that 
enables a wide range of performance aspects of the system to be captured. The Virtual Client integrates Atop into most workload profile scenarios 
by default.

| Counter Name | Example Value (min) | Example Value (max) | Example Value (avg) |
|--------------|---------------------|---------------------|---------------------|
| \Disk(sda)\# Reads | 209.0 | 49276.0 | 8220.819889922186 | 
| \Disk(sda)\# Writes | 0.0 | 373653.0 | 37056.128107800345 | 
| \Disk(sda)\% Busy Time | 0.0 | 78.0 | 2.199658379199089 | 
| \Disk(sda)\Avg. Request Time | 0.62 | 23.1 | 1.7087094325298902 | 
| \Disk(sdb)\# Reads | 209.0 | 49123.0 | 8721.350161320934 | 
| \Disk(sdb)\# Writes | 0.0 | 377891.0 | 49333.23363066996 | 
| \Disk(sdb)\% Busy Time | 0.0 | 78.0 | 4.555133801480356 | 
| \Disk(sdb)\Avg. Request Time | 0.63 | 26.0 | 2.230874928829001 | 
| \Disk(sdc)\# Reads | 209.0 | 37301.0 | 5218.567319716549 | 
| \Disk(sdc)\# Writes | 0.0 | 272345.0 | 56918.71029595665 | 
| \Disk(sdc)\% Busy Time | 0.0 | 78.0 | 18.264693622342646 | 
| \Disk(sdc)\Avg. Request Time | 0.64 | 26.5 | 4.556898707794915 |  
| \Disk\# Reads | 14757.0 | 181046.0 | 39070.70260011387 | 
| \Disk\# Writes | 22549.0 | 4031907.0 | 455077.6113114443 | 
| \Disk\Avg. % Busy Time | 0.0 | 69.27777777777777 | 8.526122393032623 | 
| \Disk\Avg. Request Time | 0.73 | 23.66388888888889 | 2.7740474157019059 | 
| \Memory\Buffers Bytes | 87556096.0 | 360185856.0 | 139338396.85526667 | 
| \Memory\Cached Bytes | 3006477107.2 | 63887638528.0 | 12306316238.597668 | 
| \Memory\Free Bytes | 412404940.8 | 13099650252.8 | 10407128820.115832 | 
| \Memory\Kernel Bytes | 194825420.8 | 3328599654.4 | 665495325.8609982 | 
| \Memory\Page Reclaims | 0.0 | 0.0 | 0.0 | 
| \Memory\Page Scans | 21827000.0 | 456560000.0 | 294238175.0 | 
| \Memory\Page Steals | 21550000.0 | 456000000.0 | 294089618.42105266 | 
| \Memory\Swap Space Free Bytes | 0.0 | 0.0 | 0.0 | 
| \Memory\Swap Space Reads | 0.0 | 0.0 | 0.0 | 
| \Memory\Swap Space Total Bytes | 0.0 | 0.0 | 0.0 | 
| \Memory\Swap Space Virtual Committed Bytes | 649278259.2 | 5798205849.6 | 1358700600.9040044 | 
| \Memory\Swap Space Virtual Limit Bytes | 8375186227.2 | 33715493273.6 | 12030269422.353611 | 
| \Memory\Swap Space Writes | 0.0 | 0.0 | 0.0 | 
| \Memory\Total Bytes | 16750372454.4 | 67430986547.2 | 24060538844.707223 | 
| \Network(eth0)\% Usage | 0.0 | 0.0 | 0.0 | 
| \Network(eth0)\KB/sec Received | 299.0 | 8819.0 | 2165.781094527363 | 
| \Network(eth0)\KB/sec Transmitted | 16.0 | 39.0 | 20.4958238420653 | 
| \Network(eth0)\Packets Received | 472504.0 | 1364828.0 | 945803.7923310554 | 
| \Network(eth0)\Packets Transmitted | 20515.0 | 233996.0 | 77819.65110098709 | 
| \Network\Avg. % Usage | 0.0 | 0.0 | 0.0 | 
| \Network\Avg. KB/sec Received | 299.0 | 8819.0 | 2165.781094527363 | 
| \Network\Avg. KB/sec Transmitted | 16.0 | 39.0 | 20.4958238420653 | 
| \Network\IP Datagrams Delivered | 24124.0 | 208809.0 | 78577.88325740319 | 
| \Network\IP Datagrams Forwarded | 0.0 | 0.0 | 0.0 | 
| \Network\IP Datagrams Received | 24128.0 | 208843.0 | 78586.3122627183 | 
| \Network\IP Datagrams Transmitted | 21317.0 | 239448.0 | 78745.12566438876 | 
| \Network\Packets Received | 472504.0 | 1364828.0 | 945803.7923310554 | 
| \Network\Packets Transmitted | 20515.0 | 233996.0 | 77819.65110098709 | 
| \Network\TCP Segments Received | 23124.0 | 200989.0 | 77267.61958997722 | 
| \Network\TCP Segments Transmitted | 21787.0 | 262562.0 | 85337.28777524678 | 
| \Network\UDP Segments Received | 340.0 | 7842.0 | 1222.9104024297647 | 
| \Network\UDP Segments Transmitted | 383.0 | 7888.0 | 1266.553720577069 | 
| \Processor Information(_Total)\% IOWait Time | 0.0 | 3.8833333333333335 | 0.12962962962962963 | 
| \Processor Information(_Total)\% IOWait Time Min | --- | --- | 0.0 | 
| \Processor Information(_Total)\% IOWait Time Max | --- | --- | 3.8833333333333335 | 
| \Processor Information(_Total)\% IOWait Time Median | --- | --- | 0.13962962962962963 | 
| \Processor Information(_Total)\% IRQ Time | 0.0 | 7.0 | 0.6301707779886148 | 
| \Processor Information(_Total)\% IRQ Time Min | --- | --- | 0.0 | 
| \Processor Information(_Total)\% IRQ Time Max | --- | --- | 1.0 | 
| \Processor Information(_Total)\% IRQ Time Median | --- | --- | 0.6 | 
| \Processor Information(_Total)\% Idle Time | 25.0 | 1311.0 | 356.6918406072106 | 
| \Processor Information(_Total)\% Idle Time Min | --- | --- | 0.0 | 
| \Processor Information(_Total)\% Idle Time Max | --- | --- | 55.983333333333337 | 
| \Processor Information(_Total)\% Idle Time Median | --- | --- | 11.7639876540  |
| \Processor Information(_Total)\% System Time | 0.0 | 35.0 | 4.337950664136622 | 
| \Processor Information(_Total)\% System Time Min | --- | --- | 1.727689476 | 
| \Processor Information(_Total)\% System Time Max | --- | --- | 15.717548975 | 
| \Processor Information(_Total)\% System Time Median | --- | --- | 5.875463786 | 
| \Processor Information(_Total)\% User Time | 2.0 | 373.0 | 96.4203036053131 | 
| \Processor Information(_Total)\% User Time Min | --- | --- | 24.016666666666667 | 
| \Processor Information(_Total)\% User Time Max | --- | --- | 89.25 | 
| \Processor Information(_Total)\% User Time Median | --- | --- | 78.10371380471381 | 
| \Processor Information(_Total)\Available Threads (Avg1) | 0.0 | 50.37 | 6.382355285632948 | 
| \Processor Information(_Total)\Available Threads (Avg15) | 0.0 | 47.26 | 5.097149364205731 | 
| \Processor Information(_Total)\Available Threads (Avg5) | 0.0 | 49.53 | 6.206646422471061 | 
| \Processor Information(_Total)\CSwitches | 649314.0 | 142975000.0 | 17557335.682482445 | 
| \Processor Information(_Total)\Serviced Interrupts | 188069.0 | 59030000.0 | 5904357.514329094 | 
| \Processor(cpu000)\% IOWait Time | 0.0 | 70.0 | 8.112523719165086 | 
| \Processor(cpu000)\% IRQ Time | 0.0 | 2.0 | 0.14648956356736243 | 
| \Processor(cpu000)\% Idle Time | 6.0 | 96.0 | 67.25483870967742 | 
| \Processor(cpu000)\% System Time | 0.0 | 2.0 | 0.3020872865275142 | 
| \Processor(cpu000)\% User Time | 0.0 | 93.0 | 23.901518026565467 | 
| \Processor(cpu001)\% IOWait Time | 0.0 | 69.0 | 7.474952561669829 | 
| \Processor(cpu001)\% IRQ Time | 0.0 | 0.0 | 0.0 | 
| \Processor(cpu001)\% Idle Time | 6.0 | 97.0 | 68.14667931688804 | 
| \Processor(cpu001)\% System Time | 0.0 | 1.0 | 0.19259962049335864 | 
| \Processor(cpu001)\% User Time | 0.0 | 93.0 | 23.965085388994308 | 
| \Processor(cpu002)\% IOWait Time | 0.0 | 70.0 | 7.7612903225806459 | 
| \Processor(cpu002)\% IRQ Time | 0.0 | 0.0 | 0.0 | 
| \Processor(cpu002)\% Idle Time | 6.0 | 97.0 | 67.74288425047439 | 
| \Processor(cpu002)\% System Time | 0.0 | 2.0 | 0.3189753320683112 | 
| \Processor(cpu002)\% User Time | 0.0 | 93.0 | 23.96584440227704 | 