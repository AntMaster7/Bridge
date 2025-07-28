using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Bridge.Contract
{
    public class EmitterCache
    {
        public EmitterCache()
        {
            this.Members = new Dictionary<Tuple<ISymbol, bool, bool>, OverloadsCollection>();
            this.Nodes = new Dictionary<Tuple<SyntaxNode, bool>, OverloadsCollection>();
        }

        private Dictionary<Tuple<SyntaxNode, bool>, OverloadsCollection> Nodes
        {
            get;
            set;
        }

        private Dictionary<Tuple<ISymbol, bool, bool>, OverloadsCollection> Members
        {
            get;
            set;
        }

        public void AddNode(SyntaxNode syntaxNode, bool isSetter, OverloadsCollection overloads)
        {
            this.Nodes[Tuple.Create(syntaxNode, isSetter)] = overloads;
        }

        public bool TryGetNode(SyntaxNode syntaxNode, bool isSetter, out OverloadsCollection overloads)
        {
            return this.Nodes.TryGetValue(Tuple.Create(syntaxNode, isSetter), out overloads);
        }

        public void AddMember(ISymbol member, bool isSetter, bool includeInline, OverloadsCollection overloads)
        {
            this.Members[Tuple.Create(member, isSetter, includeInline)] = overloads;
        }

        public bool TryGetMember(ISymbol member, bool isSetter, bool includeInline, out OverloadsCollection overloads)
        {
            return this.Members.TryGetValue(Tuple.Create(member, isSetter, includeInline), out overloads);
        }
    }
}