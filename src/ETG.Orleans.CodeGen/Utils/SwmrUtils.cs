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
using System.Linq;
using ETG.Orleans.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen.Utils
{
    public static class SwmrUtils
    {
        public static string GetReadInterfaceName(string grainInterfaceName)
        {
            return grainInterfaceName + "Reader";
        }

        public static string GetReadGrainName(string grainName)
        {
            return grainName + "Reader";
        }

        public static string GetReadReplicaInterfaceName(string grainInterfaceName)
        {
            return grainInterfaceName + "ReadReplica";
        }

        public static string GetReadReplicaGrainName(string grainName)
        {
            return grainName + "ReadReplica";
        }

        public static string GetWriteInterfaceName(string grainInterfaceName)
        {
            return grainInterfaceName + "Writer";
        }

        public static string GetWriteGrainName(string grainName)
        {
            return grainName + "Writer";
        }

        public static bool IsReadonly(MethodDeclarationSyntax methodNode, SemanticModel semanticModel)
        {
            return AttributeUtils.SelectAttributeOfType(methodNode.AttributeLists, typeof(ReadOnlyAttribute), semanticModel) != null;
        }

        public static IEnumerable<MethodDeclarationSyntax> GetReadOnlyMethods(
            IEnumerable<MethodDeclarationSyntax> methods, SemanticModel semanticModel)
        {
            return methods.Where(method => IsReadonly(method, semanticModel));
        }

        public static IEnumerable<MethodDeclarationSyntax> GetWriteMethods(
            IEnumerable<MethodDeclarationSyntax> methods, SemanticModel semanticModel)
        {
            return methods.Except(GetReadOnlyMethods(methods, semanticModel));
        }

        public static IEnumerable<MethodDeclarationSyntax> RemoveReadOnlyAttribute(
            IEnumerable<MethodDeclarationSyntax> methods, SemanticModel semanticModel)
        {
            return
                methods.Select(
                    method =>
                        method.WithAttributeLists(AttributeUtils.RemoveAttributeOfType(method.AttributeLists,
                            typeof (ReadOnlyAttribute), semanticModel)));
        }

        public static IEnumerable<MethodDeclarationSyntax> AddSessionIdParameter(
            IEnumerable<MethodDeclarationSyntax> methods)
        {
            return methods.Select(AddSessionIdParameter);
        }

        public static MethodDeclarationSyntax AddSessionIdParameter(MethodDeclarationSyntax method)
        {
            var sessionIdParam = RoslynUtils.CreateParameter("string", "sessionId");
            return RoslynUtils.AppendParameterToMethod(method, sessionIdParam);
        }
    }
}
