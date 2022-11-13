# Virtual Client

[![Pull Request Build](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml)
[![Document Build](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml/badge.svg?branch=main)](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml)
[![Document Deployment](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment)
[![NuGet Release Status](https://msazure.visualstudio.com/One/_apis/build/status/OneBranch/CRC-AIR-Workloads/microsoft.VirtualClient?branchName=main)](https://msazure.visualstudio.com/One/_build/latest?definitionId=297462&branchName=main)

Virtual Client is a unified workload and system monitoring platform for running customer-representative scenarios on virtual machines or physical hosts/blades in the Azure Cloud. 
The platform supports a wide range of different industry standard/benchmark workloads used to measuring various aspects of the system under test (e.g. CPU, I/O, network performance, power consumption). It has been an inner-source project in Microsoft for two years and now it is open sourced on GitHub.
The platform additionally provides the ability to capture important performance and reliability measurements from the underlying system. The platform supports different environments including guest/VM systems, host/blade systems and data center/DC lab systems. The platform additionally supports both x64 and ARM64 compute architectures.

* [Platform Details](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.Documentation/VirtualClientPlatform.md&_a=preview)
* [Platform Design](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/VirtualClient.Documentation/VirtualClientDesign.md&_a=preview)
* [Developer Guide](https://github.com/Azure/AzureVirtualClient/blob/main/src/VirtualClient/DEVELOPER_GUIDE.md&_a=preview)
* [Additional Usage Examples](./VirtualClient.Documentation/UsageScenarios.md)  

## Team Contacts
* [virtualclient@microsoft.com](mailto:virtualclient@microsoft.com)

## [Getting Started](https://microsoft.github.io/VirtualClient/docs/guides/getting-started/)

## [Supported Workloads](https://microsoft.github.io/VirtualClient/docs/overview/#supported-benchmark-workloads)

---
## Please bear with us for a second
VirtualClient is an MSFT inner source project we are migrating to GitHub. We are still gradually migrating our code and documents.
Please be patient if the documents have wrong links. We are actively cleaning those up.




## Telemetry Notice
Data Collection. 

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

#### VirtualClient does not collect your data by default
VirtualClient does not collect any of your benchmark data and upload to Microsoft. When benchmarking at scale, and leveraging VC's telemetry capabilities, users need to explicitly provide a connection string, that points to a user-owned Azure Data Explorer cluster. VirtualClient does host a Azure storage account to host the benchmark binaries or source. The only information VirtualClient team could infer from usage, is the download traces from Azure storage account.

#### About benchmark examples in source
VirtualClient has example benchmark outputs in source, for unit-testing purpose, to make sure our parsers work correctly.
Those runs might or might not be ran on Azure VMs. The results have also been randomly scrubbed. These examples do not represent Azure VM performance. They are in the source purely for unit testing purposes.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
