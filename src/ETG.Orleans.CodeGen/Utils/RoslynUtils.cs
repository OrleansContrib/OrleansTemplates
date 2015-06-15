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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen.Utils
{
    using SF = SyntaxFactory;

    public static class RoslynUtils
    {
        public static UsingDirectiveSyntax UsingDirective(string nameSpace)
        {
            return SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(nameSpace));
        }

        public static IEnumerable<MethodDeclarationSyntax> GetMethodDeclarations(TypeDeclarationSyntax interfaceNode)
        {
            return interfaceNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
        }

        public static ParameterSyntax CreateParameter(string type, string name)
        {
            return SF.Parameter(new SyntaxList<AttributeListSyntax>(), new SyntaxTokenList(), SF.IdentifierName(type), SF.Identifier(new SyntaxTriviaList().Add(SF.Space), name, new SyntaxTriviaList()), null);
        }

        public static MethodDeclarationSyntax AppendParameterToMethod(MethodDeclarationSyntax method,
            ParameterSyntax parameter)
        {
            if (method.ParameterList.Parameters.Any())
            {
                parameter = parameter.WithLeadingTrivia(SF.Space);
            }

            return method.WithParameterList(method.ParameterList.AddParameters(parameter));
        }

        public static bool IsPublic(BaseTypeDeclarationSyntax classNode)
        {
            return classNode.Modifiers.Any(modifier => modifier.RawKind.Equals(SF.Token(SyntaxKind.PublicKeyword).RawKind));
        }

        public static BaseListSyntax BaseList(IEnumerable<string> names)
        {
            return SF.BaseList(SF.SeparatedList<BaseTypeSyntax>(names.Select(name => SF.SimpleBaseType(SF.IdentifierName(name)))));
        }

        public static MethodDeclarationSyntax FindImplementation(IMethodSymbol methodSymbol, ClassDeclarationSyntax clazz)
        {
            foreach (MemberDeclarationSyntax member in clazz.Members)
            {
                if (member is MethodDeclarationSyntax)
                {
                    MethodDeclarationSyntax method = member as MethodDeclarationSyntax;
                    if (HasSameSignature(methodSymbol, method))
                    {
                        return method;
                    }
                }
            }
            return null;
        }

        public static bool HasSameSignature(IMethodSymbol methodSymbol, MethodDeclarationSyntax method)
        {
            return new MethodInspector(method).Equals(new MethodInspector(methodSymbol));
        }

        public static ClassDeclarationSyntax AddBase(ClassDeclarationSyntax type, string baseName)
        {
            if (type.BaseList == null)
            {
                type = type.WithBaseList(SF.BaseList());
            }
            return type.WithBaseList(AddBase(type.BaseList, baseName));
        }

        public static ClassDeclarationSyntax RemoveBase(ClassDeclarationSyntax type, string baseName)
        {
            if (type.BaseList == null)
            {
                return type;
            }
            return type.WithBaseList(RemoveBase(type.BaseList, baseName));
        }

        public static InterfaceDeclarationSyntax AddBase(InterfaceDeclarationSyntax type, string baseName)
        {
            return type.WithBaseList(AddBase(type.BaseList, baseName));
        }

        public static InterfaceDeclarationSyntax RemoveBase(InterfaceDeclarationSyntax type, string baseName)
        {
            return type.WithBaseList(RemoveBase(type.BaseList, baseName));
        }

        public static BaseListSyntax AddBase(BaseListSyntax baseList, string baseName)
        {
            baseList = baseList.AddTypes(SF.SimpleBaseType(SF.IdentifierName(baseName)));
            return baseList;
        }

        public static BaseListSyntax RemoveBase(BaseListSyntax baseList, string baseName)
        {
            BaseTypeSyntax baseType = baseList.Types.First(type => type.Type.ToString() == baseName);
            return SF.BaseList(baseList.Types.Remove(baseType));
        }
    }
}
