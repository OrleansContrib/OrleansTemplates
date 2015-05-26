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

using System.IO;
using ETG.Orleans.CodeGen;
using ETG.Orleans.CodeGen.CodeGenParticipants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ETG.Orleans.CodeGenTest
{
    [TestClass]
    public class GrainBaseGeneratorTest
    {
        [TestMethod]
        public void TestCodeGen()
        {
            StringWriter stringWriter = new StringWriter();
            new CodeGenManager("../../../TestData/State_Simple_Grains/State_Simple_Grains.csproj",
            new GrainBaseGenerator()).CodeGen(stringWriter, false).Wait();

            const string expectedOutput =
@"using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace Grains
{
    [StorageProvider(ProviderName = ""AzureStore"")]
    public abstract partial class MyGrainBase : Grain<IMyGrainState>
    {
        public override Task OnActivateAsync()
        {
            RegisterTimer(new GrainStateWriter(this, this.GetLogger()).WriteState, State, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            return base.OnActivateAsync();
        }
    }
}";
            Assert.AreEqual(expectedOutput, stringWriter.ToString());
        }
    }
}
