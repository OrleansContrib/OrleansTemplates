# Prerequisites

* Visual Studio 2013.
* Microsoft Build Tools 2015 ([download](http://go.microsoft.com/?linkid=9863815)). You can skip this step if you have Visual Studio 2015 Preview installed.

# Installation

To install OrleansTemplates, simply install the [ETG.Orleans.Templates.vsix](https://visualstudiogallery.msdn.microsoft.com/b61c87e7-0655-4a6e-8e4f-84192950e08c). This will add five project templates to your Visual Studio installation:
* ETG Orleans Solution
* ETG.Orleans.Templates.Interfaces
* ETG.Orleans.Templates.Grains
* ETG.Orleans.Templates.Host
* ETG.Orleans.Templates.WebApis

Typically, you will only need the **ETG Orleans Solution** template.

## Install templates to existing projects
Some of OrleansTemplates project templates (Grains and Grain Interfaces projects) are available as **NuGet Packages**.

If you have an existing project (e.g. a class library) that was not created with OrleansTemplates, you can install the *ETG.Orleans.Templates.Interfaces* or *ETG.Orleans.Templates.Grains* to it from the corresponding NuGet packages. This will add the appropriate references and code generation to existing projects. 