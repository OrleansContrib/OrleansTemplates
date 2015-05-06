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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ETG.Orleans.CodeGen.CodeGenParticipants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ETG.Orleans.CodeGen
{
    /// <summary>
    /// Manages codegen participant. It calls each codegen participant in order, adds blank lines between their outputs and writes the full generated code to
    /// the given TextWriter.
    /// </summary>
    public class CodeGenManager
    {
        private readonly string _csProjPath;
        private readonly IReadOnlyList<ICodeGenParticipant> _codeGenParticipants; 

        public CodeGenManager(string csProjPath, params ICodeGenParticipant[] participants)
        {
            _csProjPath = csProjPath;
            _codeGenParticipants = new List<ICodeGenParticipant>(participants);
        }

        public Task CodeGen(TextWriter textWriter)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Project project = workspace.OpenProjectAsync(_csProjPath).Result;
            if (project == null)
            {
                throw new ArgumentException("Could not open the project located at " + _csProjPath);
            }

            foreach (ICodeGenParticipant codeGenParticipant in _codeGenParticipants)
            {
                codeGenParticipant.CodeGen(workspace, project, textWriter).Wait();
                if (codeGenParticipant != _codeGenParticipants.Last())
                {
                    textWriter.Write(Environment.NewLine);
                }
            }
            workspace.CloseSolution();
            return Task.FromResult(false);
        }
    }
}
