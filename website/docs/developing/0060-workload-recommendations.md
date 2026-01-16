# Workload Selection Recommendations
The following documentation provides guidance on the practices and fundamentals to consider when selecting (or creating) workload software
for the purpose of qualifying hardware systems. Good workloads are easier to onboard into the Virtual Client platform and offer better return
on investment for the work involved. There are thousands of options available, but not all of them are as well-designed. This simple guide will help.

## Signs of a Good Workload
The following sections describes some of the aspects of good options for workload software. At a high-level, a good workload:

* #### Runs Consistently Reliable Tests
  The workload tests can be run consistently reliably. Tests that cannot consistently produce results time and time again produce 
  too much variation (noise) in the outcomes to be used to accurately gauge the state of a system under test. This is about code quality and defensive
  programming in many ways.

* #### Runs Consistently Repeatable Tests
  Workload test results should be consistently repeatable. This means that the same tests run on the same system
  over and over should produce similar results given nothing has changed on the system under test. This is not to say that the results will be identical
  but that the standard deviation of the results would be generally small given all other things being equal on the system.

* #### Produces Objective Results
  The workload tests should produce definitive and objective results. The results should not make estimations as a general rule but should
  instead attempt to measure facets of the system that are based on hardened and vetted patterns, practices and software. For example, on a Windows system
  the performance counter and ETW sub-systems are highly refined and reliable. A workload can confidently rely upon performance counters to measure many performance
  characteristics of the system in a precise manner.

* #### Produces Structured Results
  Workload results produced should be in a structured format. This makes it easy for the results to be parsed for meaningful
  insights and aggregations.

* #### Is Easy to Integrate with Other Systems
  Good workloads are generally easy to use thus making it easy to integrate into the larger execution system. This reduces the overhead required for
  teams to onboard new and valuable test scenarios.

## Additional Considerations
The following section offers a few other recommendations when selecting a workload for easy integrated into the Virtual Client.

* #### Simple Command Line Tools are Preferred
  All platforms support executables and there is a lot of of pre-existing OS/systems integration support. Command line executables can typically be compiled 
  to support multiple OS platforms (Windows vs. Linux) as well as CPU architectures. Command line executables are easy to port around (e.g. copy, download etc...).
  Workloads that require GUI support should be generally avoided as they introduce significant challenges for automation. They are often difficult to 
  integrate into automation workflows and may not even run in certain server OS environments.

* #### Written in a Cross-Platform Programming/Scripting Language
  A good workload can run on different operating systems (e.g. Linux, Windows). This ensures versatility and coverage for a wider range of customer scenarios.

* #### The Workload Application is Parameterized
  In order to ensure that the workload can support all necessary scenarios and configuration requirements, the workload should allow all important
  settings to be provided (ideally on the command line).

* #### The Workload Application Produces a Consistent Steady-State
  As mentioned above, the workload should produce consistent and reliable results. The best workloads run a "steady-state" test. A steady state
  test will run the exact same execution workflow each time given the same settings are provided. The tests should not generally be based on algorithms that cannot produce
  a steady state time and time again. Tests should also avoid testing aspects of the system that are themselves highly variable. Highly variable algorithms or aspects
  will inevitably produce highly variable results. These results will make it difficult to ascertain differences in the system performance when changes
  are made to the system under test.

* #### Runs the Same Test Every Time Given Exact Settings
  A workload should run the exact same test every time given a set of specific settings. The tests ran should vary only when the settings supplied on the 
  command line have changed.

* #### The Workload Application Should be Versioned
  This somewhat goes without saying, but the workload software should employ a proper versioning system.