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
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen
{
    /// <summary>
    /// A helper class to get information about an Method parsed with Roslyn.
    /// </summary>
    public class MethodNodeInspector
    {
        private readonly string _methodName;

        // we assume params have no default values
        private readonly IReadOnlyDictionary<string, string> _methodParams;

        private readonly string _returnType;

        public MethodNodeInspector(MethodDeclarationSyntax methodNode)
        {
            _methodName = methodNode.Identifier.Text;
            _returnType = methodNode.ReturnType.ToString();

            var methodParams = new Dictionary<string, string>();
            foreach (ParameterSyntax parameterNode in methodNode.ParameterList.Parameters)
            {
                string name = parameterNode.Identifier.Text;
                string type = parameterNode.Type.ToString();
                methodParams[name] = type;
            }
            _methodParams = new ReadOnlyDictionary<string, string>(methodParams);
        }

        public string MethodName
        {
            get { return _methodName; }
        }

        public IReadOnlyDictionary<string, string> MethodParams
        {
            get { return _methodParams; }
        }

        public string ReturnType
        {
            get { return _returnType; }
        }
    }
}
