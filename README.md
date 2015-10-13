Orleans Templates
=======
**Orleans Templates** increase Orleans developers productivity by offering a set of Visual Studio templates that spare developers from routine configuration and from writing boiler-plate code. It was created by **Microsoft Studios (BigPark)**. It offers the following:

* A full **solution template** for Visual Studio that contains projects for Grains, GrainInterfaces, Local Silo and REST Api.
* A set of **Attributes** backed by a compile-time code generation (using Roslyn) to reduce boiler-plate code. In fact, we support placing ASP.NET routing attributes directly on the Grain Interface and we generate the corresponding Api Controllers for you. In addition, we offer a `SingleWriterMultipleReaders` attribute that can be placed on a grain interface and allows the Grain to respond to multiple read requests in parallel (scale on read access).
* A class library that offers functionality such as *Lazy-write* of Grain State.

Documentation 
=======
* [Installation](Documentation/Installation.md)
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
