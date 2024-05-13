# Contributing

Welcome and thank you for your interest in contributing to **Virtual Client** projects! Before contributing to this project, please 
review this document for policies and procedures which will ease the contribution and review process for everyone. 
If you have questions, please contact **virtualclient@microsoft.com**.

## Before Starting
Ensure that you have attended to the preliminaries for the repo and have an understanding of how to build and test your
changes to the source code.

* [Repo Fundamentals](README.md)

## Style Guidelines
Most of the coding style is enforced using static code/style analysis toolsets that are integrated into the project when built.
This makes it easier for developers to follow the patterns. The important thing to remember here is that new projects must import
the build environment settings. You can see examples of the import at the bottom of any project (e.g. csproj) file in the
repo.

As a general rule, if it builds, the majority of your style requirements are already completed! :)

## Test Guidelines
Any changes or additions to source code in this repo carry the requirement at a minimum that unit tests be written to verify
important behaviors. The team does not typically accept changes to source code in this repo without having proper unit tests.
Take the time to look at existing unit tests within the project. A lot of effort was made to create unit tests that are clear,
clean and effective. Additionally, there are a lot of good patterns in place to help other developers quickly write a set of 
robust unit tests for code in this repo.

Review our documentation on testing if you would like to learn more on our practices and philosophies around testing.

## Pull Request(PR) Process
The following steps will help you get your changes in for review by the team. Review the repo README if you need a reminder
or examples on how to build and test code within the repo.

### Creating/Submitting Changes
* Create a fork/branch for your changes (e.g. users/alias/ChangeDescription). We do not allow users to push changes directly to the master branch.
* Ensure all solutions/projects within the repo build successfully before submitting your PR (see README noted above).
* Ensure all tests within the repo pass before submitting your PR. Passing unit and functional tests is a gate to complete the PR.
* Update any documentation, user and contributor, that is impacted by your changes.
  * Check for markdown (e.g. README.md) files within the solution or project directory.
  * Team members will typically help you with pointers to documentation needing update as part of the pull request process.
* Push your changes to the remote.
* Start a pull request for the team to review.

### Getting Approvals
After you've created the pull request, the following requirements must be met before you will be able to complete the pull request:
* An automated PR build must complete successfully.
* Team reviewers must review the code and provide at least 1 approval.
  * Any feedback/comments provided by reviewers must be resolved and changes committed + pushed to the remote.
* Once approved, your changes will be merged with the "main" branch.
