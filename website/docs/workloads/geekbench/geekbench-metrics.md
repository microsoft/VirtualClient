# GeekBench5 Workload Metrics
The following document illustrates the type of results that are emitted by the GeekBench5 workload and captured by the
Virtual Client for net impact analysis.


### Workload-Specific Metrics
The following metrics are emitted by the GeekBench5 workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-CPU-GEEKBENCH.json | AES-XTS (Multi-Core) | Raw Value | 1.26 | 2.35 | 2.2361644951140167 | GB/sec |
| PERF-CPU-GEEKBENCH.json | AES-XTS (Single-Core) | Raw Value | 1.21 | 2.09 | 1.9808686210640743 | GB/sec |
| PERF-CPU-GEEKBENCH.json | AES-XTS Score (Multi-Core) | Test Score | 742.0 | 1379.0 | 1311.7852877307276 | score |
| PERF-CPU-GEEKBENCH.json | AES-XTS Score (Single-Core) | Test Score | 708.0 | 1226.0 | 1162.0089576547233 | score |
| PERF-CPU-GEEKBENCH.json | Camera (Multi-Core) | Raw Value | 5.02 | 9.21 | 8.824011943539614 | images/sec |
| PERF-CPU-GEEKBENCH.json | Camera (Single-Core) | Raw Value | 4.58 | 8.76 | 8.354275244299679 | images/sec |
| PERF-CPU-GEEKBENCH.json | Camera Score (Multi-Core) | Test Score | 433.0 | 794.0 | 761.2090119435396 | score |
| PERF-CPU-GEEKBENCH.json | Camera Score (Single-Core) | Test Score | 395.0 | 756.0 | 720.6886536373507 | score |
| PERF-CPU-GEEKBENCH.json | Clang (Multi-Core) | Raw Value | 2.51 | 7.83 | 7.425597176981524 | Klines/sec |
| PERF-CPU-GEEKBENCH.json | Clang (Single-Core) | Raw Value | 3.47 | 6.51 | 6.240559174810017 | Klines/sec |
| PERF-CPU-GEEKBENCH.json | Clang Score (Multi-Core) | Test Score | 322.0 | 1005.0 | 953.0298588490771 | score |
| PERF-CPU-GEEKBENCH.json | Clang Score (Single-Core) | Test Score | 445.0 | 836.0 | 800.9562975027144 | score |
| PERF-CPU-GEEKBENCH.json | Cryptography Score (Multi-Core) | Test Score | 742.0 | 1379.0 | 1311.7852877307276 | score |
| PERF-CPU-GEEKBENCH.json | Cryptography Score (Single-Core) | Test Score | 708.0 | 1226.0 | 1162.0089576547233 | score |
| PERF-CPU-GEEKBENCH.json | Face Detection (Multi-Core) | Raw Value | 5.0 | 7.37 | 7.172499999999988 | images/sec |
| PERF-CPU-GEEKBENCH.json | Face Detection (Single-Core) | Raw Value | 3.83 | 6.97 | 6.708667209554847 | images/sec |
| PERF-CPU-GEEKBENCH.json | Face Detection Score (Multi-Core) | Test Score | 649.0 | 957.0 | 931.612920738328 | score |
| PERF-CPU-GEEKBENCH.json | Face Detection Score (Single-Core) | Test Score | 498.0 | 905.0 | 871.3536916395223 | score |
| PERF-CPU-GEEKBENCH.json | FloatingPoint Score (Multi-Core) | Test Score | 875.0 | 1099.0 | 1068.0024429967428 | score |
| PERF-CPU-GEEKBENCH.json | FloatingPoint Score (Single-Core) | Test Score | 642.0 | 920.0 | 892.807546145494 | score |
| PERF-CPU-GEEKBENCH.json | Gaussian Blur (Multi-Core) | Raw Value | 22.9 | 46.1 | 44.88300760043444 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Gaussian Blur (Single-Core) | Raw Value | 26.5 | 39.3 | 37.38417480998918 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Gaussian Blur Score (Multi-Core) | Test Score | 416.0 | 839.0 | 816.5222584147666 | score |
| PERF-CPU-GEEKBENCH.json | Gaussian Blur Score (Single-Core) | Test Score | 482.0 | 716.0 | 680.0738327904452 | score |
| PERF-CPU-GEEKBENCH.json | HDR (Multi-Core) | Raw Value | 14.1 | 24.8 | 24.09500542888169 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | HDR (Single-Core) | Raw Value | 11.6 | 22.7 | 22.018431053202997 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | HDR Score (Multi-Core) | Test Score | 1035.0 | 1817.0 | 1768.249457111835 | score |
| PERF-CPU-GEEKBENCH.json | HDR Score (Single-Core) | Test Score | 853.0 | 1662.0 | 1615.9367535287732 | score |
| PERF-CPU-GEEKBENCH.json | HTML5 (Multi-Core) | Raw Value | 437.1 | 922.7 | 886.5702225841469 | KElements/sec |
| PERF-CPU-GEEKBENCH.json | HTML5 (Single-Core) | Raw Value | 383.8 | 951.7 | 892.1498099891434 | KElements/sec |
| PERF-CPU-GEEKBENCH.json | HTML5 Score (Multi-Core) | Test Score | 372.0 | 786.0 | 755.0415309446254 | score |
| PERF-CPU-GEEKBENCH.json | HTML5 Score (Single-Core) | Test Score | 327.0 | 811.0 | 759.8032030401737 | score |
| PERF-CPU-GEEKBENCH.json | Horizon Detection (Multi-Core) | Raw Value | 17.4 | 26.0 | 25.055456026058658 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Horizon Detection (Single-Core) | Raw Value | 12.6 | 21.0 | 20.195276872964159 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Horizon Detection Score (Multi-Core) | Test Score | 705.0 | 1054.0 | 1016.4828990228014 | score |
| PERF-CPU-GEEKBENCH.json | Horizon Detection Score (Single-Core) | Test Score | 510.0 | 853.0 | 819.3455483170467 | score |
| PERF-CPU-GEEKBENCH.json | Image Compression (Multi-Core) | Raw Value | 30.1 | 51.5 | 49.88078175895753 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Image Compression (Single-Core) | Raw Value | 24.9 | 44.1 | 42.71745385450586 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Image Compression Score (Multi-Core) | Test Score | 636.0 | 1088.0 | 1054.4169381107493 | score |
| PERF-CPU-GEEKBENCH.json | Image Compression Score (Single-Core) | Test Score | 527.0 | 933.0 | 903.0100434310532 | score |
| PERF-CPU-GEEKBENCH.json | Image Inpainting (Multi-Core) | Raw Value | 49.8 | 85.3 | 81.46028773072759 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Image Inpainting (Single-Core) | Raw Value | 39.2 | 73.2 | 69.65016286644959 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Image Inpainting Score (Multi-Core) | Test Score | 1014.0 | 1738.0 | 1660.501628664495 | score |
| PERF-CPU-GEEKBENCH.json | Image Inpainting Score (Single-Core) | Test Score | 799.0 | 1492.0 | 1419.764115092291 | score |
| PERF-CPU-GEEKBENCH.json | Integer Score (Multi-Core) | Test Score | 625.0 | 941.0 | 914.0887622149837 | score |
| PERF-CPU-GEEKBENCH.json | Integer Score (Single-Core) | Test Score | 485.0 | 832.0 | 804.042345276873 | score |
| PERF-CPU-GEEKBENCH.json | Machine Learning (Multi-Core) | Raw Value | 15.9 | 27.1 | 25.364087947882785 | images/sec |
| PERF-CPU-GEEKBENCH.json | Machine Learning (Single-Core) | Raw Value | 14.7 | 24.2 | 22.42418566775247 | images/sec |
| PERF-CPU-GEEKBENCH.json | Machine Learning Score (Multi-Core) | Test Score | 410.0 | 702.0 | 656.4011943539631 | score |
| PERF-CPU-GEEKBENCH.json | Machine Learning Score (Single-Core) | Test Score | 380.0 | 627.0 | 580.3208469055375 | score |
| PERF-CPU-GEEKBENCH.json | Multi-Core Score | Test Score | 767.0 | 1005.0 | 980.1767100977198 | score |
| PERF-CPU-GEEKBENCH.json | N-Body Physics (Multi-Core) | Raw Value | 752.3 | 992.2 | 927.4999999999999 | Kpairs/sec |
| PERF-CPU-GEEKBENCH.json | N-Body Physics (Multi-Core) | Raw Value | 1.01 | 1.25 | 1.1558626328699893 | Mpairs/sec |
| PERF-CPU-GEEKBENCH.json | N-Body Physics (Single-Core) | Raw Value | 596.1 | 941.1 | 888.0819761129206 | Kpairs/sec |
| PERF-CPU-GEEKBENCH.json | N-Body Physics Score (Multi-Core) | Test Score | 601.0 | 995.0 | 923.1102062975027 | score |
| PERF-CPU-GEEKBENCH.json | N-Body Physics Score (Single-Core) | Test Score | 476.0 | 752.0 | 709.78664495114 | score |
| PERF-CPU-GEEKBENCH.json | Navigation (Multi-Core) | Raw Value | 1.48 | 2.89 | 2.7101140065146289 | MTE/sec |
| PERF-CPU-GEEKBENCH.json | Navigation (Single-Core) | Raw Value | 1.53 | 2.45 | 2.2940499457112084 | MTE/sec |
| PERF-CPU-GEEKBENCH.json | Navigation Score (Multi-Core) | Test Score | 524.0 | 1024.0 | 961.0165580890337 | score |
| PERF-CPU-GEEKBENCH.json | Navigation Score (Single-Core) | Test Score | 544.0 | 867.0 | 813.499457111835 | score |
| PERF-CPU-GEEKBENCH.json | PDF Rendering (Multi-Core) | Raw Value | 23.5 | 54.8 | 51.804044516829538 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | PDF Rendering (Single-Core) | Raw Value | 25.7 | 43.9 | 42.409690553746077 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | PDF Rendering Score (Multi-Core) | Test Score | 433.0 | 1010.0 | 954.5415309446254 | score |
| PERF-CPU-GEEKBENCH.json | PDF Rendering Score (Single-Core) | Test Score | 473.0 | 809.0 | 781.46335504886 | score |
| PERF-CPU-GEEKBENCH.json | Ray Tracing (Multi-Core) | Raw Value | 1.0 | 1.1 | 1.059050544540641 | Mpixels/sec |
| PERF-CPU-GEEKBENCH.json | Ray Tracing (Multi-Core) | Raw Value | 770.8 | 999.9 | 962.744660194175 | Kpixels/sec |
| PERF-CPU-GEEKBENCH.json | Ray Tracing (Single-Core) | Raw Value | 510.2 | 914.4 | 866.0622964169361 | Kpixels/sec |
| PERF-CPU-GEEKBENCH.json | Ray Tracing Score (Multi-Core) | Test Score | 960.0 | 1365.0 | 1315.3683496199784 | score |
| PERF-CPU-GEEKBENCH.json | Ray Tracing Score (Single-Core) | Test Score | 635.0 | 1139.0 | 1078.4891422366994 | score |
| PERF-CPU-GEEKBENCH.json | Rigid Body Physics (Multi-Core) | Raw Value | 5001.6 | 7430.6 | 7166.121661237785 | FPS |
| PERF-CPU-GEEKBENCH.json | Rigid Body Physics (Single-Core) | Raw Value | 3365.6 | 5952.7 | 5784.287106406104 | FPS |
| PERF-CPU-GEEKBENCH.json | Rigid Body Physics Score (Multi-Core) | Test Score | 807.0 | 1199.0 | 1156.6815960912052 | score |
| PERF-CPU-GEEKBENCH.json | Rigid Body Physics Score (Single-Core) | Test Score | 543.0 | 961.0 | 933.6305646036916 | score |
| PERF-CPU-GEEKBENCH.json | SQLite (Multi-Core) | Raw Value | 102.0 | 283.0 | 269.9680238870789 | Krows/sec |
| PERF-CPU-GEEKBENCH.json | SQLite (Single-Core) | Raw Value | 131.8 | 258.3 | 246.84967426710126 | Krows/sec |
| PERF-CPU-GEEKBENCH.json | SQLite Score (Multi-Core) | Test Score | 326.0 | 903.0 | 861.6783387622149 | score |
| PERF-CPU-GEEKBENCH.json | SQLite Score (Single-Core) | Test Score | 421.0 | 825.0 | 787.8889793702497 | score |
| PERF-CPU-GEEKBENCH.json | Single-Core Score | Test Score | 605.0 | 875.0 | 848.5890336590662 | score |
| PERF-CPU-GEEKBENCH.json | Speech Recognition (Multi-Core) | Raw Value | 28.7 | 42.2 | 39.75352877307272 | Words/sec |
| PERF-CPU-GEEKBENCH.json | Speech Recognition (Single-Core) | Raw Value | 14.3 | 29.7 | 28.06102062975032 | Words/sec |
| PERF-CPU-GEEKBENCH.json | Speech Recognition Score (Multi-Core) | Test Score | 898.0 | 1319.0 | 1243.3998371335507 | score |
| PERF-CPU-GEEKBENCH.json | Speech Recognition Score (Single-Core) | Test Score | 448.0 | 929.0 | 877.7090119435396 | score |
| PERF-CPU-GEEKBENCH.json | Structure from Motion (Multi-Core) | Raw Value | 4.54 | 7.3 | 7.1137079261672009 | Kpixels/sec |
| PERF-CPU-GEEKBENCH.json | Structure from Motion (Single-Core) | Raw Value | 3.3 | 6.54 | 6.369581976112893 | Kpixels/sec |
| PERF-CPU-GEEKBENCH.json | Structure from Motion Score (Multi-Core) | Test Score | 507.0 | 815.0 | 794.0420738327905 | score |
| PERF-CPU-GEEKBENCH.json | Structure from Motion Score (Single-Core) | Test Score | 369.0 | 730.0 | 710.978555917481 | score |
| PERF-CPU-GEEKBENCH.json | Text Compression (Multi-Core) | Raw Value | 3.87 | 6.27 | 6.006568946796989 | MB/sec |
| PERF-CPU-GEEKBENCH.json | Text Compression (Single-Core) | Raw Value | 2.49 | 4.79 | 4.55621064060805 | MB/sec |
| PERF-CPU-GEEKBENCH.json | Text Compression Score (Multi-Core) | Test Score | 765.0 | 1241.0 | 1187.6183496199784 | score |
| PERF-CPU-GEEKBENCH.json | Text Compression Score (Single-Core) | Test Score | 493.0 | 946.0 | 900.8458197611292 | score |
| PERF-CPU-GEEKBENCH.json | Text Rendering (Multi-Core) | Raw Value | 142.5 | 276.8 | 262.3624592833867 | KB/sec |
| PERF-CPU-GEEKBENCH.json | Text Rendering (Single-Core) | Raw Value | 128.8 | 268.0 | 251.43379478827428 | KB/sec |
| PERF-CPU-GEEKBENCH.json | Text Rendering Score (Multi-Core) | Test Score | 447.0 | 869.0 | 823.4541259500543 | score |
| PERF-CPU-GEEKBENCH.json | Text Rendering Score (Single-Core) | Test Score | 404.0 | 841.0 | 789.1682953311618 | score |