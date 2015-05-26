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
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen
{
    /// <summary>
    /// A helper class to get information about an Attribute parsed with Roslyn.
    /// </summary>
    public class AttributeInspector
    {
        private readonly  string _attributeName;
        private readonly string _attributeQualifiedName;
        private readonly IReadOnlyList<string> _unamedArguments;
        private readonly IReadOnlyDictionary<string, string> _namedArguments;

        public AttributeInspector(AttributeSyntax attributeSyntax, SemanticModel semanticModel)
        {
            _attributeName = attributeSyntax.Name.ToString();

            TypeInfo typeInfo = semanticModel.GetTypeInfo(attributeSyntax);
            ITypeSymbol typeSymbol = typeInfo.Type;
            _attributeQualifiedName = typeSymbol.ToString();
            

            var unamedArguments = new List<string>();
            var namedArguments = new Dictionary<string, string>();
            if (attributeSyntax.ArgumentList != null)
            {
                foreach (AttributeArgumentSyntax attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
                {
                    String argValue = attributeArgumentSyntax.Expression.ToString();
                    if (attributeArgumentSyntax.NameEquals != null)
                    {
                        string argName = attributeArgumentSyntax.NameEquals.Name.Identifier.Text;
                        namedArguments.Add(argName, argValue);
                    }
                    else
                    {
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (argValue.StartsWith("@"))
                        {
                            argValue = argValue.Remove(0, 1).Replace("\"\"", "\"");
                        }
                        else
                        {
                            argValue = Regex.Unescape(argValue);
                        }
                        argValue = argValue.Trim('"');
                        unamedArguments.Add(argValue);
                    }
                }
            }
            _unamedArguments = unamedArguments.AsReadOnly();
            _namedArguments = new ReadOnlyDictionary<string, string>(namedArguments);
        }

        public String AttributeName
        {
            get { return _attributeName; }
        }

        public string AttributeQualifiedName
        {
            get { return _attributeQualifiedName; }
        }

        public IReadOnlyList<string> UnamedArguments
        {
            get { return _unamedArguments; }
        }

        public IReadOnlyDictionary<string, string> NamedArguments
        {
            get { return _namedArguments; }
        }

        public bool IsAttributeTypeOf(Type type)
        {
            return AttributeQualifiedName == type.FullName;
        }
    }
}
