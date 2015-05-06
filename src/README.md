# Code Generation Documentation
## Overview
In **Orleans Templates**, we generate code in both the *Grains* and the *Grain Interfaces* projects.
The code is generated as part of the build process. In fact, right before a project is compiled, the code generation tasks are invoked and new code is added. Because the code is generated before the compilation, the generated code is also compiled and added to Intellisense.
To invoke the code generation tasks before the build, we add a BeforeBuild target (see [How to: Extend the Visual Studio Build Process](https://msdn.microsoft.com/en-us/library/ms366724.aspx)) to the .csproj files. Here is what it looks like:

* Grain Interfaces Project
```xml
<UsingTask TaskName="ETG.Orleans.Tasks.GenerateApiControllersTask" AssemblyFile="$(ProjectDir)..\..\packages\ETG.Orleans.1.0.1\content\dependencies\ETG.Orleans.dll" />
  <Target Name="BeforeBuild">
    <message text="ETG: GenerateCode running..." importance="high" />
    <GenerateApiControllersTask ProjectPath="$(ProjectDir)$(AssemblyName).csproj" OutputPath="$(ProjectDir)Properties\etg.orleans.codegen.cs" />
    <message text="ETG: GenerateCode done!" importance="high" />
  </Target>
</Project>
```
* Grains Project
```xml
<UsingTask TaskName="ETG.Orleans.Tasks.GenerateGrainBaseTask" AssemblyFile="$(ProjectDir)..\..\packages\ETG.Orleans.1.0.0\content\dependencies\ETG.Orleans.dll" />
  <Target Name="BeforeBuild">
    <message text="ETG: GenerateCode running..." importance="high" />
    <GenerateGrainBaseTask ProjectPath="$(ProjectDir)$(AssemblyName).csproj" OutputPath="$(ProjectDir)Properties\etg.orleans.codegen.cs" />
    <message text="ETG: GenerateCode done!" importance="high" />
  </Target>
```

Both the `GenerateApiControllersTask` and the `GenerateGrainBaseTask` are part of the **ETG.Orleans** assembly which is added to the **ETG Orleans Solution Template** using a NuGet package. These two tasks are described in the following.

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

## Generate Grain Base Task
The **GenerateGrainBaseTask** is invoked during the build (before compilation) of the *Grains* project. Similarly to the **GenerateApiControllerTask**, this task will look for specific Attributes in the grains project and inject code accordingly. However, this task is a bit trickier because the generated code is added to the parent class of the grain and is injected via inheritance.

For this to work correctly, the **ETG Grain** item template must be used to create a `Grain`. With this template, when a `Grain` is created, an empty `abstract partial` parent class will be generated and inject as shown below:

```csharp
namespace Grains
{
    [State(Type = typeof(IPrefsGrainState), LazyWrite = true, Period = 5, StorageProvider = "MemoryStore")]
    public class PrefsGrain : PrefsGrainBase, IPrefsGrain
    {
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }
    }
}
```

```csharp
namespace Grains
{
    public abstract partial class PrefsGrainBase : Grain<IPrefsGrainState>
    {
    }
}
```
Note that in Visual Studio *Solution Explorer*, the `PrefsGrainBase` class is placed in the subtree of the corresponding `PrefsGrain` class so the user don't see it without expanding the node. The goal is to hide this class because its content is *codegened* (in another `partial` class).

Because `PrefsGrainBase` is `partial`, the **GenerateGrainBaseTask** can generate the rest of the class (based on the Attributes added to the `PrefsGrain`) and add the generated code to **Properties\etg.orleans.codegen.cs**.

## Code Generation
The code generation is done using **Roslyn**. The best way to understand it is to read the source code of the **ETG.Orleans.CodeGen** project.

