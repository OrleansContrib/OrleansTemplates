# Code Generation Documentation
## Overview
In **Orleans Templates**, we generate code in both the *Grains* and the *Grain Interfaces* projects.
The code is generated as part of the build process. In fact, right before a project is compiled, the code generation tasks are invoked and new code is added. Because the code is generated before the compilation, the generated code is also compiled and added to Intellisense.
To invoke the code generation tasks before the build, we add a BeforeBuild target (see [How to: Extend the Visual Studio Build Process](https://msdn.microsoft.com/en-us/library/ms366724.aspx)) to the .csproj files. Here is what it looks like:

* Grain Interfaces Project
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <UsingTask TaskName="ETG.Orleans.CodeGen.Tasks.GenerateApiControllersTask" AssemblyFile="$(MSBuildThisFileDirectory)..\tools\ETG.Orleans.CodeGen.dll" />
  <UsingTask TaskName="ETG.Orleans.CodeGen.Tasks.GenerateSwmrInterfacesTask" AssemblyFile="$(MSBuildThisFileDirectory)..\tools\ETG.Orleans.CodeGen.dll" />
  <Target Name="BeforeBuild">
    <GenerateApiControllersTask ProjectPath="$(ProjectDir)$(AssemblyName).csproj" OutputPath="$(ProjectDir)Properties\etg.orleans.apicontrollers.cs" />
    <GenerateSwmrInterfacesTask ProjectPath="$(ProjectDir)$(AssemblyName).csproj" OutputPath="$(ProjectDir)Properties\etg.orleans.swmrinterfaces.cs" />
  </Target>
</Project>
```
* Grains Project
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <UsingTask TaskName="ETG.Orleans.CodeGen.Tasks.GenerateSwmrGrainsTask" AssemblyFile="$(MSBuildThisFileDirectory)..\tools\ETG.Orleans.CodeGen.dll" />
  <Target Name="BeforeBuild">
    <GenerateSwmrGrainsTask ProjectPath="$(ProjectDir)$(AssemblyName).csproj" OutputPath="$(ProjectDir)Properties\etg.orleans.swmrgrains.cs" />
  </Target>
</Project>
```

All code generation *Tasks* are part of the **ETG.Orleans.CodeGen** assembly which is added to the **ETG Orleans Solution Template** using a NuGet package. These two tasks are described in the following.

## Generate Api Controllers Task
The **GenerateApiControllerTask** is invoked during the build (before compilation) of the *Grain Interfaces* project. This task does the following:
* Parse all the code in the *Grain Interfaces* project.
* For each grain interface annotated with the `ETG.Orleans.Attributes.ApiControllerAttribute`
 * Generate a corresponding Api Controller class that inherits from `System.Web.Http.ApiController`.
 * Generate a corresponding `RoutePrefix` and place it on the generated class.
 * For each method declared in the Grain Interface
   * Generate a corresponding method in the Api Controller and add an implementation that delegates the call to the Grain.
   * Copy the routing attributes from the *Grain Interface* method to the generated method.
   * Add a `string id` parameter to the list of parameters of the generated method. The `id` is used to identify the *Orleans Grain* that should be invoked.

For example, if the *Grain Interface* contains the following:
```csharp
    [ApiController(RoutePrefix = "api/prefs")]
    public interface IPrefsGrain : IGrainWithStringKey
    {
        [Route("new/{id}")]
        [HttpPost]
        Task SetValue([FromBody] KeyValuePair<string, string> entry);
        
        [Route("value/{id}/{key}")]
        Task<string> GetValue(string key);
    }
```
The generated Api Controller would be:
```csharp
    [RoutePrefix("api/prefs")]
    public partial class PrefsGrainController : ApiController
    {
        [Route("new/{id}")]
        [HttpPost]
        public async Task SetValue([FromBody] KeyValuePair<string, string> entry, string id)
        {
            var grain = GrainFactory.GetGrain<IPrefsGrain>(id);
            await grain.SetValue(entry);
        }

        [Route("value/{id}/{key}")]
        public Task<string> GetValue(string key, string id)
        {
            var grain = GrainFactory.GetGrain<IPrefsGrain>(id);
            return grain.GetValue(key);
        }
    }
```

## Single Writer Multiple Readers (SWMR)
The Single Writer Multiple Readers pattern is implemented in two tasks: **GenerateSwmrInterfacesTask** (part of the grain interfaces project) and **GenerateSwmrGrainsTask** (part of the grain classes project). Like the **GenerateApiControllerTask **, these tasks are invoked during the build (by the *BeforeBuild* target).

The user Grain Interface should be annotated with the `SingleWriterMultipleReaders` and `ReadOnly` attributes as follows. In addition, we require client code to provide a `Task<IGrainState> GetState()` method as part of the SWMR grain.
```csharp
    [SingleWriterMultipleReaders(ReadReplicaCount = 10)]
    public interface IHelloGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<string> ReadSomething();

        Task WriteSomething(string something);
        
        // must be added to Single Writer Multiple Readers grains (do not mark it as readonly).
        Task<IGrainState> GetState();
    }
```

**GenerateSwmrInterfacesTask** creates `IHelloGrainReader`, `IHelloGrainWriter` and `IHelloGrainReadReplica`. The `IHelloGrainReader` exposes the read interface and the `IHelloGrainWriter` exposes the write interface. The `IHelloGrainReadReplica` is used internally to create read replicas for a given grain. To benefit from the SWMR scaling, client code must use the generated `IHelloGrainReader` and `IHelloGrainWriter` instead of the `IHelloGrain` directly.

The `IHelloGrainReader`, `IHelloGrainWriter` and `IHelloGrainReadReplica` are as follows:

```csharp
    public interface IHelloGrainReader
    {
        Task<string> ReadSomething(string sessionId);
    }

    public interface IHelloGrainWriter
    {
        Task WriteSomething(string sessionId, string something);
    }
    
    public interface IHelloGrainReadReplica
    {
        Task<string> ReadSomething(string sessionId);
    }    
```

The `IHelloGrainReader` is a stateless worker that simply forwards read calls to read replicas. It uses a topology (consistent hash) to distribute the load among read replicas. Because it's a stateless worker, it automatically scales. However, the maximum number of read requests that can be executed in parallel is bound by the number of read replicas. When an `IHelloGrainReader` receives a read request, it delegates the request to one of the `IHelloGrainReadReplica`'s. The `IHelloGrainReader` uses the sessionId and a consistent hash to determine the identity of the read replica that will handle a given read request. The mapping between a sessionId and a read replica is static.

The `IHelloGrainWriter` is a normal Grain (not a stateless worker). When a `IHelloGrainWriter` receives a write request, it forwards the request to the `IHelloGrain` (there is only one replica of `IHelloGrain` for a given grain id). Then, `IHelloGrainWriter` fetches the State of `IHelloGrain` and sends a copy of the State to all read replicas. To increase performance, the `IHelloGrainWriter` does not wait until the State is received by all read replicas but only until the state is received by the read replica associated with the current sessionId. The aim is to ensure consistency whithin a session (for a given sessionId) while preserving performance.

The `IHelloGrainReadReplica` is a copy of the read-only interface and implementation of `IHelloGrain`. It keeps a copy of the State of `IHelloGrain` in memory (not persisted) and uses this State to serve read requests (much like `IHelloGrain`). Because the state of the `IHelloGrainReadReplica` is not persisted, the `IHelloGrainReadReplica` also need to fetch the current `State` from `IHelloGrain` on activation. Subsequent updates to the State are sent to `IHelloGrainReadReplica` from `IHelloGrainWriter`.

The topology is immutable (until we implement auto scaling) and each activation of `IHelloGrainReader` and `IHelloGrainWriter` has a copy of it.

We did some initial testing and the speed-up was almost linear for randomly generated session ids. However, it's important to keep in mind that increasing the number of replicas increases memory and CPU usage (every write request must update all read replicas) and thus should not be abused. In addition, increasing the number of replicas beyond hardware capacity will introduce unnecessary overhead.

## Code Generation
The code generation is done using **Roslyn**. The best way to understand it is to read the source code of the **ETG.Orleans.CodeGen** project.

