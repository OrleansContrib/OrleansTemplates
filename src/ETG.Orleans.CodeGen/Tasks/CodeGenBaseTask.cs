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
using System.IO;
using System.Reflection;
using ETG.Orleans.CodeGen.CodeGenParticipants;
using ETG.Orleans.CodeGen.Utils;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace ETG.Orleans.CodeGen.Tasks
{

    /// <summary>
    /// Base class for visual studio build tasks that are called from a project templates to generate code.
    /// </summary>
    public abstract class CodeGenBaseTask : Task
    {
        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string ProjectPath { get; set; }

        public override bool Execute()
        {
            try
            {
                using (var streamWriter = new StreamWriter(OutputPath))
                {
                    new CodeGenManager(ProjectPath, GetCodeGenParticipants()).CodeGen(streamWriter).Wait();
                }
                return true;
            }
            catch (ReflectionTypeLoadException exception)
            {
                StringWriter strWriter = new StringWriter();
                LoaderExceptionUtils.LogExceptionDetails(exception.LoaderExceptions, strWriter);
                LogMessage(strWriter.ToString());
            }
            catch (Exception e)
            {
                LogMessage(e.ToString());
            }
            return false;
        }

        public abstract ICodeGenParticipant[] GetCodeGenParticipants();

        private void LogMessage(string message)
        {
            Log.LogMessage(MessageImportance.High, "ETG: " + message);
        }
    }
}
