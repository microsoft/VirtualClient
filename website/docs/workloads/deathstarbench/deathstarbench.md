# DeathStarBench
DeathStarBench is an open-source benchmark suite for cloud microservices.
DeathStarBench includes six end-to-end services, four for cloud systems, and one for cloud-edge systems running on drone swarms.
* Social Network (released)
* Media Service (released)
* Hotel Reservation (released)
* E-commerce site (in progress)
* Banking System (in progress)
* Drone coordination system (in progress)

In VC, we have integrated 3 end-to-end services that are released (i.e, SocialNetwork, Media Service, Hotel Reservation).
We use DeathStarBench to study the architectural characteristics of microservices, their implications in networking and operating systems,
their challenges with respect to cluster management, and their trade-offs in terms of application design and programming frameworks.

This toolset was compiled from the github repo to integrate into VirtualClient.

* [DeathStarBench Github](https://github.com/delimitrou/DeathStarBench)
* [DeathStarBench Research paper](https://gy1005.github.io/publication/2019.asplos.deathstarbench/2019.asplos.deathstarbench.pdf?msclkid=1b5071f9d01a11ec8f1946ebedfa484d)

-----------------------------------------------------------------------

### What is Being Tested?
DeathStarBench is designed to anlayze network communications between client and server. It produces different percentile latencies for network comminucation and 
also data transfer/sec for each http request.

|Test Name   | Metric Name                          | Description                                                           |
|--------------------------------------|----------------------------------------------------------------------|
|socialNetwork_MixedWorkload	|Transfer/sec	| Throughput for given test name scenario
|socialNetwork_MixedWorkload	|100% Network Latency	
|socialNetwork_MixedWorkload	|99.99% Network Latency	
|socialNetwork_MixedWorkload	|99% Network Latency	
|socialNetwork_MixedWorkload	|90% Network Latency
|socialNetwork_MixedWorkload	|75% Network Latency
|socialNetwork_MixedWorkload	|50% Network Latency
|socialNetwork_MixedWorkload	|99% network Latency
|socialNetwork_MixedWorkload	|Stdev network Latency
|socialNetwork_MixedWorkload	|Avg network Latency
|socialNetwork_ReadUserTimeline	|Transfer/sec | Throughput for given test name scenario
|socialNetwork_ReadUserTimeline	|100% Network Latency	
|socialNetwork_ReadUserTimeline	|99.99% Network Latency	
|socialNetwork_ReadUserTimeline	|99% Network Latency	
|socialNetwork_ReadUserTimeline	|90% Network Latency	
|socialNetwork_ReadUserTimeline	|75% Network Latency	
|socialNetwork_ReadUserTimeline	|50% Network Latency	
|socialNetwork_ReadUserTimeline	|99% network Latency	
|socialNetwork_ReadUserTimeline	|Stdev network Latency	
|socialNetwork_ReadUserTimeline	|Avg network Latency	
|socialNetwork_ReadHomeTimeline	|Transfer/sec	|Throughput for given test name scenario
|socialNetwork_ReadHomeTimeline	|100% Network Latency	
|socialNetwork_ReadHomeTimeline	|99.99% Network Latency	
|socialNetwork_ReadHomeTimeline	|99% Network Latency	
|socialNetwork_ReadHomeTimeline	|90% Network Latency	
|socialNetwork_ReadHomeTimeline	|75% Network Latency	
|socialNetwork_ReadHomeTimeline	|50% Network Latency	
|socialNetwork_ReadHomeTimeline	|99% network Latency	
|socialNetwork_ReadHomeTimeline	|Stdev network Latency	
|socialNetwork_ReadHomeTimeline	|Avg network Latency	
|socialNetwork_ComposePost	|Transfer/sec	| Throughput for given test name scenario
|socialNetwork_ComposePost	|100% Network Latency	
|socialNetwork_ComposePost	|99.99% Network Latency	
|socialNetwork_ComposePost	|99% Network Latency	
|socialNetwork_ComposePost	|90% Network Latency	
|socialNetwork_ComposePost	|75% Network Latency	
|socialNetwork_ComposePost	|50% Network Latency	
|socialNetwork_ComposePost	|99% network Latency	
|socialNetwork_ComposePost	|Stdev network Latency	
|socialNetwork_ComposePost	|Avg network Latency	
|hotelReservation_MixedWorkload	|Transfer/sec	| Throughput for given test name scenario
|hotelReservation_MixedWorkload	|100% Network Latency	
|hotelReservation_MixedWorkload	|99.99% Network Latency	
|hotelReservation_MixedWorkload	|99% Network Latency	
|hotelReservation_MixedWorkload	|90% Network Latency	
|hotelReservation_MixedWorkload	|75% Network Latency	
|hotelReservation_MixedWorkload	|50% Network Latency	
|hotelReservation_MixedWorkload	|99% network Latency	
|hotelReservation_MixedWorkload	|Stdev network Latency	
|hotelReservation_MixedWorkload	|Avg network Latency	
|mediaMicroservices_ComposeReviews	|Transfer/sec	| Throughput for given test name scenario
|mediaMicroservices_ComposeReviews	|100% Network Latency	
|mediaMicroservices_ComposeReviews	|99.99% Network Latency	
|mediaMicroservices_ComposeReviews	|99% Network Latency	
|mediaMicroservices_ComposeReviews	|90% Network Latency	
|mediaMicroservices_ComposeReviews	|75% Network Latency	
|mediaMicroservices_ComposeReviews	|50% Network Latency	
|mediaMicroservices_ComposeReviews	|99% network Latency	
|mediaMicroservices_ComposeReviews	|Stdev network Latency	
|mediaMicroservices_ComposeReviews	|Avg network Latency	

-----------------------------------------------------------------------

### Supported Platforms
* Linux x64
