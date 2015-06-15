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
using System.Linq;
using System.Threading.Tasks;
using ETG.Orleans.Attributes;
using ETG.Orleans.CodeGen.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ETG.Orleans.CodeGen.CodeGenParticipants
{
    using SF = SyntaxFactory;

    public class SwmrInterfaceGenerator : ICodeGenParticipant
    {
        private SemanticModel _semanticModel;

        public async Task<CodeGenResult> CodeGen(Workspace workspace, Project project)
        {
            CompilationUnitSyntax cu = SF.CompilationUnit();

            var usings = new HashSet<string>();
            foreach (Document document in project.Documents)
            {
                SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                _semanticModel = await document.GetSemanticModelAsync();

                IEnumerable<InterfaceDeclarationSyntax> interfaces =
                    syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
                
                foreach (var interfaceNode in interfaces)
                {
                    if (!RoslynUtils.IsPublic(interfaceNode)) continue;

                    AttributeSyntax swmrAttribute = AttributeUtils.SelectAttributeOfType(interfaceNode.AttributeLists, typeof(SingleWriterMultipleReadersAttribute), _semanticModel);

                    if (swmrAttribute == null)
                    {
                        continue;
                    }

                    var namespaceNode = interfaceNode.Parent as NamespaceDeclarationSyntax;
                    if (namespaceNode == null)
                    {
                        throw new Exception("A grain interface must be declared inside a namespace");
                    }

                    usings.UnionWith(syntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(usingDirective => usingDirective.Name.ToString()));

                    var methods = RoslynUtils.GetMethodDeclarations(interfaceNode);
                    var readMethods = SwmrUtils.GetReadOnlyMethods(methods, _semanticModel);
                    var writeMethods = methods.Except(readMethods).Where(method => new MethodInspector(method).MethodName != "GetState");

                    readMethods = SwmrUtils.RemoveReadOnlyAttribute(readMethods, _semanticModel);

                    string grainInterfaceName = interfaceNode.Identifier.Text;
                    var readReplicaMethods = new List<MethodDeclarationSyntax>(readMethods) {GenerateSetStateMethod()};

                    InterfaceDeclarationSyntax readReplicaInterface =
                        GenerateInterface(SwmrUtils.GetReadReplicaInterfaceName(grainInterfaceName), readReplicaMethods,
                            interfaceNode.BaseList);

                    readMethods = SwmrUtils.AddSessionIdParameter(readMethods);
                    writeMethods = SwmrUtils.AddSessionIdParameter(writeMethods);

                    string readerInterfaceName = SwmrUtils.GetReadInterfaceName(grainInterfaceName);
                    string writerInterfaceName = SwmrUtils.GetWriteInterfaceName(grainInterfaceName);

                    InterfaceDeclarationSyntax readerInterface = GenerateInterface(readerInterfaceName, readMethods,
                        interfaceNode.BaseList);
                    InterfaceDeclarationSyntax writerInterface = GenerateInterface(writerInterfaceName, writeMethods,
                        interfaceNode.BaseList);

                    string fullNameSpace = _semanticModel.GetDeclaredSymbol(namespaceNode).ToString();
                    NamespaceDeclarationSyntax namespaceDclr = SF.NamespaceDeclaration(SF.IdentifierName(fullNameSpace)).WithUsings(namespaceNode.Usings);

                    namespaceDclr = namespaceDclr.AddMembers(readerInterface, writerInterface, readReplicaInterface);

                    usings.UnionWith(namespaceNode.Usings.Select(@using => @using.Name.ToString()));
                    usings.Remove("ETG.Orleans.Attributes");
                    cu = cu.AddMembers(namespaceDclr);
                }
            }
            return new CodeGenResult(Formatter.Format(cu, workspace).ToString(), usings);
        }

        private static InterfaceDeclarationSyntax GenerateInterface(string name, IEnumerable<MethodDeclarationSyntax> methods, BaseListSyntax baseList)
        {
            InterfaceDeclarationSyntax generatedInterface = SF.InterfaceDeclaration(name).AddModifiers(SF.Token(SyntaxKind.PublicKeyword)).WithBaseList(baseList);
            foreach (var method in methods)
            {
                generatedInterface = generatedInterface.AddMembers(method.WithLeadingTrivia(SF.EndOfLine("")));
            }
            return generatedInterface;
        }

        private MethodDeclarationSyntax GenerateSetStateMethod()
        {
            SyntaxTree syntaxTree = SF.ParseSyntaxTree(@"Task SetState(IGrainState state);");
            return syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        }

    }
}
