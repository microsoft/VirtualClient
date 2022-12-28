# Error Handling Recommendations
The following sections describe important details and recommendations for handling errors/exceptions in the operations of the
Virtual Client runtime. Error handling is particularly important for the quality of a runtime platform where there are many moving
parts.

## VirtualClientException
The Virtual Client platform/core contains a base class that is used for most exceptions that are explicitly raised/thrown in the application
named **VirtualClientException**. This exception class derives from the .NET framework 'Exception' class and allows the developer to communicate
not only a description of the error but also a structured "reason" for the error. The profile execution logic makes determinations on the severity
of exceptions that are raised/thrown based on the data type of the exception as well as the error reason that is supplied.

* [Custom Platform/Core Exceptions](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/Exceptions.cs)  
* [Error Reasons](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/Enumerations.cs)  
* [Profile Execution Logic](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Core/ProfileExecutor.cs)



``` csharp
// Custom exception classes in the Virtual Client platform/core derive from the base VirtualClientException class. Each of the
// platform/core exception classes implement the same constructors for consisteny. The 'ErrorReason' provided is an important distinction.
public class WorkloadException : VirtualClientException
{
    public WorkloadException()
        : base()
    {
    }

    public WorkloadException(string message)
        : base(message)
    {
    }

    public WorkloadException(string message, ErrorReason reason)
        : base(message, reason)
    {
    }

    public WorkloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public WorkloadException(string message, Exception innerException, ErrorReason reason)
        : base(message, innerException, reason)
    {
    }
}

// Errors raised during the operations of the hosted REST API service.
throw new ApiException();

// Errors raised during the operations of a dependency installer/handler.
throw new DependencyException();

// Errors raised during the operations of a monitor.
throw new MonitorException();

// Errors raised during the operations of a workload/test.
throw new WorkloadException();

// Errors raised during the operations of parsing the results of a workload/test.
throw new WorkloadResultsException();
```


## Exception Handling
Based upon the data type of the exception thrown as well as the 'ErrorReason' (if it is one of the custom platform/core exceptions), the profile execution
logic makes a decision on whether to handle it or let the Virtual Client crash. Thus error handling in the Virtual Client is a fair amount methodical and there
are recommendations as to how exceptions should be handled and thrown/raised. The type and severity of exceptions that are raised are at the discretion of the
developer that implements them. The specifics of the nature of errors can only be known in the context of the component a developer implements. The developer
can communicate error handling intent to the profile execution that is relevant. There are recommendations in sections that follow.

The following sections explain some of the methodology around exception handling in the Virtual Client.

* **Exceptions that are not explicitly handled by the profile execution logic result in a crash**  
  The profile execution logic handles a few different exception types. However, if an exception is not a part of that explicit list of exceptions, the Virtual Client
  runtime application will crash.

* **VirtualClientException instances with error reasons from 100 - 399 are considered transient**  
  Exceptions with an ErrorReason value from 100 - 399 are considered transient and will be handled. Transient errors are those in which the
  developer has indicated the error will very likely go away within a period of time and thus the runtime operations will begin succeeding as expected. Exceptions
  of this type will not crash the Virtual Client. The profile execution logic will continue onward. For example, if a particular workload execution action in a
  profile happened to fail, the profile execution logic would continue on to the next action.

* **VirtualClientException instances with error reasons from 400 - 499 represent critical failures that cannot be confirmed as transient or not**  
  Exceptions with an ErrorReason value from 400 - 499 represent highly disruptive failures to the operations of the Virtual Client. However, they may or may not be 
  transient. That determination is not clear. It is this distinction (the fact that we cannot know if it is transient) why the profile execution logic generally handles 
  exceptions of this type and continue onward.

* **VirtualClientException instances with error reasons >= 500 represent terminal/permanent failures**  
  Exceptions with an ErrorReason value greater than or equal to 500 represent terminal failures. These are failures where it is generally know that the Virtual Client
  could never recover from them. For example the user supplied the name of a profile on the command line that does not exist. In this case, the profile file does not exist
  and thus the Virtual Client could never know what it was supposed to do. Exceptions of this type will crash the Virtual Client.

## Exception Handling Recommendations
The following sections document recommendations for the developer to follow when implementing new components in the Virtual Client codebase.

* **Explicitly handle known exceptions**  
  As a general rule, all known exceptions should be handled and dealt with appropriately. The developer should make a decision depending upon the type of exception
  whether it should then be propagated further. Exceptions that are not handled in an individual component will almost always crash the Virtual Client application.
  This is by design.

