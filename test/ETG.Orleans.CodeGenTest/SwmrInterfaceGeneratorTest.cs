/*
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ETG.Orleans.CodeGen;
using ETG.Orleans.CodeGen.CodeGenParticipants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ETG.Orleans.CodeGenTest
{
    /// <summary>
    /// Summary description for SwmrInterfaceGenerator
    /// </summary>
    [TestClass]
    public class SwmrInterfaceGeneratorTest
    {
        [TestMethod]
        public void TestSimpleGrain()
        {
            StringWriter stringWriter = new StringWriter();
            new CodeGenManager("../../../TestData/SWMR_Simple_Interfaces/SWMR_Simple_Interfaces.csproj",
            new SwmrInterfaceGenerator()).CodeGen(stringWriter, false).Wait();

            const string expectedOutput =
@"using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IHelloGrainReader : IGrainWithStringKey
    {
        Task<string> ReadSomething(string sessionId);

        Task<string> ReadSomethingElse(string param, string sessionId);
    }

    public interface IHelloGrainWriter : IGrainWithStringKey
    {
        Task WriteSomething(string something, string sessionId);

        Task<string> WriteSomethingElse(string something, string sessionId);
    }

    public interface IHelloGrainReadReplica : IGrainWithStringKey
    {
        Task<string> ReadSomething();

        Task<string> ReadSomethingElse(string param);

        Task SetState(IGrainState state);
    }
}";
            Assert.AreEqual(expectedOutput, stringWriter.ToString());
        }


        [TestMethod]
        public void TestThatReadWriteInterfacesAreGeneratedForMultipleGrains()
        {
            StringWriter stringWriter = new StringWriter();
            new CodeGenManager("../../../TestData/SWMR_MultipleGrains_Interfaces/SWMR_MultipleGrains_Interfaces.csproj",
            new SwmrInterfaceGenerator()).CodeGen(stringWriter, false).Wait();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(stringWriter.ToString());
            var interfaces = syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            Assert.AreEqual(6, interfaces.Count());

            var interfaceNames = new HashSet<string>(interfaces.Select(i => i.Identifier.Text));
            Assert.IsTrue(interfaceNames.Contains("IHelloGrainReader"));
            Assert.IsTrue(interfaceNames.Contains("IHelloGrainWriter"));
            Assert.IsTrue(interfaceNames.Contains("IHelloGrainReadReplica"));
            Assert.IsTrue(interfaceNames.Contains("IOtherGrainReader"));
            Assert.IsTrue(interfaceNames.Contains("IOtherGrainWriter"));
            Assert.IsTrue(interfaceNames.Contains("IOtherGrainReadReplica"));
        }
    }
}
