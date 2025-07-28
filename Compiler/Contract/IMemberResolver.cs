using Microsoft.CodeAnalysis;

namespace Bridge.Contract
{
    public interface IMemberResolver
    {
        SymbolInfo ResolveNode(SyntaxNode node, ILog log);

        SemanticModel SemanticModel
        {
            get;
        }

        Compilation Compilation
        {
            get;
        }
    }
}