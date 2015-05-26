using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
