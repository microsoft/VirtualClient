---
sidebar_position: 45
---

# Build a CI / CD pipeline


## Pull Request
Every pull request need to pass PR build.

[![Pull Request Build](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml)

GitHub Action: https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml


## Document build and publish

VirtualClient uses [Docusaurus](https://docusaurus.io/) to host front page and documents.

Every main branch check-in will trigger a document yarn build and publish to gh-pages branch.
[![Document Build](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml/badge.svg?branch=main)](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml)
- GitHub Action: https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml

This action actually deploys gh-pages branch to GitHub page server.
[![Document Deployment](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment)

## NuGet build
Azure Pipeline:

Unfortunately this Azure pipeline will not be public, because it involves Microsoft signing processes. That is also why our pull requests will always require one Microsoft employee to sign off.

[![NuGet Release Status](https://msazure.visualstudio.com/One/_apis/build/status/OneBranch/CRC-AIR-Workloads/microsoft.VirtualClient?branchName=main)](https://msazure.visualstudio.com/One/_build/latest?definitionId=297462&branchName=main)