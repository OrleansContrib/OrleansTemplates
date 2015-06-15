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

    public class SwmrGrainsGenerator : ICodeGenParticipant
    {
        private const string ReadReplicaCountParamName = "ReadReplicaCount";
        private const string StatelessWorkerAttributeName = "StatelessWorker";
        private SemanticModel _semanticModel;

        public async Task<CodeGenResult> CodeGen(Workspace workspace, Project project)
        {
            CompilationUnitSyntax cu = SF.CompilationUnit();

            var usings = new HashSet<string>();
            bool copyUsings = false;

            foreach (Document document in project.Documents)
            {
                SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                _semanticModel = await document.GetSemanticModelAsync();

                IEnumerable<ClassDeclarationSyntax> classes =
                    syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classNode in classes)
                {
                    if (!RoslynUtils.IsPublic(classNode)) continue;

                    ITypeSymbol swmrInterface = FindSwmrInterface(classNode);
                    if (swmrInterface == null)
                    {
                        continue;
                    }

                    var namespaceNode = classNode.Parent as NamespaceDeclarationSyntax;
                    if (namespaceNode == null)
                    {
                        throw new Exception("A grain must be declared inside a namespace");
                    }

                    usings.UnionWith(syntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(usingDirective => usingDirective.Name.ToString()));

                    int replicaCount = GetReadReplicaCount(swmrInterface);

                    NamespaceDeclarationSyntax namespaceDclr = SF.NamespaceDeclaration(SF.IdentifierName(namespaceNode.Name.ToString())).WithUsings(namespaceNode.Usings);

                    namespaceDclr = namespaceDclr.AddMembers(
                        GenerateWriteGrain(classNode, swmrInterface, replicaCount),
                        GenerateReadGrain(classNode, swmrInterface, replicaCount),
                        GenerateReadReplicaGrain(classNode, swmrInterface)
                        );

                    usings.UnionWith(namespaceNode.Usings.Select(@using => @using.Name.ToString()));

                    cu = cu.AddMembers(namespaceDclr);

                    // only copy the usings if at least one class was generated
                    copyUsings = true;
                }
            }

            if (copyUsings)
            {
                usings.UnionWith(GetCommonUsings());
            }
            return new CodeGenResult(Formatter.Format(cu, workspace).ToString(), usings);
        }

        private ClassDeclarationSyntax GenerateReadReplicaGrain(ClassDeclarationSyntax grainClass, ITypeSymbol swmrInterface)
        {
            string readReplicaGrainName = SwmrUtils.GetReadReplicaGrainName(grainClass.Identifier.Text);
            string readReplicaInterfaceName = SwmrUtils.GetReadReplicaInterfaceName(swmrInterface.Name);
            ClassDeclarationSyntax readReplicaGrain = GenerateClassSqueleton(readReplicaGrainName).WithBaseList(RoslynUtils.BaseList(new[] { "Grain", readReplicaInterfaceName }));

            string grainStateTypeName = FindGrainStateTypeName(grainClass);
            readReplicaGrain = readReplicaGrain.AddMembers(SF.FieldDeclaration(SF.VariableDeclaration(SF.ParseTypeName(grainStateTypeName), SF.SeparatedList(new[] { SF.VariableDeclarator(SF.Identifier("State")) }))).AddModifiers(SF.Token(SyntaxKind.PrivateKeyword)));

            readReplicaGrain = readReplicaGrain.AddMembers(GenerateReadReplicaOnActivateAsync(swmrInterface));

            foreach (ISymbol member in swmrInterface.GetMembers())
            {
                IMethodSymbol methodSymbol = member as IMethodSymbol;
                if (methodSymbol == null || !IsReadOnlyMethod(methodSymbol))
                {
                    continue;
                }
                MethodDeclarationSyntax methodImpl = RoslynUtils.FindImplementation(methodSymbol, grainClass);
                readReplicaGrain = readReplicaGrain.AddMembers(methodImpl.WithLeadingTrivia(SF.EndOfLine("")));
            }

            readReplicaGrain = readReplicaGrain.AddMembers(GenerateSetStateMethod(grainStateTypeName));

            return readReplicaGrain;
        }

        private static MethodDeclarationSyntax GenerateReadReplicaOnActivateAsync(ITypeSymbol swmrInterface)
        {
            SyntaxTree syntaxTree = SF.ParseSyntaxTree(string.Format(
    @"
        public override async Task OnActivateAsync()
        {{
            string grainId = this.GetPrimaryKeyString().Split('_')[0];
            {0} grain = GrainFactory.GetGrain<{0}>(grainId);
            await SetState(await grain.GetState());
            await base.OnActivateAsync();
        }}", swmrInterface.Name));
            return syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        }

        public MethodDeclarationSyntax GenerateSetStateMethod(string grainStateTypeName)
        {
            SyntaxTree syntaxTree = SF.ParseSyntaxTree(string.Format(
@"
        public Task SetState(IGrainState state)
        {{
            State = state as {0};
            return TaskDone.Done;
        }}", grainStateTypeName));
            return syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        }


        private static IEnumerable<string> GetCommonUsings()
        {
            return new[] { "System.Linq", "ETG.Orleans.Swmr", "System.Collections.Generic", "Orleans.Concurrency" };
        }

        private static ClassDeclarationSyntax GenerateReadGrain(ClassDeclarationSyntax grainClass, ITypeSymbol swmrInterface, int readReplicaCount)
        {
            string readGrainName = SwmrUtils.GetReadInterfaceName(grainClass.Identifier.Text);
            string readerInterfaceName = SwmrUtils.GetReadInterfaceName(swmrInterface.Name);
            ClassDeclarationSyntax readGrain = GenerateClassSqueleton(readGrainName).WithAttributeLists(AttributeUtils.AttributeListList(AttributeUtils.Attribute(StatelessWorkerAttributeName))).WithBaseList(RoslynUtils.BaseList(new[] { "Grain", readerInterfaceName }));
            readGrain = AddTopologyField(readGrain);
            readGrain = readGrain.AddMembers(GenerateOnActivateAsyncMethod(readReplicaCount));

            string readReplicaInterfaceName = SwmrUtils.GetReadReplicaInterfaceName(swmrInterface.Name);

            foreach (ISymbol member in swmrInterface.GetMembers())
            {
                IMethodSymbol methodSymbol = member as IMethodSymbol;
                if (methodSymbol == null || !IsReadOnlyMethod(methodSymbol))
                {
                    continue;
                }

                MethodInspector methodInspector = new MethodInspector(methodSymbol);
                string parameters = "string sessionId";
                if (methodInspector.MethodParams.Any())
                {
                    parameters = string.Join(", ", methodInspector.MethodParams.Select(param => string.Format("{0} {1}", param.Value, param.Key))) + " ," + parameters;
                }

                SyntaxTree syntaxTree = SF.ParseSyntaxTree(string.Format(
    @"
        public {0} {1}({2})
        {{
            string sessionNode = _topology.GetNode(sessionId);
            var readReplica = GrainFactory.GetGrain<{3}>(this.GetPrimaryKeyString() + ""_"" + sessionNode);
            return readReplica.{1}({4});
        }}", methodInspector.ReturnType, methodInspector.MethodName, parameters, readReplicaInterfaceName, string.Join(", ", methodInspector.MethodParams.Keys)));

                readGrain =
                    readGrain.AddMembers(
                        syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().WithLeadingTrivia(SF.EndOfLine("")));
            }
            return readGrain;
        }

        private static ClassDeclarationSyntax GenerateWriteGrain(ClassDeclarationSyntax grainClass, ITypeSymbol swmrInterface, int readReplicaCount)
        {
            string grainName = grainClass.Identifier.Text;
            string writerGrainName = SwmrUtils.GetWriteInterfaceName(grainName);
            string writerInterfaceName = SwmrUtils.GetWriteInterfaceName(swmrInterface.Name);
            ClassDeclarationSyntax writerGrain = GenerateClassSqueleton(writerGrainName).WithBaseList(RoslynUtils.BaseList(new[] { "Grain", writerInterfaceName }));

            writerGrain = AddTopologyField(writerGrain);
            writerGrain = writerGrain.AddMembers(GenerateOnActivateAsyncMethod(readReplicaCount));

            string readReplicaInterfaceName = SwmrUtils.GetReadReplicaInterfaceName(swmrInterface.Name);
            foreach (ISymbol member in swmrInterface.GetMembers())
            {
                IMethodSymbol methodSymbol = member as IMethodSymbol;
                if (methodSymbol == null || IsReadOnlyMethod(methodSymbol) || new MethodInspector(methodSymbol).MethodName == "GetState")
                {
                    continue;
                }

                MethodInspector methodInspector = new MethodInspector(methodSymbol);
                MethodDeclarationSyntax methodImpl = GenerateMethodDeclaration(methodInspector);
                methodImpl = SwmrUtils.AddSessionIdParameter(methodImpl).AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AsyncKeyword)).WithSemicolonToken(SF.Token(SyntaxKind.None));

                BlockSyntax statmentBlock = SF.Block();
                statmentBlock = AddStatement(statmentBlock, "string grainId = this.GetPrimaryKeyString();");
                statmentBlock = AddStatement(statmentBlock, string.Format("{0} grain = GrainFactory.GetGrain<{0}>(grainId);", swmrInterface.Name));
                statmentBlock = AddStatement(statmentBlock, String.Format("{0} await grain.{1}({2});", methodInspector.ReturnType != "Task"? "var result =" : "", methodInspector.MethodName, string.Join(", ", methodInspector.MethodParams.Keys)));
                statmentBlock = AddStatement(statmentBlock, "IGrainState state = await grain.GetState();");
                statmentBlock = AddStatement(statmentBlock, "string sessionNode = _topology.GetNode(sessionId);");
                statmentBlock = AddStatement(statmentBlock, "IEnumerable<string> otherNodes = _topology.Nodes.Where(node => node != sessionNode);");

                ForEachStatementSyntax forEachStatement = SF.ForEachStatement(
                    SF.PredefinedType(SF.Token(SyntaxKind.StringKeyword)),
                    SF.Identifier("node"),
                    SF.IdentifierName("otherNodes"),
                    SF.Block(SF.ParseStatement(GenerateSetStateStmt(readReplicaInterfaceName, @"grainId + ""_"" + node")))
                );

                statmentBlock = statmentBlock.AddStatements(forEachStatement);
                statmentBlock =
                    AddStatement(statmentBlock, (string.Format("{0} {1}", "await",
                            GenerateSetStateStmt(readReplicaInterfaceName, @"grainId + ""_"" + sessionNode"))));
                if (methodInspector.ReturnType != "Task")
                {
                    statmentBlock = AddStatement(statmentBlock, "return result;");
                }
                methodImpl = methodImpl.WithBody(statmentBlock);
                writerGrain = writerGrain.AddMembers(methodImpl);
            }

            return writerGrain;
        }

        private static BlockSyntax AddStatement(BlockSyntax statementBlock, string statement)
        {
            return statementBlock.AddStatements(SF.ParseStatement(statement + "\n"));
        }

        private static ClassDeclarationSyntax AddTopologyField(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.AddMembers(
                SF.FieldDeclaration(SF.VariableDeclaration(SF.ParseTypeName("ITopology"),
                    SF.SeparatedList(new[] { SF.VariableDeclarator(SF.Identifier("_topology")) })))
                    .AddModifiers(SF.Token(SyntaxKind.PrivateKeyword)));
        }

        private static MethodDeclarationSyntax GenerateOnActivateAsyncMethod(int readReplicaCount)
        {
            SyntaxTree syntaxTree = SF.ParseSyntaxTree(string.Format(
                @"
        public override async Task OnActivateAsync()
        {{
            _topology = new ConsistentHashRing({0});
            await base.OnActivateAsync();
        }}", readReplicaCount));
            return syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        }

        private static MethodDeclarationSyntax GenerateMethodDeclaration(MethodInspector methodInspector)
        {
            MethodDeclarationSyntax methodDclr = SF.MethodDeclaration(SF.ParseTypeName(methodInspector.ReturnType), SF.Identifier(methodInspector.MethodName));
            foreach (KeyValuePair<string, string> keyValuePair in methodInspector.MethodParams)
            {
                string paramType = keyValuePair.Value;
                string paramName = keyValuePair.Key;
                methodDclr = RoslynUtils.AppendParameterToMethod(methodDclr, RoslynUtils.CreateParameter(paramType, paramName));
            }
            return methodDclr;
        }

        private static bool IsReadOnlyMethod(IMethodSymbol methodSymbol)
        {
            return AttributeUtils.FindAttributeOfType(methodSymbol, typeof(ReadOnlyAttribute)) != null;
        }

        private static string GenerateSetStateStmt(string grainInterfaceName, string grainIdVariableName)
        {
            return string.Format("GrainFactory.GetGrain<{0}>({1}).SetState(state);", grainInterfaceName, grainIdVariableName);
        }

        private static ClassDeclarationSyntax GenerateClassSqueleton(string className)
        {

            return SF.ClassDeclaration(SF.Identifier(className)).AddModifiers(SF.Token(SyntaxKind.PublicKeyword));
        }

        private int GetReadReplicaCount(ITypeSymbol swmrInterface)
        {
            return Convert.ToInt32(FindSwrmAttribute(swmrInterface).NamedArguments.First(pair => pair.Key == ReadReplicaCountParamName).Value.Value);
        }

        private ITypeSymbol FindSwmrInterface(TypeDeclarationSyntax classNode)
        {
            if (classNode.BaseList != null)
            {
                foreach (BaseTypeSyntax type in classNode.BaseList.Types)
                {
                    TypeInfo typeInfo = _semanticModel.GetTypeInfo(type.Type);
                    ITypeSymbol typeSymbol = typeInfo.Type;
                    if (typeSymbol != null)
                    {
                        if (FindSwrmAttribute(typeSymbol) != null)
                        {
                            return typeSymbol;
                        }
                    }
                }
            }
            return null;
        }

        private string FindGrainStateTypeName(TypeDeclarationSyntax classNode)
        {
            if (classNode.BaseList != null)
            {
                foreach (BaseTypeSyntax type in classNode.BaseList.Types)
                {
                    TypeInfo typeInfo = _semanticModel.GetTypeInfo(type.Type);
                    INamedTypeSymbol typeSymbol = typeInfo.Type as INamedTypeSymbol;
                    if (typeSymbol != null)
                    {
                        if (typeSymbol.Name == "Grain")
                        {
                            return typeSymbol.TypeArguments.First().Name;
                        }
                    }
                }
            }
            return null;
        }

        private static AttributeData FindSwrmAttribute(ISymbol typeSymbol)
        {
            return AttributeUtils.FindAttributeOfType(typeSymbol, typeof(SingleWriterMultipleReadersAttribute));
        }
    }
}