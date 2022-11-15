---
id: blob-storage
sidebar_position: 7
---

# Blob storage support
Virtual Client supports a few different types of Azure Blob stores that can be used as part of a workload profile execution. This documentation
covers how to supply those blob stores to the Virtual Client on the command line as well as how to use them in Virtual Client codebase.
  
### Supported Blob Stores
The following stores are supported by the Virtual Client. The stores must be Azure Storage Account blob stores.

* **Packages Store**  
  The packages blob store contains workload and dependency packages that must be downloaded to a system during the execution of a
  workload profile. These are typically NuGet/zip files that contain binaries, scripts, etc... that are required by the scenario profile.

  The packages store can be supplied to the Virtual Client on the command line using the '--packageStore' parameter and supplying the
  full connection string to the Azure storage account blob resource.

  ```
  Example:
  VirtualClient.exe --profile=PERF-NETWORK.json --timeout=1440 --packageStore={ConnectionString or SASTokenUri}
  ```

   Note that Virtual Client expects the packages to exist in the blob store in a container called 'packages'.



  ![packages store](./img/blob-storage-support-1.png)


* **Content Store**  
  The content blob store is used for uploading files and content captured by workloads or monitors that run as part of Virtual Client workload
  operations. For example, a monitor might be implemented to upload certain types of logs, bin files or cab files to the specified content blob
  store.

  ```
  Example:
  VirtualClient.exe --profile=PERF-NETWORK.json --timeout=1440 --contentStore={ConnectionString or SASTokenUri}
  ```



  ![monitoring content](./img/blob-storage-support-2.png)


---------

### Blob Store Authentication
Virtual Client supports the following authentication options for all blob stores:

