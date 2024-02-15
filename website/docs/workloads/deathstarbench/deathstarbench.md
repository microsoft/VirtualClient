# DeathStarBench
DeathStarBench is an open-source benchmark suite for cloud microservices.
DeathStarBench includes six end-to-end services, four for cloud systems, and one for cloud-edge systems running on drone swarms.
* Social Network
* Media Service
* Hotel Reservation
* E-Commerce Site (not yet onboarded)
* Banking System (not yet onboarded)
* Drone coordination system (not yet onboarded)

In VC, we have integrated 3 end-to-end services that are released (i.e, SocialNetwork, Media Service, Hotel Reservation).
We use DeathStarBench to study the architectural characteristics of microservices, their implications in networking and operating systems,
their challenges with respect to cluster management, and their trade-offs in terms of application design and programming frameworks.

This toolset was compiled from the github repo to integrate into VirtualClient.

* [DeathStarBench Github](https://github.com/delimitrou/DeathStarBench)
* [DeathStarBench Research paper](https://gy1005.github.io/publication/2019.asplos.deathstarbench/2019.asplos.deathstarbench.pdf?msclkid=1b5071f9d01a11ec8f1946ebedfa484d)

## What is Being Measured?
DeathStarBench is designed to anlayze network communication latencies and throughput between client and server systems. It produces different percentile latencies for network 
communication and also data transfer/sec for each HTTP request.

### Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the DeathStarBench workload.

| Scenario Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------|-------------|---------------------|---------------------|---------------------|------|
| hotelReservation_MixedWorkload | 100% Network Latency | 8.67 | 208.64 | 21.41504 | ms |
| hotelReservation_MixedWorkload | 50% Network Latency | 320.0 | 596.0 | 459.076 | us |
| hotelReservation_MixedWorkload | 75% Network Latency | 362.0 | 687.0 | 541.96 | us |
| hotelReservation_MixedWorkload | 90% Network Latency | 0.86 | 1.06 | 0.925 | ms |
| hotelReservation_MixedWorkload | 90% Network Latency | 431.0 | 846.0 | 667.4628099173554 | us |
| hotelReservation_MixedWorkload | 99% Network Latency | 0.87 | 4.8 | 2.1194799999999995 | ms |
| hotelReservation_MixedWorkload | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| hotelReservation_MixedWorkload | 99.99% Network Latency | 5.0 | 23.98 | 10.10556 | ms |
| hotelReservation_MixedWorkload | Avg Req/sec | 53.05 | 53.82 | 53.33760000000001 |  |
| hotelReservation_MixedWorkload | Avg network Latency | 368.63 | 713.42 | 533.17708 | us |
| hotelReservation_MixedWorkload | Stdev Req/sec | 74.3 | 77.5 | 74.93035999999998 |  |
| hotelReservation_MixedWorkload | Stdev network Latency | 171.58 | 821.33 | 422.1610080645161 | us |
| hotelReservation_MixedWorkload | Stdev network Latency | 0.86 | 1.01 | 0.935 | ms |
| hotelReservation_MixedWorkload | Transfer/sec | 1.61 | 1.62 | 1.6100399999999998 | MB |
| mediaMicroservices_ComposeReviews | 100% Network Latency | 8.57 | 216.7 | 45.076381322957207 | ms |
| mediaMicroservices_ComposeReviews | 100% Network Latency | 3.14 | 3.14 | 3.14 | s |
| mediaMicroservices_ComposeReviews | 50% Network Latency | 377.0 | 619.0 | 470.984496124031 | us |
| mediaMicroservices_ComposeReviews | 75% Network Latency | 0.87 | 0.93 | 0.8966666666666667 | ms |
| mediaMicroservices_ComposeReviews | 75% Network Latency | 481.0 | 846.0 | 597.156626506024 | us |
| mediaMicroservices_ComposeReviews | 90% Network Latency | 594.0 | 848.0 | 699.4677419354839 | us |
| mediaMicroservices_ComposeReviews | 90% Network Latency | 0.87 | 1.71 | 1.1991666666666666 | ms |
| mediaMicroservices_ComposeReviews | 99% Network Latency | 0.96 | 82.18 | 3.6118992248062016 | ms |
| mediaMicroservices_ComposeReviews | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| mediaMicroservices_ComposeReviews | 99.99% Network Latency | 5.4 | 122.75 | 18.143735408560315 | ms |
| mediaMicroservices_ComposeReviews | 99.99% Network Latency | 1.69 | 1.69 | 1.69 | s |
| mediaMicroservices_ComposeReviews | Avg Req/sec | 53.05 | 53.58 | 53.26624031007752 |  |
| mediaMicroservices_ComposeReviews | Avg network Latency | 448.1 | 849.99 | 577.9506779661016 | us |
| mediaMicroservices_ComposeReviews | Avg network Latency | 0.86 | 8.99 | 1.2863636363636364 | ms |
| mediaMicroservices_ComposeReviews | Stdev Req/sec | 74.2 | 77.74 | 74.93906976744185 |  |
| mediaMicroservices_ComposeReviews | Stdev network Latency | 202.22 | 841.95 | 457.2306629834254 | us |
| mediaMicroservices_ComposeReviews | Stdev network Latency | 0.87 | 99.82 | 2.4854545454545455 | ms |
| mediaMicroservices_ComposeReviews | Transfer/sec | 214.41 | 215.28 | 214.56081395348839 | KB |
| socialNetwork_ComposePost | 100% Network Latency | 1.03 | 1.03 | 1.03 | m |
| socialNetwork_ComposePost | 100% Network Latency | 9.18 | 206.46 | 26.08708904109589 | ms |
| socialNetwork_ComposePost | 50% Network Latency | 314.0 | 536.0 | 431.31849315068498 | us |
| socialNetwork_ComposePost | 50% Network Latency | 14.08 | 14.08 | 14.08 | s |
| socialNetwork_ComposePost | 75% Network Latency | 369.0 | 707.0 | 517.6746575342465 | us |
| socialNetwork_ComposePost | 75% Network Latency | 18.38 | 18.38 | 18.38 | s |
| socialNetwork_ComposePost | 90% Network Latency | 0.86 | 1.1 | 0.943 | ms |
| socialNetwork_ComposePost | 90% Network Latency | 461.0 | 848.0 | 640.9929078014185 | us |
| socialNetwork_ComposePost | 90% Network Latency | 23.51 | 23.51 | 23.51 | s |
| socialNetwork_ComposePost | 99% Network Latency | 841.0 | 841.0 | 841.0 | us |
| socialNetwork_ComposePost | 99% Network Latency | 0.86 | 5.59 | 2.0380068728522335 | ms |
| socialNetwork_ComposePost | 99% Network Latency | 35.36 | 35.36 | 35.36 | s |
| socialNetwork_ComposePost | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| socialNetwork_ComposePost | 99.99% Network Latency | 0.93 | 0.93 | 0.93 | m |
| socialNetwork_ComposePost | 99.99% Network Latency | 5.24 | 131.71 | 10.476369863013696 | ms |
| socialNetwork_ComposePost | Avg Req/sec | 50.66 | 53.62 | 53.25143344709897 |  |
| socialNetwork_ComposePost | Avg network Latency | 357.53 | 750.97 | 502.6953082191782 | us |
| socialNetwork_ComposePost | Avg network Latency | 14.75 | 14.75 | 14.75 | s |
| socialNetwork_ComposePost | Stdev Req/sec | 74.21 | 76.43 | 74.77890784982935 |  |
| socialNetwork_ComposePost | Stdev network Latency | 178.71 | 842.99 | 402.9131468531469 | us |
| socialNetwork_ComposePost | Stdev network Latency | 0.9 | 2.3 | 1.1783333333333333 | ms |
| socialNetwork_ComposePost | Stdev network Latency | 6.96 | 6.96 | 6.96 | s |
| socialNetwork_ComposePost | Transfer/sec | 203.52 | 215.21 | 214.512457337884 | KB |
| socialNetwork_MixedWorkload | 100% Network Latency | 10.57 | 212.74 | 34.00373188405797 | ms |
| socialNetwork_MixedWorkload | 50% Network Latency | 260.0 | 545.0 | 423.28985507246378 | us |
| socialNetwork_MixedWorkload | 75% Network Latency | 319.0 | 649.0 | 509.4528985507246 | us |
| socialNetwork_MixedWorkload | 90% Network Latency | 0.86 | 1.07 | 0.9120000000000001 | ms |
| socialNetwork_MixedWorkload | 90% Network Latency | 397.0 | 837.0 | 634.6826568265683 | us |
| socialNetwork_MixedWorkload | 99% Network Latency | 0.92 | 4.74 | 2.2688043478260875 | ms |
| socialNetwork_MixedWorkload | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| socialNetwork_MixedWorkload | 99.99% Network Latency | 5.2 | 61.57 | 12.163768115942029 | ms |
| socialNetwork_MixedWorkload | Avg Req/sec | 52.93 | 53.59 | 53.20576086956522 |  |
| socialNetwork_MixedWorkload | Avg network Latency | 318.75 | 679.17 | 503.808695652174 | us |
| socialNetwork_MixedWorkload | Stdev Req/sec | 74.21 | 75.97 | 74.77231884057972 |  |
| socialNetwork_MixedWorkload | Stdev network Latency | 186.21 | 837.75 | 450.1571102661597 | us |
| socialNetwork_MixedWorkload | Stdev network Latency | 0.87 | 1.08 | 0.9830769230769232 | ms |
| socialNetwork_MixedWorkload | Transfer/sec | 2.94 | 2.95 | 2.9403985507246377 | MB |
| socialNetwork_ReadHomeTimeline | 100% Network Latency | 9.78 | 231.93 | 30.178617021276595 | ms |
| socialNetwork_ReadHomeTimeline | 50% Network Latency | 335.0 | 559.0 | 455.40070921985815 | us |
| socialNetwork_ReadHomeTimeline | 75% Network Latency | 396.0 | 760.0 | 547.6205673758865 | us |
| socialNetwork_ReadHomeTimeline | 90% Network Latency | 503.0 | 849.0 | 670.3127413127413 | us |
| socialNetwork_ReadHomeTimeline | 90% Network Latency | 0.85 | 1.2 | 0.9400000000000002 | ms |
| socialNetwork_ReadHomeTimeline | 99% Network Latency | 0.91 | 4.76 | 2.20468085106383 | ms |
| socialNetwork_ReadHomeTimeline | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| socialNetwork_ReadHomeTimeline | 99.99% Network Latency | 5.18 | 28.01 | 10.59241134751773 | ms |
| socialNetwork_ReadHomeTimeline | Avg Req/sec | 53.01 | 53.69 | 53.300567375886519 |  |
| socialNetwork_ReadHomeTimeline | Avg network Latency | 383.4 | 765.05 | 533.2132269503546 | us |
| socialNetwork_ReadHomeTimeline | Stdev Req/sec | 74.34 | 76.32 | 74.8363829787234 |  |
| socialNetwork_ReadHomeTimeline | Stdev network Latency | 186.09 | 826.21 | 439.895 | us |
| socialNetwork_ReadHomeTimeline | Stdev network Latency | 0.91 | 1.42 | 1.0525 | ms |
| socialNetwork_ReadHomeTimeline | Transfer/sec | 213.99 | 215.23 | 214.5518439716312 | KB |
| socialNetwork_ReadUserTimeline | 100% Network Latency | 9.57 | 211.58 | 31.61867383512545 | ms |
| socialNetwork_ReadUserTimeline | 50% Network Latency | 333.0 | 558.0 | 455.64874551971328 | us |
| socialNetwork_ReadUserTimeline | 75% Network Latency | 392.0 | 763.0 | 547.831541218638 | us |
| socialNetwork_ReadUserTimeline | 90% Network Latency | 495.0 | 848.0 | 668.3110236220473 | us |
| socialNetwork_ReadUserTimeline | 90% Network Latency | 0.85 | 1.22 | 0.9456 | ms |
| socialNetwork_ReadUserTimeline | 99% Network Latency | 0.9 | 5.04 | 2.2501075268817205 | ms |
| socialNetwork_ReadUserTimeline | 99% Req/sec | 300.0 | 300.0 | 300.0 |  |
| socialNetwork_ReadUserTimeline | 99.99% Network Latency | 5.07 | 33.31 | 11.192293906810037 | ms |
| socialNetwork_ReadUserTimeline | Avg Req/sec | 53.02 | 53.68 | 53.3015770609319 |  |
| socialNetwork_ReadUserTimeline | Avg network Latency | 380.75 | 765.2 | 535.2530107526883 | us |
| socialNetwork_ReadUserTimeline | Stdev Req/sec | 74.36 | 76.3 | 74.83716845878137 |  |
| socialNetwork_ReadUserTimeline | Stdev network Latency | 0.89 | 1.03 | 0.95375 | ms |
| socialNetwork_ReadUserTimeline | Stdev network Latency | 185.79 | 848.64 | 446.7977490774908 | us |
| socialNetwork_ReadUserTimeline | Transfer/sec | 214.48 | 215.13 | 214.55741935483872 | KB |
