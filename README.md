Orleans Templates
=======
**Orleans Templates** increase Orleans developers productivity by offering a set of Visual Studio templates that spare developers from routine configuration and from writing boiler-plate code. It was created by **Microsoft Studios (BigPark)**. It offers the following:

* A full **solution template** for Visual Studio that contains projects for Grains, GrainInterfaces, Local Silo and REST Api.
* A set of **Attributes** backed by a compile-time code generation (using Roslyn) to reduce boiler-plate code. In fact, we support placing ASP.NET routing attributes directly on the Grain Interface and we generate the corresponding Api Controllers for you. In addition, we offer a `State` attribute that allow one to associate a **state** with a Grain and to **persist** the State periodically (using a timer) without writing a single line of code.

Prerequisites
=======
* Visual Studio 2013.
* Microsoft Build Tools 2015 ([download](http://go.microsoft.com/?linkid=9863815)). You can skip this step if you have Visual Studio 2015 Preview installed.

Installation
=======
To install the Orleans Templates, simply clone the repository and copy the Visual Studio templates:
* Copy the **ETG Orleans Solution Template** to your Visual Studio Project Templates folder (mine is C:\Users\nbilal\Documents\Visual Studio 2013\Templates\ProjectTemplates).
* Copy the **ETG Orleans Grain Item Template** to your Visual Studio Item Templates folder (mine is C:\Users\nbilal\Documents\Visual Studio 2013\Templates\ItemTemplates).

Documentation 
=======
* [Tutorial](Documentation/Tutorial.md)
* [Code Generation Documentation]. (useful for contributors)

Contribute!
=======
We welcome and appreciate contributions of all sorts including suggestions, bug fixes, improvements, more tests, etc.

One of the best way to contribute would be to add new `Attribute`s that offer useful functionality or save users from writing boiler-plate code. Please consult the [Code Generation Documentation] and the source code for more information on how to add new `Attribute`s.

License
=======
This project is licensed under the [MIT license](LICENSE).


[Code Generation Documentation]: Documentation/CodeGenDocumentation.md
