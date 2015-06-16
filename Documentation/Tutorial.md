# Tutorial

* Open visual studio **as administrator** and create a new solution using **File->New->Project->Visual C#-> ETG Orleans** Solution.
* Enter your solution name and hit OK.
* Set your Host project as a startup project.
* Hit **F5** to start the Orleans Silo and the web API container (If you get a permission exception, make sure your visual studio has been started as administrator). The console output should look like the following.
```
Successfully started Orleans silo 'MyComputerName' as a Primary node.
Orleans Silo is running.
Started web host at: http://MyComputerName:81
Press ENTER to terminate.
```

At this point, you have a Silo and a web host running. You can interact with your grains using a Web client (if the grain have a corresponding web ApiController). 

## ApiControllerAttribute
There are two ways you can add an ApiController to your grains:

1. In the WebApis project, create a new Controller that inherits from System.Web.Http.ApiController and add the corresponding Web Api to it.
2. Add your Web Api routing attributes directly on the Grain Interface (as shown below) and the Api Controller implementation will be automatically generated for you.
```csharp
namespace GrainInterfaces
{
    [ApiController(RoutePrefix = "api/prefs")]
    public interface IPrefsGrain : IGrainWithStringKey
    {
        [Route("new/{id}")]
        [HttpPost]
        Task SetValue([FromBody] KeyValuePair<string, string> entry);

        [Route("value/{id}/{key}")]
        Task<string> GetValue(string key);

        [Route("entries/{id}")]
        [HttpGet]
        Task<IDictionary<string, string>> GetAllEntries();

        [Route("clear/{id}")]
        [HttpDelete]
        Task ClearValues();
    }
}
```
Notes: 
* You must place the **ETG.Orleans.Attributes.ApiControllerAttribute** on the Grain Interface in order to get the Api Controller implementation generated. 
* The generated Api Controller will be located in **Properties\etg.orleans.apicontrollers.cs** file of the Grain Interfaces project.
* We only support grains with a `string` key (i.e. `IGrainWithStringKey`) at this point. 

## Single Writer Multiple Readers Attribute
The `SingleWriterMultipleReaders` attribute addresses the use case of a grain that is subject to a high read access (multiple concurrent readers) and low write access. In such case, read requests can be executed in **parallel** rather than serially; which allows the grain to handle higher load of read requests and to be more responsive.

The `SingleWriterMultipleReaders` attribute can be placed on a Grain Interface as shown below.
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

The `ReadReplicaCount` is the number of read replicas that will be created. For this to work, *read methods* must be marked with the `ReadOnly` attribute (as shown above).

For each grain interface marked with `SingleWriterMultipleReaders`, OrleansTemplates will generate **read** and **write** interfaces that **must be used instead** of the original grain (the grain will not scale otherwise). The *read* and *write* grain interfaces are as follows:

```csharp
    public interface IHelloGrainReader : IGrainWithStringKey
    {
        Task<string> ReadSomething(string sessionId);
    }

    public interface IHelloGrainWriter : IGrainWithStringKey
    {
        Task WriteSomething(string something, string sessionId);
    }
```

To ensure consistency, we require client code to pass in a **session id** (a given session id is bound to one read replica). Whereas read requests with the same session id are executed serially (one after the other), read requests with different session ids are executed in parallel (if there are enough read replicas).

Client code will look as follows:
```csharp
// issue a read request:
IHelloGrainReader reader = GrainFactory.GetGrain<IHelloGrainReader>("grainId");
var value = reader.readSomething("currentSessionId");

// issue a write request:
IHelloGrainReader writer = GrainFactory.GetGrain<IHelloGrainWriter>("grainId");
var value = writer.writeSomething("newValue", "currentSessionId");
```

Note that even though increasing the number of replicas increases the amount of parallelism for read requests, it increases memory and CPU usage (every write request must update all read replicas) and thus should not be abused. In addition, increasing the number of replicas beyond your hardware capacity will introduce unnecessary overhead.