using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ETG.Orleans.CodeGen
{
    public class TypeInspector
    {
        public string ShortName { get; private set; }

        public TypeInspector(ITypeSymbol typeSymbol)
        {
            ShortName = typeSymbol.ToDisplayString();
            if (ShortName.Contains('.'))
            {
                ShortName = typeSymbol.Name;
            }

            if (typeSymbol is INamedTypeSymbol)
            {
                INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
                if (namedTypeSymbol.IsGenericType)
                {
                    ShortName += string.Format("<{0}>",
                        string.Join(", ", namedTypeSymbol.TypeArguments.Select(symbol => new TypeInspector(symbol).ShortName)));
                }
            }
        }

        public TypeInspector(TypeSyntax typeSyntax)
        {
            ShortName = typeSyntax.ToString();
        }
    }
}
