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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen
{
    /// <summary>
    /// A helper class to get information about an Method parsed with Roslyn.
    /// </summary>
    public class MethodInspector
    {
        private readonly string _methodName;

        // we assume params have no default values
        private readonly IReadOnlyDictionary<string, string> _methodParams;

        private readonly string _returnType;

        public MethodInspector(MethodDeclarationSyntax methodNode)
        {
            _methodName = methodNode.Identifier.Text;
            _returnType = new TypeInspector(methodNode.ReturnType).ShortName;

            var methodParams = new Dictionary<string, string>();
            foreach (ParameterSyntax parameterNode in methodNode.ParameterList.Parameters)
            {
                string name = parameterNode.Identifier.Text;
                string type = new TypeInspector(parameterNode.Type).ShortName;
                methodParams[name] = type;
            }
            _methodParams = new ReadOnlyDictionary<string, string>(methodParams);
        }

        public MethodInspector(IMethodSymbol methodSymbol)
        {
            _methodName = methodSymbol.Name;
            INamedTypeSymbol returnTypeSymbol = methodSymbol.ReturnType as INamedTypeSymbol;
            _returnType = new TypeInspector(returnTypeSymbol).ShortName;

            var methodParams = new Dictionary<string, string>();
            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                string name = parameter.Name;
                string type = new TypeInspector(parameter.Type).ShortName;
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

        public override bool Equals(object obj)
        {
            if (!(obj is MethodInspector))
            {
                return false;
            }
            MethodInspector methodInspector = (MethodInspector)obj;
            return _methodName.Equals(methodInspector._methodName) &&
                   AreEqual(_methodParams, methodInspector._methodParams) &&
                   _returnType.Equals(methodInspector._returnType);
        }

        private static bool AreEqual(IReadOnlyDictionary<string, string> dict1, IReadOnlyDictionary<string, string> dict2 )
        {
            return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
        }
    }
}