[Shared Access Signatures (SAS) Overview](https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview)  
[Account Shared Access Signatures](https://docs.microsoft.com/en-us/rest/api/storageservices/create-account-sas?redirectedfrom=MSDN)

  * **Storage Account Connection String**  
    The primary or secondary connection string to the Azure storage account. This provides full access privileges to the entire
    storage account but the least amount of security. This is generally recommended only for testing scenarios. The use of a
    SAS URI or connection string is preferred because it enables finer grained control of the exact resources within the storage
    account that the application should be able to access.<br/><br/>
    ```(e.g. DefaultEndpointsProtocol=https;AccountName=anystorageaccount;AccountKey=w7Q+BxLw...;EndpointSuffix=core.windows.net)```

  * **Blob Service Connection String**  
    This is a connection string to the Blob service in the storage account. It allows user-defined/restricted access privileges to be defined for
    all containers and blobs in the storage account. This is a good fit for scenarios where content (e.g. from different monitors) is uploaded to 
    different containers within the blob store and thus the application needs access to all containers.<br/><br/>
    ```(e.g. BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...)```

  * **Blob Service SAS URI**  
    This is a SAS URI to the Blob service in the storage account. This provides exactly the same types of privileges as the Blob service-level connection string noted above.<br/><br/>
    ```(e.g. https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...)```

  * **Blob Container SAS URI**  
    This is a SAS URI to a single blob container within the storage account. This is the most restrictive way of providing privileges but is also the most secure because it
    provides the least amount of access to the application. This is a good fit for scenarios where all content (e.g. across all monitors) is uploaded to a single container 
    within the blob store.<br/><br/>
    ```(e.g. https://anystorageaccount.blob.core.windows.net/packages?sp=r&st=2021-11-23T18:22:49Z&se=2021-11-24T02:22:49Z&spr=https&sv=2020-08-04&sr=c&sig=ndyPRH...)```

Use the following recommendations when creating shared access keys in the blob store to ensure the right amount of privileges
are granted to the Virtual Client application for uploading and downloading blobs.
* **Blob Service Connection Strings or SAS URIs**  
  Select the following options when defining shared access signatures at the Blob service level:
  * Allowed services = Blob
  * Allowed resource types = Container, Object
  * Allowed permissions = Read, Write, Create
  * Allowed protocols = HTTPS only
  

  ![](./img/blob-service-sas-1.png)


* **Blob Container SAS URIs**  
  Select the following options when defining shared access signatures at the Blob container level:
  * Signing method = Account key
  * Permissions = Read, Write, Create
  * Allowed protocols = HTTPS only
  


  ![](./img/blob-container-sas-1.png)


---------

### Blob Store Folder/File Naming Conventions
In  order to ensure that files associated with Virtual Client executors or monitors are easy to find in the blob stores, a naming convention is used.
At a high level, all files associated with a given experiment are contained together in the blob store. The following pieces of information are included
in the virtual path and file name that is used to ensure that files associated with that experiment are organized in the blob store:

* **Experiment ID**  
  The ID of the experiment is used to ensure all files associated with any executors or monitors are in the same virtual folder within the blob store.
  Furthermore because an experiment ID is a global identifier, it is easier for the user or automation to find files associated with a given experiment
  (e.g. for debugging/triage).

* **Agent ID**  
  The ID of the Virtual Client instance running (i.e. the agent) is also included. This ensures that files captured from a specific VM or node etc... are easy
  to distinguish from each other.

* **Component Name**  
  This is the name of the executor or monitor that created the files (directly or indirectly).

* **File Name**  
  This is the name of the file as it should be reflected in the blob store. A timestamp (round-trip date/time format) will be added to the beginning of the file name
  (e.g. 2022-03-07T01:32:27.1237655Z-monitor.log). The addition of the timestamp ensures that the files within a given virtual folder in the blob store will all
  have unique names in order to avoid collisions. Round-trip date/time formats in addition to being a valid timestamp are naturally sortable in UX experiences
  such as a web browser.

* **Round-Trip Formatted Timestamp**  
  As noted above, a round-trip formatted timestamp will be added to each file name to ensure uniqueness.

Given the pieces of information noted above, the format for virtual paths and file names will look like:


``` csharp
// Path Format:
// -----------------------------------------------
{ExperimentID}/{AgentID}/{ComponentName}/{RoundTripFormattedTimestamp}-{FileName}

// Examples:
// -----------------------------------------------
// Given the Following Information:
// - Experiment ID = 24149a49-66c9-4bd1-9332-18370c7c70e1
// - Agent ID = cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01
// - A Background Monitor = ExampleMonitor
// - Files Produced = monitor.log
//
// The blob virtual folder paths/names would like the following:
24149a49-66c9-4bd1-9332-18370c7c70e1/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/examplemonitor/2022-03-07T01:32:27.1237655Z-monitor.log
24149a49-66c9-4bd1-9332-18370c7c70e1/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/examplemonitor/2022-03-07T01:34:32.6483092Z-monitor.log
24149a49-66c9-4bd1-9332-18370c7c70e1/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/examplemonitor/2022-03-07T01:36:30.2645013Z-monitor.log

// Structure Within the Blob Store:
// -----------------------------------------------
// (container) monitors
//   -> (virtual folder) /24149a49-66c9-4bd1-9332-18370c7c70e1 
//      -> (virtual folder) /cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01
//          -> (virtual folder) /examplemonitor
//             -> (blob) 2022-03-07T01:32:27.1237655Z-monitor.log
//             -> (blob) 2022-03-07T01:34:32.6483092Z-monitor.log
//             -> (blob) 2022-03-07T01:36:30.2645013Z-monitor.log
```

---------

### Integration with the Development Process
This section describes how access to the supported blob stores can be integrated into the Virtual Client process.

##### How Blob Stores are Integrated into Dependencies
When the Virtual Client reads the blob stores (i.e. connection strings) from the command line, it will place a set of
BlobStore objects into the dependencies collection that is passed into the constructors of ALL profile actions, dependencies
and monitors.

``` csharp
// In the CommandBase class in the Main project
List<BlobStore> blobStores = new List<BlobStore>();
if (this.ContentBlobStore != null)
{
    blobStores.Add(this.ContentBlobStore);
}

if (this.PackagesBlobStore != null)
{
    blobStores.Add(this.PackagesBlobStore);
}

IServiceCollection dependencies = new ServiceCollection();
...
dependencies.AddSingleton<IEnumerable<BlobStore>>(blobStores);
```

Each of the BlobStore instances describes a single blob store that can be then used within any of the components described
in the profile. To use upload or download blobs from one of the stores, create a BlobManager instance. The blob manager has
simple download and upload methods available.

[Example Implementation/Usage](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Monitors/ExampleMonitorWithBlobUploadIntegration.cs)

``` csharp
// Using the example implementation noted above:
//
// Get the blob store reference and create a BlobManager to upload blobs to the store.
if (this.TryGetContentStore(out BlobStore contentStore))
{
    DateTime snapshotTime = DateTime.UtcNow;
    IBlobManager blobManager = new BlobManager(contentStore.ConnectionString);

    // For this example, assume there is a file that contains the output of some monitoring
    // toolset. We want to upload this file to the content blob store.
    using (FileStream uploadStream = new FileStream(resultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
        BlobDescriptor resultsBlob = new BlobDescriptor
        {
            // For this scenario, we have a folder structure and naming convention for each of the blobs
            // that is specific to the monitor but that also allows for consecutive blobs to be uploaded without
            // overwriting previously uploaded blobs.
            //
            // Example:
            // Given an experiment ID = 24149a49-66c9-4bd1-9332-18370c7c70e1 and an Agent ID = cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01:
            //
            // Format = 24149a49-66c9-4bd1-9332-18370c7c70e1/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/examplemonitor/2022-03-07T01:32:27.1237655Z-monitor.log
            BlobName = BlobDescriptor.ToPath(this.ExperimentId, this.AgentId, "examplemonitor", "monitor.log"),
            ContainerName = "monitors",
            ContentEncoding = Encoding.UTF8,
            ContentType = "text/plain"
        };

        await blobManager.UploadBlobAsync(resultsBlob, uploadStream, cancellationToken)
            .ConfigureAwait(false);
    }
}
```
