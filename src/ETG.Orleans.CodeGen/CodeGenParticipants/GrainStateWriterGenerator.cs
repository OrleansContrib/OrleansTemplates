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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ETG.Orleans.CodeGen.CodeGenParticipants
{
    /// <summary>
    /// Generates the GrainStateWriter class which handles writing the State of a grain in a lazy fashion.
    /// </summary>
    public class GrainStateWriterGenerator : ICodeGenParticipant
    {
        public Task<CodeGenResult> CodeGen(Workspace workspace, Project project)
        {
            IEnumerable<string> usings = new [] { "System", "System.Threading.Tasks", "Orleans", "Orleans.Runtime" };
            const string code = @"internal class GrainStateWriter
{
       private IGrainState _currentState;
        private readonly IGrainWithStringKey _grain;
        private readonly Logger _logger;

        public GrainStateWriter(Grain grain, Logger logger)
        {
            if (!(grain is IGrainWithStringKey))
            {
                throw new ArgumentException(string.Format(""Only grains that implement {0} are supported"", typeof(IGrainWithStringKey).ToString()));
            }
            _grain = (IGrainWithStringKey)grain;
            _logger = logger;
        }
        public async Task WriteState(object stateObj)
        {
            IGrainState state = stateObj as IGrainState;
            if (state == null)
            {
                throw new ArgumentException(""stateObj should be an implementation of IGrainState"");    
            }

            if (!stateObj.Equals(_currentState))
            {
                try
                {
                    await state.WriteStateAsync();
                    _currentState = state;
                }
                catch
                {
                    _logger.Error(0, ""Exception commiting state for grain: "" + GetType().FullName + ""("" + _grain.GetPrimaryKeyString() + "")"");
                }
            }
        }
}";
            return Task.FromResult(new CodeGenResult(code, usings));
        }
    }
}
