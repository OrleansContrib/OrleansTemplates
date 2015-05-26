using System;
using System.Text;
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
