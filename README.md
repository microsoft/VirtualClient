# <img src="./website/static/img/vc-logo.svg" width="50"> Virtual Client


[![Pull Request Builds](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml)
[![Documentation Builds](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml/badge.svg?branch=main)](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml)
[![Documentation Deployment Builds](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment)

------

The following links provide additional information on the Virtual Client project.

* [Overview](https://microsoft.github.io/VirtualClient/docs/overview/)
* [Getting Started + How to Build](https://microsoft.github.io/VirtualClient/docs/guides/getting-started)

## [Getting Started](https://microsoft.github.io/VirtualClient/docs/guides/getting-started/)

You can follow along with the ["getting started" tutorial](https://microsoft.github.io/VirtualClient/docs/guides/getting-started/) running a quick workload (OpenSSL Speed - SHA256) on your
local system.

## Contributing

We welcome your contribution, and there are a number of ways to contribute to the Virtual Client project:

* [Make a Feature Request](https://github.com/microsoft/VirtualClient/issues/new/choose).
* [Submit bugs](https://github.com/microsoft/VirtualClient/issues/new/choose). We apologize for anything missed and will investigate it ASAP.
* [Review source code changes](https://github.com/microsoft/VirtualClient/pulls).
* [Review the documentation](https://github.com/microsoft/VirtualClient/tree/main/website/docs).
* [Fork and Contribute Directly](./CONTRIBUTING.md). We welcome you to directly work in the codebase. We offer a [developer guide](https://microsoft.github.io/VirtualClient/docs/category/developing/). Please contact us if you have any questions.
* [Provide Feedback to the Team](https://github.com/microsoft/VirtualClient/discussions/categories/show-and-tell).

Thank you and we look forward to your contribution.

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


## Telemetry Notice
Data Collection. 

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

### VirtualClient Does Not Collect Your Data by Default
VirtualClient does not collect any of your benchmark data and upload to Microsoft. When benchmarking at scale, and leveraging VC's telemetry capabilities, users need to explicitly provide a connection string, that points to a user-owned Azure Data Explorer cluster. VirtualClient does host a Azure storage account to host the benchmark binaries or source. The only information VirtualClient team could infer from usage, is the download traces from Azure storage account.

### About Benchmark Examples in Source
VirtualClient has example benchmark outputs in source, for unit-testing purpose, to make sure our parsers work correctly.
Those runs might or might not be ran on Azure VMs. The results have also been randomly scrubbed. These examples do not represent Azure VM performance. They are in the source purely for unit testing purposes.


## Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.