* **Retry on transient errors**  
  For many cases of transient errors, a simple set of retries is enough to recover. Retries on transient error situations can be one of the best ways to build
  reliability into an application. The Virtual Client codebase leverages an open source .NET framework called 'Polly' to implement retry handling logic in
  a consistent manner. The framework is already a part of the Virtual Client platform/core projects and libraries.

  * [Polly Transient Fault Handling Library](http://www.thepollyproject.org/)

* **Always provide good context-specific information in exceptions raised/thrown**  
  Regardless of the quality of a software application, errors will always occur given time and certain scenarios. Error handling is always a challenging thing to
  do well in any application; however, one of the key most important things to ensure is that the information provided in an error/exception is clear. Few things
  are worse than having to figure out why an application is failing and having very little information by which to do so. When developing components in the Virtual
  Client codebase, take the time to provide good information in error messages. Provide any amount of context information that is available in the code at the point
  of throwing the exception that would help another person (including users and folks that do not program) understand what has happened. Sometimes a quality error enables
  a user to course-correct. Otherwise, the user will likely be reaching out to the team and making it the someone on your team's problem instead. Lots of time is
  wasted in this way.

  ``` csharp
  try
  {
      ...
  }
  catch (FileNotFoundException exc)
  {
      // This is an example of an error which is not very good. What file was not found?!! There will likely be more
      // than 1 file involved in running a particular Virtual Client profile. Give the user some pertinent information
      // about the situation so that they might be able to do something intelligent about it.
      throw new WorkloadException("The file was not found.", exc);

      // This is an example of better error information. The user might be able to address this on his/her own without need
      // for extra assistance. That is more satisfying for the user (who can self-help) as well as for the team.
      throw new WorkloadException(
          $"The expected script file '{scriptFile}' was not found in the workload package with the name '{this.PackageName}'. " +
          $"This script is used to parse the custom results from the workload.",
          exc);
  }
  ```

* **Define an appropriate 'ErrorReason' when raising/throwing exceptions that indicate transient errors**  
  If an exception is caught within a given component that is deemed to be transient (despite any retries attempted), the exception thrown should be one of the
  types that derive from the 'VirtualClientException' class. Additionally an appropriate 'ErrorReason' should be supplied indicating the category and severity of
  the error so that the profile execution logic can itself handle it appropriately. Transient error reasons are in the range of 100 - 399. If an appropriate reason
  does not already exist on the 'ErrorReason' enumeration, create one.

  ``` csharp
  try
  {
      ...
  }
  catch (FileNotFoundException exc)
  {
      // ErrorReason.WorkloadResultsNotFound = 314

      throw new WorkloadException(
        $"The '{workloadName}' workload experienced an issue writing the results file to the system.",
        exc,
        ErrorReason.WorkloadResultsNotFound);
  }
  ```

* **Define an appropriate 'ErrorReason' when raising/throwing exceptions that indicate terminal/permanent errors**  
  If an exception is caught within a given component that is deemed to be transient (despite any retries attempted), the exception thrown should be one of the
  types that derive from the 'VirtualClientException' class. Additionally an appropriate 'ErrorReason' should be supplied indicating the category and severity of
  the error so that the profile execution logic can itself handle it appropriately. Transient error reasons are in the range of 100 - 399. If an appropriate reason
  does not already exist on the 'ErrorReason' enumeration, create one.

  ``` csharp
  try
  {
      ...
  }
  catch (FileNotFoundException exc)
  {
      // ErrorReason.ProfileNotFound = 500

      throw new DependencyException(
        $"A profile '{profileName}' does not exist on the file system by this name or path provided.",
        exc,
        ErrorReason.ProfileNotFound);
  }
  ```

* **Always include the original exception when raising/throwing another/custom type of exception**  
  If an exception is caught within a given component and the developer chooses to propagate it by throwing an exception of a different
  type, the original exception should be included in the new exception instance.

  ``` csharp
  try
  {
      // Some logic here that is looking for a specific file on the file system.
      string script = this.systemManagement.FileSystem.File.ReadAllText(filePath);
  }
  catch (FileNotFoundException exc)
  {
      throw new WorkloadException(
          $"The expected workload script '{filePath}' does not exist on the file system.",
          exc,
          ErrorReason.DependencyNotFound);
  }
  ```