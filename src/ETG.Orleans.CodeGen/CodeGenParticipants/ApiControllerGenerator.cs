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

    /// <summary>
    /// Parses all the code in a given Project and generates an ApiController for each Grain interfaces annotated with <see cref="ApiControllerAttribute"/>.
    /// </summary>
    public class ApiControllerGenerator : ICodeGenParticipant
    {
        const string RoutePrefix = "RoutePrefix";
        private const string ApicontrollerClassName = "ApiController";

        public async Task<CodeGenResult> CodeGen(Workspace workspace, Project project)
        {
            var usings = new HashSet<string>();
            usings.UnionWith(GetCommonUsings());
            CompilationUnitSyntax cu = SF.CompilationUnit();

            foreach (Document document in project.Documents)
            {
                SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                SemanticModel semanticModel = await document.GetSemanticModelAsync();

                IEnumerable<InterfaceDeclarationSyntax> interfaces = syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();

                bool copyDocumentUsings = false;

                foreach (var interfaceNode in interfaces)
                {
                    ClassDeclarationSyntax classDclr = GenerateApiControllerForInterface(interfaceNode, semanticModel);
                    if (classDclr == null)
                    {
                        continue;
                    }

                    // only copy the usings in the document if at least one ApiController is generated
                    copyDocumentUsings = true;
                    var namespaceNode = interfaceNode.Parent as NamespaceDeclarationSyntax;
                    // ReSharper disable once PossibleNullReferenceException
                    usings.UnionWith(namespaceNode.Usings.Select(@using => @using.Name.ToString()));

                    // use the same namespace in the generated class
                    string fullNameSpace = semanticModel.GetDeclaredSymbol(namespaceNode).ToString();
                    NamespaceDeclarationSyntax namespaceDclr = SF.NamespaceDeclaration(SF.IdentifierName(fullNameSpace)).WithUsings(namespaceNode.Usings);
                    namespaceDclr = namespaceDclr.AddMembers(classDclr);
                    cu = cu.AddMembers(namespaceDclr);
                }
                if (copyDocumentUsings)
                {
                    usings.UnionWith(syntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(@using => @using.Name.ToString()));
                }
            }

            return new CodeGenResult(Formatter.Format(cu, workspace).ToString(), usings);
        }

        private static ClassDeclarationSyntax GenerateApiControllerForInterface(InterfaceDeclarationSyntax interfaceNode, SemanticModel semanticModel)
        {
            if (!RoslynUtils.IsPublic(interfaceNode)) return null;

            AttributeSyntax apiControllerAttribute = AttributeUtils.SelectAttributeOfType(interfaceNode.AttributeLists, typeof(ApiControllerAttribute), semanticModel);

            // if the interface is not annotated with the ApiController attribute, do nothing
            if (apiControllerAttribute == null)
            {
                return null;
            }

            var namespaceNode = interfaceNode.Parent as NamespaceDeclarationSyntax;
            if (namespaceNode == null)
            {
                throw new Exception("A grain interface must be declared inside a namespace");
            }

            // copy all attributes except the ApiController attribute
            SyntaxList<AttributeListSyntax> attributesLists = AttributeUtils.RemoveAttributeOfType(interfaceNode.AttributeLists, typeof(ApiControllerAttribute), semanticModel);

            // add the RoutePrefix attribute (if any)
            var attributeInspector = new AttributeInspector(apiControllerAttribute, semanticModel);
            if (attributeInspector.NamedArguments.ContainsKey(RoutePrefix))
            {
                AttributeSyntax routePrefixAttribute = AttributeUtils.AttributeWithArgument(RoutePrefix,
                    attributeInspector.NamedArguments[RoutePrefix]);
                attributesLists = attributesLists.Add(AttributeUtils.AttributeList(routePrefixAttribute));
            }

            string grainName = interfaceNode.Identifier.Text;
            string apiControllerName = GetApiControllerName(grainName);

            // create the Api controller class, add the attributes to it and make it a subclass of ApiController
            ClassDeclarationSyntax classDclr =
                SF.ClassDeclaration(SF.Identifier(apiControllerName)).AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)).WithAttributeLists(SF.List(attributesLists)).WithBaseList(SF.BaseList(SF.SeparatedList<BaseTypeSyntax>().Add(SF.SimpleBaseType(SF.IdentifierName(ApicontrollerClassName)))));

            // generate the api controller methods and add them to the class
            IEnumerable<MethodDeclarationSyntax> apiControllerMethods = GenerateApiControllerMethods(RoslynUtils.GetMethodDeclarations(interfaceNode), grainName);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var method in apiControllerMethods)
            {
                classDclr = classDclr.AddMembers(method);
            }
            return classDclr;
        }

        private static IEnumerable<MethodDeclarationSyntax> GenerateApiControllerMethods(IEnumerable<MethodDeclarationSyntax> grainInterfaceMethods, string grainName)
        {
            var methodsDeclarations = new List<MethodDeclarationSyntax>();
            foreach (var methodNode in grainInterfaceMethods)
            {
                // insert the id parameter at the end of the list of parameters
                var idParam = RoslynUtils.CreateParameter("string", "id");
                MethodDeclarationSyntax methodDclr = RoslynUtils.AppendParameterToMethod(methodNode, idParam);

                methodDclr = methodDclr.AddModifiers(SF.Token(SyntaxKind.PublicKeyword)).WithSemicolonToken(SF.Token(SyntaxKind.None));

                StatementSyntax getGrainStmt = SF.ParseStatement(string.Format(
                        "var grain = GrainFactory.GetGrain<{0}>(id);\n", grainName));
                MethodInspector methodInspector = new MethodInspector(methodNode);

                string callGrainStmt = string.Format("grain.{0}({1});",
                    methodInspector.MethodName, string.Join(", ", methodInspector.MethodParams.Keys));

                if (methodInspector.ReturnType != "Task")
                {
                    callGrainStmt = callGrainStmt.Insert(0, "return ");
                }
                else
                {
                    callGrainStmt = callGrainStmt.Insert(0, "await ");
                    methodDclr = methodDclr.AddModifiers(SF.Token(SyntaxKind.AsyncKeyword));
                }
                StatementSyntax returnStmt = SF.ParseStatement(callGrainStmt);

                methodsDeclarations.Add(methodDclr.WithBody(SF.Block(getGrainStmt, returnStmt)));
            }
            return methodsDeclarations;
        }

        private static string GetApiControllerName(string grainName)
        {
            return NamingUtils.FormatName(grainName, 'I', "Controller");
        }

        private static IEnumerable<string> GetCommonUsings()
        {
            return new[]
            {
                "System", 
                "System.Web.Http", 
                "System.Threading.Tasks",
                "Orleans"
            };
        }

        public static bool IsAttributeTypeOf(SemanticModel semanticModel, AttributeSyntax attributeSyntax, Type type)
        {
            TypeInfo typeInfo = semanticModel.GetTypeInfo(attributeSyntax);
            ITypeSymbol typeSymbol = typeInfo.Type;
            return typeSymbol.ToString() == type.FullName;
        }
    }
}
