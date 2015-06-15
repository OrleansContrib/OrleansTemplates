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
    [TestClass]
    public class SwmrGrainGeneratorTest
    {
        [TestMethod]
        public void TestCodeGen()
        {
            StringWriter stringWriter = new StringWriter();
            new CodeGenManager("../../../TestData/SWMR_Simple_Grains/SWMR_Simple_Grains.csproj",
            new SwmrGrainsGenerator()).CodeGen(stringWriter, false).Wait();

            const string expectedOutput =
@"using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using System.Linq;
using ETG.Orleans.Swmr;
using System.Collections.Generic;
using Orleans.Concurrency;

namespace Grains
{
    public class HelloGrainWriter : Grain, IHelloGrainWriter
    {
        private ITopology _topology;

        public override async Task OnActivateAsync()
        {
            _topology = new ConsistentHashRing(10);
            await base.OnActivateAsync();
        }

        public async Task WriteSomething(string something, string sessionId)
        {
            string grainId = this.GetPrimaryKeyString();
            IHelloGrain grain = GrainFactory.GetGrain<IHelloGrain>(grainId);
            await grain.WriteSomething(something);
            IGrainState state = await grain.GetState();
            string sessionNode = _topology.GetNode(sessionId);
            IEnumerable<string> otherNodes = _topology.Nodes.Where(node => node != sessionNode);
            foreach (string node in otherNodes)
            {
                GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + ""_"" + node).SetState(state);
            }

            await GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + ""_"" + sessionNode).SetState(state);
        }

        public async Task<string> WriteSomethingElse(string something, string sessionId)
        {
            string grainId = this.GetPrimaryKeyString();
            IHelloGrain grain = GrainFactory.GetGrain<IHelloGrain>(grainId);
            var result = await grain.WriteSomethingElse(something);
            IGrainState state = await grain.GetState();
            string sessionNode = _topology.GetNode(sessionId);
            IEnumerable<string> otherNodes = _topology.Nodes.Where(node => node != sessionNode);
            foreach (string node in otherNodes)
            {
                GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + ""_"" + node).SetState(state);
            }

            await GrainFactory.GetGrain<IHelloGrainReadReplica>(grainId + ""_"" + sessionNode).SetState(state);
            return result;
        }
    }

    [StatelessWorker]
    public class HelloGrainReader : Grain, IHelloGrainReader
    {
        private ITopology _topology;

        public override async Task OnActivateAsync()
        {
            _topology = new ConsistentHashRing(10);
            await base.OnActivateAsync();
        }

        public Task<string> ReadSomething(string sessionId)
        {
            string sessionNode = _topology.GetNode(sessionId);
            var readReplica = GrainFactory.GetGrain<IHelloGrainReadReplica>(this.GetPrimaryKeyString() + ""_"" + sessionNode);
            return readReplica.ReadSomething();
        }

        public Task<string> ReadSomethingElse(string param, string sessionId)
        {
            string sessionNode = _topology.GetNode(sessionId);
            var readReplica = GrainFactory.GetGrain<IHelloGrainReadReplica>(this.GetPrimaryKeyString() + ""_"" + sessionNode);
            return readReplica.ReadSomethingElse(param);
        }
    }

    public class HelloGrainReadReplica : Grain, IHelloGrainReadReplica
    {
        private IHelloGrainState State;

        public override async Task OnActivateAsync()
        {
            string grainId = this.GetPrimaryKeyString().Split('_')[0];
            IHelloGrain grain = GrainFactory.GetGrain<IHelloGrain>(grainId);
            await SetState(await grain.GetState());
            await base.OnActivateAsync();
        }

        public Task<string> ReadSomething()
        {
            return Task.FromResult(State.Value);
        }

        public Task<string> ReadSomethingElse(string param)
        {
            return Task.FromResult(State.OtherValue);
        }

        public Task SetState(IGrainState state)
        {
            State = state as IHelloGrainState;
            return TaskDone.Done;
        }
    }
}";
            Assert.AreEqual(expectedOutput, stringWriter.ToString());
        }

        [TestMethod]
        public void TestThatReadWriteGrainsAreGeneratedForAllGrains()
        {
            StringWriter stringWriter = new StringWriter();
            new CodeGenManager("../../../TestData/SWMR_MultipleGrains_Grains/SWMR_MultipleGrains_Grains.csproj",
            new SwmrGrainsGenerator()).CodeGen(stringWriter, false).Wait();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(stringWriter.ToString());
            var classes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            Assert.AreEqual(6, classes.Count());

            var interfaceNames = new HashSet<string>(classes.Select(c => c.Identifier.Text));
            Assert.IsTrue(interfaceNames.Contains("HelloGrainReader"));
            Assert.IsTrue(interfaceNames.Contains("HelloGrainWriter"));
            Assert.IsTrue(interfaceNames.Contains("HelloGrainReadReplica"));
            Assert.IsTrue(interfaceNames.Contains("OtherGrainReader"));
            Assert.IsTrue(interfaceNames.Contains("OtherGrainWriter"));
            Assert.IsTrue(interfaceNames.Contains("OtherGrainReadReplica"));
        }
    }
}
