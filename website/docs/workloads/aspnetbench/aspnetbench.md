# AspNetBenchmark
AspNetBenchmark is a benchmark developed by MSFT ASPNET team, based on open source benchmark TechEmpower.  
This workload has server and client part, on the same test machine. The server part is started as a ASPNET service. The client calls server using open source bombardier binaries.  
Bombardier binaries could be downloaded from Github release, or directly compile from source using "go build ."

Even though both AspNetBenchmarks and Bombardier are open source, this workload is relatively 

* [AspNetBenchmarks Github](https://github.com/aspnet/benchmarks)
* [Bombardier Github](https://github.com/codesenberg/bombardier)
* [Bombardier Release](https://github.com/codesenberg/bombardier/releases/tag/v1.2.5)

## Setup
1. VC installs dotnet SDK
2. VC clones AspNetBenchmarks github repo
3. dotnet build src/benchmarks project in AspNetBenchmarks repo
4. Use dotnet to start server
```
dotnet <path_to_binary>\Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:5000 --server Kestrel --kestrelTransport Sockets --protocol http --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7" --header "Connection: keep-alive" 
```
5. Use bombardier to start client
```
bombardier-windows-amd64.exe -d 15s -c 256 -t 2s --fasthttp --insecure -l http://localhost:5000/json --print r --format json
```


### Supported Platforms

* linux-x64
* linux-arm64
* win-x64
* win-arm64