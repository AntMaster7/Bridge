using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Bridge.Contract
{
    public class TypeConfigItem
    {
        public string Name
        {
            get;
            set;
        }

        public MemberDeclarationSyntax Entity
        {
            get;
            set;
        }

        public ExpressionSyntax Initializer
        {
            get;
            set;
        }

        public VariableDeclaratorSyntax VarInitializer
        {
            get;
            set;
        }

        public bool IsConst
        {
            get;
            set;
        }

        public ISymbol InterfaceMember
        {
            get; set;
        }

        public ISymbol DerivedMember
        {
            get; set;
        }

        public bool IsPropertyInitializer
        {
            get; set;
        }

        public string GetName(IEmitter emitter, bool withoutTypeParams = false)
        {
            string fieldName = this.Name;

            if (this.VarInitializer != null)
            {
                var symbolInfo = emitter.Resolver.ResolveNode(this.VarInitializer, emitter);
                if (symbolInfo.Symbol != null)
                {
                    fieldName = OverloadsCollection.Create(emitter, symbolInfo.Symbol).GetOverloadName(false, null, withoutTypeParams);
                }
            }
            else if (this.Entity is PropertyDeclarationSyntax)
            {
                fieldName = OverloadsCollection.Create(emitter, (PropertyDeclarationSyntax)this.Entity, isField: true).GetOverloadName(false, null, withoutTypeParams);
            }
            else
            {
                if (this.Entity != null)
                {
                    var symbolInfo = emitter.Resolver.ResolveNode(this.Entity, emitter);
                    if (symbolInfo.Symbol != null)
                    {
                        fieldName = OverloadsCollection.Create(emitter, symbolInfo.Symbol).GetOverloadName(false, null, withoutTypeParams);
                    }
                }
            }
            return fieldName;
        }
    }

    public class TypeConfigInfo
    {
        public TypeConfigInfo()
        {
            this.Fields = new List<TypeConfigItem>();
            this.Events = new List<TypeConfigItem>();
            this.Properties = new List<TypeConfigItem>();
            this.Alias = new List<TypeConfigItem>();
            this.AutoPropertyInitializers = new List<TypeConfigItem>();
        }

        public bool HasMembers
        {
            get
            {
                return this.Fields.Count > 0 || this.Events.Count > 0 || this.Properties.Count > 0 || this.Alias.Count > 0;
            }
        }

        public bool HasConfigMembers
        {
            get
            {
                return this.Events.Count > 0 || this.Properties.Count > 0 || this.Alias.Count > 0;
            }
        }

        public List<TypeConfigItem> Fields
        {
            get;
            set;
        }

        public List<TypeConfigItem> Events
        {
            get;
            set;
        }

        public List<TypeConfigItem> Properties
        {
            get;
            set;
        }

        public List<TypeConfigItem> Alias
        {
            get;
            set;
        }

        public List<TypeConfigItem> AutoPropertyInitializers
        {
            get;
            set;
        }
    }
}