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
    /// Parses all the code in a given Project and generates a base class for each Grain annotated with <see cref="StateAttribute"/>.
    /// The generated class is an intermediate class that inherits from <see cref="Orleans.Grain"/> and contains the functionality specified in the <see cref="StateAttribute"/>.
    /// </summary>
    public class GrainBaseGenerator : ICodeGenParticipant
    {
        private const string BaseSuffix = "Base";
        private const string StateTypePropertyName = "Type";
        private const string StateLazyWritePropertyName = "LazyWrite";
        private const string StatePeriodPropertyName = "Period";
        private const string StorageProviderPropertyName = "StorageProvider";

        private SemanticModel _semanticModel;

        public async Task<CodeGenResult> CodeGen(Workspace workspace, Project project)
        {
            CompilationUnitSyntax cu = SF.CompilationUnit();
            
            foreach (Document document in project.Documents)
            {
                SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                _semanticModel = await document.GetSemanticModelAsync();

                IEnumerable<ClassDeclarationSyntax> grainDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (ClassDeclarationSyntax grainDeclaration in grainDeclarations)
                {
                    if (!RoslynUtils.IsPublic(grainDeclaration)) continue;

                    AttributeSyntax stateAttr = AttributeUtils.SelectAttributeOfType(grainDeclaration.AttributeLists, typeof(StateAttribute), _semanticModel);
                    if (stateAttr == null) continue;

                    var namespaceNode = grainDeclaration.Parent as NamespaceDeclarationSyntax;
                    if (namespaceNode == null)
                    {
                        throw new Exception("A grain must be declared inside a namespace");
                    }

                    string fullNameSpace = _semanticModel.GetDeclaredSymbol(namespaceNode).ToString();
                    NamespaceDeclarationSyntax namespaceDclr = 
                        SF.NamespaceDeclaration(SF.IdentifierName(fullNameSpace));

                    AttributeInspector attrInspector = new AttributeInspector(stateAttr, _semanticModel);
                    string stateType = GetStateType(attrInspector);
                    bool lazyWriteValue = GetLazyWriteValue(attrInspector);
                    string storageProviderValue = GetStorageProviderValue(attrInspector);

                    string grainBaseName = grainDeclaration.Identifier.Text + BaseSuffix;
                    ClassDeclarationSyntax classDclr = SF.ClassDeclaration(SF.Identifier(grainBaseName)).AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AbstractKeyword), SF.Token(SyntaxKind.PartialKeyword)).WithBaseList(SF.BaseList(SF.SeparatedList<BaseTypeSyntax>().Add(SF.SimpleBaseType(SF.IdentifierName(string.Format("Grain<{0}>", stateType))))));

                    if (lazyWriteValue)
                    {
                        long period = GetPeriodValue(attrInspector);
                        TypeSyntax returnType = SF.ParseTypeName("Task");
                        MethodDeclarationSyntax methodDeclaration = SF.MethodDeclaration(returnType, SF.Identifier("OnActivateAsync")).AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)).AddBodyStatements(
                            SF.ParseStatement(string.Format("RegisterTimer(new GrainStateWriter(this, this.GetLogger()).WriteState, State, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds({0}));\n", period)),
                            SF.ParseStatement("return base.OnActivateAsync();"));
                        classDclr = classDclr.AddMembers(methodDeclaration);
                    }

                    if (storageProviderValue != null)
                    {
                        AttributeSyntax storageProviderAttribute = SF.Attribute(SF.IdentifierName("StorageProvider"), SF.AttributeArgumentList().WithArguments(SF.SeparatedList<AttributeArgumentSyntax>().Add(SF.AttributeArgument(SF.NameEquals(SF.IdentifierName("ProviderName")), null, SF.IdentifierName(storageProviderValue)))));
                        classDclr = classDclr.WithAttributeLists(SF.List<AttributeListSyntax>().Add(SF.AttributeList().AddAttributes(storageProviderAttribute)));
                    }

                    namespaceDclr = namespaceDclr.AddMembers(classDclr);
                    cu = cu.AddMembers(namespaceDclr);
                }
            }
            return new CodeGenResult(Formatter.Format(cu, workspace).ToString(), GetCommonUsings());
        }

        private string GetStorageProviderValue(AttributeInspector attrInspector)
        {
            if (!attrInspector.NamedArguments.ContainsKey(StorageProviderPropertyName))
            {
                return null;
            }
            return attrInspector.NamedArguments[StorageProviderPropertyName];
        }

        private static long GetPeriodValue(AttributeInspector attrInspector)
        {
            if (!attrInspector.NamedArguments.ContainsKey(StatePeriodPropertyName))
            {
                return new StateAttribute().Period;
            }
            return Convert.ToInt64(attrInspector.NamedArguments[StatePeriodPropertyName]);
        }

        private static bool GetLazyWriteValue(AttributeInspector attrInspector)
        {
            if (!attrInspector.NamedArguments.ContainsKey(StateLazyWritePropertyName))
            {
                return false;
            }
            return attrInspector.NamedArguments[StateLazyWritePropertyName] == "true";
        }

        private static string GetStateType(AttributeInspector attrInspector)
        {
            if (!attrInspector.NamedArguments.ContainsKey(StateTypePropertyName))
            {
                throw new ArgumentException("The State Attribute must specify a Type");
            }
            string type = attrInspector.NamedArguments[StateTypePropertyName];
            if (!type.Contains("(") || !type.Contains(")"))
            {
                throw new ArgumentException("The State type must be specified with the typeof() operator");
            }
            return attrInspector.NamedArguments[StateTypePropertyName].Split('(')[1].Split(')')[0];
        }

        private static string[] GetCommonUsings()
        {
            return new[]
            {
                "System", 
                "System.Threading.Tasks",
                "Orleans",
                "Orleans.Providers"
            };
        }
    }
}
