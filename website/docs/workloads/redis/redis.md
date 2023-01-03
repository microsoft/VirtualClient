# Redis
Redis is an open source (BSD licensed), in-memory, high-performance, distributed memory object caching system used as a database, 
cache, message broker and streaming engine.

* [Official Redis Documentation](https://redis.io/docs/about/)
* [Redis Github Repo](https://github.com/redis/redis)

Two of the widely used tools for benchmarking performance of a Redis server include:
1. [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)
2. [Redis Benchmarking Tool](https://redis.io/docs/reference/optimization/benchmarks/)

## What is Being Measured?
Either the Memtier toolset or Redis benchmark is used to generate various traffic patterns against Redis instances. These toolsets performs GET and SET operations against 
the Redis server and provides throughput and latency percentile distributions.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Memtier workload against a
Redis server.

| Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|--------------|---------------------|---------------------|---------------------|------|
| GET_Average_Latency | 0.04 | 32.005 | 0.48362615964930158 | milliSeconds |
| GET_Max_Latency | 0.087 | 82.815 | 2.9978202671016409 | milliSeconds |
| GET_Min_Latency | 0.016 | 18.512 | 0.06400326231012303 | milliSeconds |
| GET_P50_Latency | 0.039 | 31.983 | 0.43099306759098818 | milliSeconds |
| GET_P95_Latency | 0.055 | 44.031 | 1.0166923233764916 | milliSeconds |
| GET_P99_Latency | 0.063 | 56.287 | 1.711716484860842 | milliSeconds |
| GET_Requests/Sec | 7980.85 | 1515636.38 | 657043.5354113564 | requests/second |
| HSET_Average_Latency | 0.046 | 32.141 | 0.4740447570984351 | milliSeconds |
| HSET_Max_Latency | 0.095 | 77.567 | 2.9574821328439619 | milliSeconds |
| HSET_Min_Latency | 0.024 | 11.952 | 0.06351511444155553 | milliSeconds |
| HSET_P50_Latency | 0.047 | 31.983 | 0.4103505632869458 | milliSeconds |
| HSET_P95_Latency | 0.055 | 36.063 | 0.9638419228220432 | milliSeconds |
| HSET_P99_Latency | 0.079 | 66.559 | 1.612821481368201 | milliSeconds |
| HSET_Requests/Sec | 7849.53 | 1136727.25 | 569987.1470433805 | requests/second |
| INCR_Average_Latency | 0.041 | 32.09 | 0.4909997961056173 | milliSeconds |
| INCR_Max_Latency | 0.087 | 103.935 | 3.1033001325313487 | milliSeconds |
| INCR_Min_Latency | 0.024 | 23.936 | 0.06726842695483709 | milliSeconds |
| INCR_P50_Latency | 0.039 | 31.983 | 0.429000203894383 | milliSeconds |
| INCR_P95_Latency | 0.055 | 42.111 | 1.0510322153124685 | milliSeconds |
| INCR_P99_Latency | 0.071 | 92.095 | 1.7620013253134879 | milliSeconds |
| INCR_Requests/Sec | 7960.53 | 1430857.12 | 631131.4617376895 | requests/second |
| LPOP_Average_Latency | 0.049 | 33.015 | 0.4860578550310941 | milliSeconds |
| LPOP_Max_Latency | 0.103 | 107.199 | 3.0467243207422145 | milliSeconds |
| LPOP_Min_Latency | 0.024 | 25.968 | 0.0682613925986336 | milliSeconds |
| LPOP_P50_Latency | 0.047 | 31.983 | 0.4283811805484766 | milliSeconds |
| LPOP_P95_Latency | 0.063 | 43.935 | 1.0061803445815075 | milliSeconds |
| LPOP_P99_Latency | 0.079 | 62.495 | 1.6812478462557988 | milliSeconds |
| LPOP_Requests/Sec | 7645.8 | 1163162.75 | 554949.0140090728 | requests/second |
| LPUSH (needed to benchmark LRANGE)_Average_Latency | 0.047 | 32.331 | 0.4680763152528547 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_Max_Latency | 0.103 | 89.151 | 3.0183870309951059 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_Min_Latency | 0.024 | 23.92 | 0.0674498368678627 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_P50_Latency | 0.047 | 31.983 | 0.4168964110929859 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_P95_Latency | 0.063 | 43.935 | 0.9477659053833611 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_P99_Latency | 0.079 | 62.207 | 1.6001190864600324 | milliSeconds |
| LPUSH (needed to benchmark LRANGE)_Requests/Sec | 7806.7 | 1112888.88 | 552787.2849683929 | requests/second |
| LPUSH_Average_Latency | 0.049 | 32.39 | 0.5174908247527781 | milliSeconds |
| LPUSH_Max_Latency | 0.095 | 80.447 | 3.14175991436436 | milliSeconds |
| LPUSH_Min_Latency | 0.024 | 27.888 | 0.06941176470588206 | milliSeconds |
| LPUSH_P50_Latency | 0.047 | 31.983 | 0.46588184320522027 | milliSeconds |
| LPUSH_P95_Latency | 0.063 | 38.975 | 1.0646764196146397 | milliSeconds |
| LPUSH_P99_Latency | 0.079 | 66.623 | 1.7610980731980833 | milliSeconds |
| LPUSH_Requests/Sec | 7800.62 | 1112888.88 | 546899.8452446729 | requests/second |
| LRANGE_100 (first 100 elements)_Average_Latency | 0.219 | 31.556 | 2.1469685460848288 | milliSeconds |
| LRANGE_100 (first 100 elements)_Max_Latency | 0.319 | 232.959 | 12.235842577487766 | milliSeconds |
| LRANGE_100 (first 100 elements)_Min_Latency | 0.192 | 8.736 | 0.2514208809135396 | milliSeconds |
| LRANGE_100 (first 100 elements)_P50_Latency | 0.207 | 31.567 | 1.8669212887438807 | milliSeconds |
| LRANGE_100 (first 100 elements)_P95_Latency | 0.263 | 56.447 | 4.660227977161501 | milliSeconds |
| LRANGE_100 (first 100 elements)_P99_Latency | 0.295 | 102.527 | 7.441752039151712 | milliSeconds |
| LRANGE_100 (first 100 elements)_Requests/Sec | 7983.4 | 139111.11 | 61537.65344718601 | requests/second |
| LRANGE_300 (first 300 elements)_Average_Latency | 0.657 | 36.091 | 3.5058012846655797 | milliSeconds |
| LRANGE_300 (first 300 elements)_Max_Latency | 0.839 | 144.767 | 21.579035889070146 | milliSeconds |
| LRANGE_300 (first 300 elements)_Min_Latency | 0.592 | 2.752 | 0.6773694942903761 | milliSeconds |
| LRANGE_300 (first 300 elements)_P50_Latency | 0.615 | 33.631 | 3.4033213703099505 | milliSeconds |
| LRANGE_300 (first 300 elements)_P95_Latency | 0.775 | 79.487 | 6.062862561174552 | milliSeconds |
| LRANGE_300 (first 300 elements)_P99_Latency | 0.807 | 105.919 | 12.36377732463295 | milliSeconds |
| LRANGE_300 (first 300 elements)_Requests/Sec | 3586.68 | 31933.59 | 18214.11462530588 | requests/second |
| LRANGE_500 (first 450 elements)_Average_Latency | 0.989 | 41.284 | 4.857378721451877 | milliSeconds |
| LRANGE_500 (first 450 elements)_Max_Latency | 1.199 | 142.975 | 23.548218597063625 | milliSeconds |
| LRANGE_500 (first 450 elements)_Min_Latency | 0.88 | 5.92 | 1.0238686786296896 | milliSeconds |
| LRANGE_500 (first 450 elements)_P50_Latency | 0.911 | 42.111 | 4.821265497553018 | milliSeconds |
| LRANGE_500 (first 450 elements)_P95_Latency | 1.119 | 75.967 | 7.64909665579119 | milliSeconds |
| LRANGE_500 (first 450 elements)_P99_Latency | 1.167 | 110.527 | 13.705281810766719 | milliSeconds |
| LRANGE_500 (first 450 elements)_Requests/Sec | 2631.63 | 21726.68 | 12339.182366435563 | requests/second |
| LRANGE_600 (first 600 elements)_Average_Latency | 1.307 | 47.115 | 5.649815456769985 | milliSeconds |
| LRANGE_600 (first 600 elements)_Max_Latency | 1.567 | 451.839 | 26.307774877650905 | milliSeconds |
| LRANGE_600 (first 600 elements)_Min_Latency | 1.176 | 7.904 | 1.3783254486133792 | milliSeconds |
| LRANGE_600 (first 600 elements)_P50_Latency | 1.231 | 48.735 | 5.666452691680261 | milliSeconds |
| LRANGE_600 (first 600 elements)_P95_Latency | 1.479 | 104.511 | 8.99248735725938 | milliSeconds |
| LRANGE_600 (first 600 elements)_P99_Latency | 1.559 | 133.759 | 15.49082096247961 | milliSeconds |
| LRANGE_600 (first 600 elements)_Requests/Sec | 1844.91 | 16207.12 | 9321.784952589722 | requests/second |
| MSET (10 keys)_Average_Latency | 0.108 | 31.115 | 1.018267536704731 | milliSeconds |
| MSET (10 keys)_Max_Latency | 0.183 | 116.543 | 5.104883768352365 | milliSeconds |
| MSET (10 keys)_Min_Latency | 0.088 | 15.584 | 0.18198939641109289 | milliSeconds |
| MSET (10 keys)_P50_Latency | 0.103 | 31.983 | 0.91268515497553 | milliSeconds |
| MSET (10 keys)_P95_Latency | 0.135 | 44.127 | 1.9114253670473083 | milliSeconds |
| MSET (10 keys)_P99_Latency | 0.151 | 66.367 | 3.06757911908646 | milliSeconds |
| MSET (10 keys)_Requests/Sec | 8211.46 | 333866.69 | 232481.1612709011 | requests/second |
| P50lat | 0.175 | 77.823 | 15.913107353397399 | msec |
| P90lat | 0.223 | 134.143 | 20.048949115730684 | msec |
| P95lat | 0.231 | 179.199 | 24.911873409866585 | msec |
| P99_9lat | 0.311 | 311.295 | 46.154507291343488 | msec |
| P99lat | 0.271 | 239.615 | 34.136540179956579 | msec |
| PING_INLINE_Average_Latency | 0.031 | 32.654 | 0.8576825874197166 | milliSeconds |
| PING_INLINE_Max_Latency | 0.071 | 116.223 | 4.955231624018758 | milliSeconds |
| PING_INLINE_Min_Latency | 0.024 | 31.696 | 0.06140850239575872 | milliSeconds |
| PING_INLINE_P50_Latency | 0.031 | 31.983 | 0.843882454888369 | milliSeconds |
| PING_INLINE_P95_Latency | 0.047 | 43.935 | 1.82383678254664 | milliSeconds |
| PING_INLINE_P99_Latency | 0.063 | 63.967 | 3.040517789784892 | milliSeconds |
| PING_INLINE_Requests/Sec | 7752.32 | 1252000.0 | 581094.6461810576 | requests/second |
| PING_MBULK_Average_Latency | 0.03 | 32.952 | 0.715390049954124 | milliSeconds |
| PING_MBULK_Max_Latency | 0.063 | 151.423 | 4.454256193291875 | milliSeconds |
| PING_MBULK_Min_Latency | 0.016 | 31.728 | 0.06472831073503899 | milliSeconds |
| PING_MBULK_P50_Latency | 0.031 | 31.983 | 0.6089410745233973 | milliSeconds |
| PING_MBULK_P95_Latency | 0.047 | 42.495 | 1.6596985421551618 | milliSeconds |
| PING_MBULK_P99_Latency | 0.047 | 70.143 | 2.810248853094097 | milliSeconds |
| PING_MBULK_Requests/Sec | 7680.98 | 1724689.75 | 693490.7826455291 | requests/second |
| RPOP_Average_Latency | 0.048 | 32.67 | 0.4743477086200745 | milliSeconds |
| RPOP_Max_Latency | 0.087 | 87.359 | 2.9461633787021466 | milliSeconds |
| RPOP_Min_Latency | 0.024 | 31.792 | 0.06490900749350025 | milliSeconds |
| RPOP_P50_Latency | 0.047 | 31.983 | 0.4230913493398589 | milliSeconds |
| RPOP_P95_Latency | 0.063 | 43.903 | 0.9695402457052563 | milliSeconds |
| RPOP_P99_Latency | 0.079 | 68.095 | 1.6080631595045119 | milliSeconds |
| RPOP_Requests/Sec | 7728.4 | 1252000.0 | 574482.0906270068 | requests/second |
| RPUSH_Average_Latency | 0.047 | 32.51 | 0.4755399123254154 | milliSeconds |
| RPUSH_Max_Latency | 0.103 | 80.703 | 3.0799974513202166 | milliSeconds |
| RPUSH_Min_Latency | 0.032 | 19.888 | 0.06624487715363413 | milliSeconds |
| RPUSH_P50_Latency | 0.047 | 31.983 | 0.4171364053420334 | milliSeconds |
| RPUSH_P95_Latency | 0.055 | 42.271 | 1.010727189315935 | milliSeconds |
| RPUSH_P99_Latency | 0.071 | 60.607 | 1.6829286369660515 | milliSeconds |
| RPUSH_Requests/Sec | 7770.36 | 1252000.0 | 571089.7713635434 | requests/second |
| SADD_Average_Latency | 0.041 | 32.461 | 0.4328111332007951 | milliSeconds |
| SADD_Max_Latency | 0.079 | 88.639 | 2.8693630524545035 | milliSeconds |
| SADD_Min_Latency | 0.024 | 19.984 | 0.06152418820410837 | milliSeconds |
| SADD_P50_Latency | 0.039 | 31.967 | 0.38828725085385126 | milliSeconds |
| SADD_P95_Latency | 0.047 | 35.999 | 0.9137253912422898 | milliSeconds |
| SADD_P99_Latency | 0.063 | 70.463 | 1.5430375184788703 | milliSeconds |
| SADD_Requests/Sec | 7776.4 | 1430857.12 | 635851.0720176373 | requests/second |
| SET_Average_Latency | 0.04 | 32.145 | 0.6898151187684778 | milliSeconds |
| SET_Max_Latency | 0.087 | 89.983 | 3.641637169945968 | milliSeconds |
| SET_Min_Latency | 0.024 | 19.968 | 0.06945458252625107 | milliSeconds |
| SET_P50_Latency | 0.039 | 31.983 | 0.6662162299928638 | milliSeconds |
| SET_P95_Latency | 0.055 | 41.823 | 1.3665498012029769 | milliSeconds |
| SET_P99_Latency | 0.071 | 62.399 | 2.2267908043633399 | milliSeconds |
| SET_Requests/Sec | 7945.35 | 1252000.0 | 572498.2329707408 | requests/second |
| SPOP_Average_Latency | 0.037 | 32.491 | 0.3794379364836622 | milliSeconds |
| SPOP_Max_Latency | 0.079 | 97.279 | 2.638165579119087 | milliSeconds |
| SPOP_Min_Latency | 0.016 | 13.544 | 0.054340622929091839 | milliSeconds |
| SPOP_P50_Latency | 0.039 | 31.983 | 0.33848531810766677 | milliSeconds |
| SPOP_P95_Latency | 0.047 | 33.343 | 0.8019090538336049 | milliSeconds |
| SPOP_P99_Latency | 0.063 | 85.247 | 1.3729359706362153 | milliSeconds |
| SPOP_Requests/Sec | 7800.62 | 1669333.38 | 706734.1855013502 | requests/second |
| Throughput | 17241.1 | 17831817.36 | 420260.31174061438 | req/sec |
| Throughput_1 | 7991.8 | 725154.18 | 121838.8537220757 | req/sec |
| Throughput_2 | 7978.87 | 692205.02 | 125506.61857237639 | req/sec |
| Throughput_3 | 7978.82 | 682122.49 | 226745.34571946169 | req/sec |
| Throughput_4 | 7987.6 | 686825.72 | 222295.19148550728 | req/sec |
| Throughput_5 | 7997.75 | 598232.21 | 365697.4250622406 | req/sec |
| Throughput_6 | 8069.37 | 601568.91 | 361309.62545643156 | req/sec |
| Throughput_7 | 7990.86 | 599252.78 | 359559.5156846473 | req/sec |
| Throughput_8 | 7848.25 | 603902.39 | 355645.1763692946 | req/sec |
| Throughput_9 | 7959.36 | 601072.71 | 366984.76313278006 | req/sec |
| Throughput_10 | 8077.92 | 600753.97 | 359649.95155601666 | req/sec |
| Throughput_11 | 11259.51 | 606504.23 | 371196.62074688795 | req/sec |
| Throughput_12 | 12927.96 | 604422.16 | 366911.36591286308 | req/sec |
| Throughput_13 | 15823.17 | 600497.42 | 370432.29354771788 | req/sec |
| Throughput_14 | 9425.41 | 631362.58 | 367812.6839004149 | req/sec |
| Throughput_15 | 7938.37 | 600614.93 | 365751.3786307054 | req/sec |
| Throughput_16 | 8040.45 | 620613.9 | 364617.5147717842 | req/sec |
| Throughput_17 | 24216.81 | 597151.65 | 521581.0625 | req/sec |
| Throughput_18 | 14095.34 | 588297.44 | 503727.2914444444 | req/sec |
| Throughput_19 | 40187.53 | 588862.04 | 510501.55233333338 | req/sec |
| Throughput_20 | 7966.28 | 612374.9 | 516058.48594444445 | req/sec |
| Throughput_21 | 7968.36 | 595201.63 | 507762.146 | req/sec |
| Throughput_22 | 11257.82 | 585266.7 | 513529.0027777778 | req/sec |
| Throughput_23 | 14063.64 | 606528.09 | 500516.64094444448 | req/sec |
| Throughput_24 | 106473.13 | 586454.49 | 519954.47449999998 | req/sec |
| Throughput_25 | 37818.15 | 601838.71 | 525582.0592222222 | req/sec |
| Throughput_26 | 32977.5 | 596654.97 | 524031.9821111111 | req/sec |
| Throughput_27 | 72817.5 | 599946.86 | 540320.1502777778 | req/sec |
| Throughput_28 | 30742.39 | 603668.89 | 525381.3121111111 | req/sec |
| Throughput_29 | 18079.89 | 594704.31 | 527886.6923333333 | req/sec |
| Throughput_30 | 8059.06 | 598590.76 | 536338.2217777777 | req/sec |
| Throughput_31 | 7862.62 | 604166.52 | 526071.3032222221 | req/sec |
| Throughput_32 | 173970.91 | 612097.88 | 545605.0684444446 | req/sec |
| ZADD_Average_Latency | 0.049 | 32.278 | 0.47239202691680268 | milliSeconds |
| ZADD_Max_Latency | 0.095 | 76.287 | 2.975741027732463 | milliSeconds |
| ZADD_Min_Latency | 0.032 | 19.952 | 0.07185685154975509 | milliSeconds |
| ZADD_P50_Latency | 0.047 | 31.983 | 0.4249959216965742 | milliSeconds |
| ZADD_P95_Latency | 0.063 | 41.695 | 0.948325856443719 | milliSeconds |
| ZADD_P99_Latency | 0.079 | 55.935 | 1.5922194127243066 | milliSeconds |
| ZADD_Requests/Sec | 7911.42 | 1112888.88 | 537931.0682534662 | requests/second |
| ZPOPMIN_Average_Latency | 0.036 | 24.386 | 0.3803461969820555 | milliSeconds |
| ZPOPMIN_Max_Latency | 0.079 | 109.695 | 2.6673927406199025 | milliSeconds |
| ZPOPMIN_Min_Latency | 0.016 | 13.448 | 0.05634461663947769 | milliSeconds |
| ZPOPMIN_P50_Latency | 0.039 | 31.935 | 0.33162479608482828 | milliSeconds |
| ZPOPMIN_P95_Latency | 0.047 | 39.935 | 0.8215623980424145 | milliSeconds |
| ZPOPMIN_P99_Latency | 0.063 | 86.527 | 1.403702691680261 | milliSeconds |
| ZPOPMIN_Requests/Sec | 10461.41 | 1669333.38 | 700640.1472772221 | requests/second |
