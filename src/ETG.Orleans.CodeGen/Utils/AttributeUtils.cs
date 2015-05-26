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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen.Utils
{
    using SF = SyntaxFactory;

    public static class AttributeUtils
    {
        public static AttributeSyntax SelectAttributeOfType(SyntaxList<AttributeListSyntax> attributeLists, Type type, SemanticModel semanticModel)
        {
            return attributeLists.SelectMany(
                attributeListSyntax => attributeListSyntax.Attributes)
                .FirstOrDefault(
                    attributeSyntax =>
                        new AttributeInspector(attributeSyntax, semanticModel).IsAttributeTypeOf(type));
        }

        public static SyntaxList<AttributeListSyntax> RemoveAttributeOfType(SyntaxList<AttributeListSyntax> attributeLists, Type type, SemanticModel semanticModel)
        {
            var newList = new List<AttributeListSyntax>();
            foreach (AttributeListSyntax attributeListSyntax in attributeLists)
            {
                var attributes = new List<AttributeSyntax>();
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    AttributeInspector attributeInspector = new AttributeInspector(attributeSyntax, semanticModel);
                    if (attributeInspector.IsAttributeTypeOf(type))
                    {
                        continue;
                    }
                    attributes.Add(attributeSyntax);
                }
                if (attributes.Any())
                {
                    newList.Add(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));
                }
            }
            return SyntaxFactory.List(newList);
        }

        public static AttributeData FindAttributeOfType(ISymbol typeSymbol, Type type)
        {
            foreach (AttributeData attributeData in typeSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass.ToString() ==
                    type.FullName)
                {
                    return attributeData;
                }
            }
            return null;
        }

        public static SyntaxList<AttributeListSyntax> AttributeListList(params AttributeSyntax[] attributes)
        {
            var list = new SyntaxList<AttributeListSyntax>();
            foreach (AttributeSyntax attributeSyntax in attributes)
            {
                list = list.Add(AttributeList(attributeSyntax));
            }
            return list;
        }

        public static AttributeListSyntax AttributeList(params AttributeSyntax[] attributes)
        {
            SeparatedSyntaxList<AttributeSyntax> separatedList = SF.SeparatedList<AttributeSyntax>();
            foreach (var attributeSyntax in attributes)
            {
                separatedList = separatedList.Add(attributeSyntax);
            }
            return SF.AttributeList(separatedList);
        }

        public static AttributeSyntax Attribute(string attributeName)
        {
            return SF.Attribute(SF.IdentifierName(attributeName));
        }

        public static AttributeSyntax AttributeWithArgument(string attributeName, params string[] attributeArguments)
        {
            return SF.Attribute(SF.IdentifierName(attributeName),
                        SF.AttributeArgumentList()
                            .WithArguments(SF.SeparatedList(attributeArguments.Select(arg => SF.AttributeArgument(SF.IdentifierName(arg))))));
        }
    }
}
