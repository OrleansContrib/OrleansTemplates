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

At this point, you have a Silo running backed by a REST Api. You can interact with your grains using a REST client (if the grain have a corresponding web ApiController). There are two ways you can add an ApiController to your grains:

1. In the WebApis project, create a new Controller that inherits from System.Web.Http.ApiController and add the corresponding REST Api to it.
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
* The generated Api Controller will be located in **Properties\etg.orleands.codegen.cs** file of the Grain Interfaces project.
* We only support grains with a **string** key at this point. 

## Additional Grain Attributes
In addition to placing ASP.NET routing attributes on the Grain Interface, we support additional Attributes that can be placed on the Grain implementation and that add behaviour to the grain. To use our Attributes, you should create a Grain implementation using the **ETG Grain** item template. To do this, right click on the Grains project and select **Add -> New Item -> Visual C# Items -> ETG Grain**.

The ETG grain is a normal Orleans Grain but supports additional Attributes that spare you from writing boiler-plate code. We currently have one Attribute called `State` that you can place on your Grain implementation as shown below:
```csharp
    [State(Type = typeof(IPrefsGrainState), LazyWrite = true, Period = 5, StorageProvider = "MemoryStore")]
    public class PrefsGrain : PrefsGrainBase, IPrefsGrain
    {
    ...
    }    
```
In this example, we are declaring that the Grain have a `State` of type `IPrefsGrainState` (the state interface is user defined and must implement `IGrainState`), that the state of the Grain is persisted lazily (only if the `State` has changed) every 5 seconds. The StorageProvider property of the State indicates where the State will be persisted. In this case, we used **MemoryStore** which is defined in the Silo’s **DevTestServerConfiguration.xml** file.

Note that the `PrefsGrain` class inherits from `PrefsGrainBase` and not `Grain`. In fact, the code of `PrefsGrainBase` (which is a subclass of `Grain`) is auto generated at compile time and will contain the code implementing the behavior defined by the Attributes placed on the grain (such as the `State` attribute).
