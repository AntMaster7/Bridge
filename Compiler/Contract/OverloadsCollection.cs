using System;
using Bridge.Contract.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bridge.Contract
{
    /// <summary>
    /// The OverloadsCollection class manages and resolves overloaded members (methods, properties, events, fields, constructors, operators, indexers) within a type hierarchy when translating C# code to JavaScript. Since JavaScript doesn't have native method overloading like C#, Bridge needs to generate unique names for overloaded members.
    /// </summary>
    public class OverloadsCollection
    {
        public static OverloadsCollection Create(IEmitter emitter, FieldDeclarationSyntax fieldDeclaration)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(fieldDeclaration, false, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, fieldDeclaration);
        }

        public static OverloadsCollection Create(IEmitter emitter, EventFieldDeclarationSyntax eventDeclaration)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(eventDeclaration, false, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, eventDeclaration);
        }

        public static OverloadsCollection Create(IEmitter emitter, EventDeclarationSyntax eventDeclaration, bool remove)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(eventDeclaration, remove, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, eventDeclaration, remove);
        }

        public static OverloadsCollection Create(IEmitter emitter, MethodDeclarationSyntax methodDeclaration)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(methodDeclaration, false, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, methodDeclaration);
        }

        public static OverloadsCollection Create(IEmitter emitter, ConstructorDeclarationSyntax constructorDeclaration)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(constructorDeclaration, false, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, constructorDeclaration);
        }

        public static OverloadsCollection Create(IEmitter emitter, PropertyDeclarationSyntax propDeclaration, bool isSetter = false, bool isField = false)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(propDeclaration, isSetter, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, propDeclaration, isSetter, isField);
        }

        public static OverloadsCollection Create(IEmitter emitter, IndexerDeclarationSyntax indexerDeclaration, bool isSetter = false)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(indexerDeclaration, isSetter, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, indexerDeclaration, isSetter);
        }

        public static OverloadsCollection Create(IEmitter emitter, OperatorDeclarationSyntax operatorDeclaration)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetNode(operatorDeclaration, false, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, operatorDeclaration);
        }

        public static OverloadsCollection Create(IEmitter emitter, ISymbol member, bool isSetter = false, bool includeInline = false)
        {
            OverloadsCollection collection;

            if (emitter.Cache.TryGetMember(member, isSetter, includeInline, out collection))
            {
                return collection;
            }

            return new OverloadsCollection(emitter, member, isSetter, includeInline);
        }

        public IEmitter Emitter
        {
            get;
            private set;
        }

        public ITypeSymbol Type
        {
            get;
            private set;
        }

        public INamedTypeSymbol TypeDefinition
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string JsName
        {
            get;
            private set;
        }

        public string AltJsName
        {
            get;
            private set;
        }

        public string FieldJsName
        {
            get;
            private set;
        }

        public string ParametersCount
        {
            get;
            private set;
        }

        public bool Static
        {
            get;
            private set;
        }

        public bool Inherit
        {
            get;
            private set;
        }

        public bool Constructor
        {
            get;
            private set;
        }

        public bool IsSetter
        {
            get;
            private set;
        }

        public bool IncludeInline
        {
            get;
            private set;
        }

        public ISymbol Member
        {
            get;
            private set;
        }

        public ISymbol OriginalMember
        {
            get; set;
        }

        private OverloadsCollection(IEmitter emitter, FieldDeclarationSyntax fieldDeclaration)
        {
            this.Emitter = emitter;
            this.Name = emitter.GetFieldName(fieldDeclaration);
            this.JsName = this.Emitter.GetEntityName(fieldDeclaration);
            this.Inherit = !fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Static = fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(fieldDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(fieldDeclaration, false, this);
        }

        private OverloadsCollection(IEmitter emitter, EventFieldDeclarationSyntax eventDeclaration)
        {
            this.Emitter = emitter;
            this.Name = emitter.GetEventName(eventDeclaration);
            this.JsName = this.Emitter.GetEntityName(eventDeclaration);
            this.Inherit = !eventDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Static = eventDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(eventDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(eventDeclaration, false, this);
        }

        private OverloadsCollection(IEmitter emitter, EventDeclarationSyntax eventDeclaration, bool remove)
        {
            this.Emitter = emitter;
            this.Name = eventDeclaration.Identifier.ValueText;
            this.JsName = Helpers.GetEventRef(eventDeclaration, emitter, remove, true);
            this.AltJsName = Helpers.GetEventRef(eventDeclaration, emitter, !remove, true);
            this.FieldJsName = emitter.GetEntityName(eventDeclaration);
            this.Inherit = !eventDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.IsSetter = remove;
            this.Static = eventDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(eventDeclaration);
            this.FieldJsName = Helpers.GetEventRef((IEventSymbol)this.Member, emitter, true, true, true, false, true);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(eventDeclaration, remove, this);
        }

        private OverloadsCollection(IEmitter emitter, MethodDeclarationSyntax methodDeclaration)
        {
            this.Emitter = emitter;
            this.Name = methodDeclaration.Identifier.ValueText;
            this.JsName = this.Emitter.GetEntityName(methodDeclaration);
            this.Inherit = !methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Static = methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(methodDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(methodDeclaration, false, this);
        }

        private OverloadsCollection(IEmitter emitter, ConstructorDeclarationSyntax constructorDeclaration)
        {
            this.Emitter = emitter;
            this.Name = constructorDeclaration.Identifier.ValueText;
            this.JsName = this.Emitter.GetEntityName(constructorDeclaration);
            this.Inherit = false;
            this.Constructor = true;
            this.Static = constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(constructorDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(constructorDeclaration, false, this);
        }

        private OverloadsCollection(IEmitter emitter, PropertyDeclarationSyntax propDeclaration, bool isSetter, bool isField)
        {
            this.Emitter = emitter;
            this.IsField = isField;
            this.Name = propDeclaration.Identifier.ValueText;
            this.JsName = Helpers.GetPropertyRef(propDeclaration, emitter, isSetter, true, true);
            this.AltJsName = Helpers.GetPropertyRef(propDeclaration, emitter, !isSetter, true, true);
            this.FieldJsName = propDeclaration.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))?.Body == null ? emitter.GetEntityName(propDeclaration) : null;
            this.Inherit = !propDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Static = propDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.IsSetter = isSetter;
            this.Member = this.FindMember(propDeclaration);
            var p = (IPropertySymbol)this.Member;
            this.FieldJsName = this.Emitter.GetEntityName(p);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(propDeclaration, isSetter, this);
        }

        private OverloadsCollection(IEmitter emitter, IndexerDeclarationSyntax indexerDeclaration, bool isSetter)
        {
            this.Emitter = emitter;
            this.Name = "this";
            this.JsName = Helpers.GetPropertyRef(indexerDeclaration, emitter, isSetter, true, true);
            this.AltJsName = Helpers.GetPropertyRef(indexerDeclaration, emitter, !isSetter, true, true);
            this.Inherit = true;
            this.Static = false;
            this.IsSetter = isSetter;
            this.Member = this.FindMember(indexerDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(indexerDeclaration, isSetter, this);
        }

        private OverloadsCollection(IEmitter emitter, OperatorDeclarationSyntax operatorDeclaration)
        {
            this.Emitter = emitter;
            this.Name = operatorDeclaration.OperatorToken.ValueText;
            this.JsName = this.Emitter.GetEntityName(operatorDeclaration);
            this.Inherit = !operatorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Static = operatorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
            this.Member = this.FindMember(operatorDeclaration);
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.InitMembers();
            this.Emitter.Cache.AddNode(operatorDeclaration, false, this);
        }

        private OverloadsCollection(IEmitter emitter, ISymbol member, bool isSetter = false, bool includeInline = false, bool isField = false)
        {
            if (member is IMethodSymbol)
            {
                var method = (IMethodSymbol)member;
                this.Inherit = method.MethodKind != MethodKind.Constructor && !method.IsStatic;
                this.Static = method.IsStatic;
                this.Constructor = method.MethodKind == MethodKind.Constructor;
            }
            else
            {
                this.Inherit = !member.IsStatic;
                this.Static = member.IsStatic;
            }

            this.Emitter = emitter;
            this.Name = member.Name;
            this.IsField = isField;

            if (member is IPropertySymbol)
            {
                this.JsName = Helpers.GetPropertyRef((IPropertySymbol)member, emitter, isSetter, true, true);
                this.AltJsName = Helpers.GetPropertyRef((IPropertySymbol)member, emitter, !isSetter, true, true);
                var p = (IPropertySymbol)member;
                this.FieldJsName = this.Emitter.GetEntityName(p);
            }
            else if (member is IEventSymbol eventSymbol)
            {
                this.JsName = Helpers.GetEventRef(eventSymbol, emitter, isSetter, true, true);
                this.AltJsName = Helpers.GetEventRef(eventSymbol, emitter, !isSetter, true, true);
                this.FieldJsName = Helpers.GetEventRef(eventSymbol, emitter, true, true, true, false, true);
            }
            else
            {
                this.JsName = this.Emitter.GetEntityName(member);
            }

            this.IncludeInline = includeInline;
            this.Member = member;
            this.TypeDefinition = this.Member.ContainingType;
            this.Type = this.Member.ContainingType;
            this.IsSetter = isSetter;
            this.InitMembers();
            this.Emitter.Cache.AddMember(member, isSetter, includeInline, this);
        }

        public bool IsField
        {
            get;
            set;
        }

        public List<IMethodSymbol> Methods
        {
            get;
            private set;
        }

        public List<IFieldSymbol> Fields
        {
            get;
            private set;
        }

        public List<IPropertySymbol> Properties
        {
            get;
            private set;
        }

        public List<IEventSymbol> Events
        {
            get;
            private set;
        }

        public bool HasOverloads
        {
            get
            {
                return this.Members.Count > 1;
            }
        }

        protected virtual int GetIndex(ISymbol member)
        {
            var originalMember = member;

            while (member != null && member.IsOverride && !this.IsTemplateOverride(member))
            {
                member = Helpers.GetBaseMember(member);
            }

            if (member == null)
            {
                member = originalMember;
            }

            return this.Members.IndexOf(member.OriginalDefinition);
        }

        private List<ISymbol> members;

        public List<ISymbol> Members
        {
            get
            {
                this.InitMembers();
                return this.members;
            }
        }

        protected virtual void InitMembers()
        {
            if (this.members == null)
            {
                this.Properties = this.GetPropertyOverloads();
                this.Events = this.GetEventOverloads();
                this.Methods = this.GetMethodOverloads();
                this.Fields = this.GetFieldOverloads();

                this.members = new List<ISymbol>();
                this.members.AddRange(this.Methods);
                this.members.AddRange(this.Properties);
                this.members.AddRange(this.Fields);
                this.members.AddRange(this.Events);

                this.SortMembersOverloads();
            }
        }

        protected virtual void SortMembersOverloads()
        {
            this.Members.Sort((m1, m2) =>
            {
                if (!SymbolEqualityComparer.Default.Equals(m1.ContainingType, m2.ContainingType))
                {
                    return Helpers.IsSubclassOf(m1.ContainingType, m2.ContainingType) ? 1 : -1;
                }

                var iCount1 = GetExplicitInterfaceImplementationsCount(m1);
                var iCount2 = GetExplicitInterfaceImplementationsCount(m2);
                if (iCount1 > 0 && iCount2 == 0)
                {
                    return -1;
                }

                if (iCount2 > 0 && iCount1 == 0)
                {
                    return 1;
                }

                if (iCount1 > 0 && iCount2 > 0)
                {
                    var explicitInterfaces1 = GetExplicitInterfaceImplementations(m1);
                    var explicitInterfaces2 = GetExplicitInterfaceImplementations(m2);

                    foreach (var im1 in explicitInterfaces1)
                    {
                        foreach (var im2 in explicitInterfaces2)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(im1.ContainingType, im2.ContainingType))
                            {
                                if (Helpers.IsSubclassOf(im1.ContainingType, im2.ContainingType))
                                {
                                    return 1;
                                }

                                if (Helpers.IsSubclassOf(im2.ContainingType, im1.ContainingType))
                                {
                                    return -1;
                                }
                            }
                        }
                    }
                }

                var method1 = m1 as IMethodSymbol;
                var method2 = m2 as IMethodSymbol;

                if ((method1 != null && method1.MethodKind == MethodKind.Constructor) &&
                    (method2 == null || method2.MethodKind != MethodKind.Constructor))
                {
                    return -1;
                }

                if ((method2 != null && method2.MethodKind == MethodKind.Constructor) &&
                    (method1 == null || method1.MethodKind != MethodKind.Constructor))
                {
                    return 1;
                }

                if ((method1 != null && method1.MethodKind == MethodKind.Constructor) &&
                    (method2 != null && method2.MethodKind == MethodKind.Constructor))
                {
                    return string.Compare(this.MemberToString(m1), this.MemberToString(m2));
                }

                var a1 = this.GetAccessibilityWeight(m1.DeclaredAccessibility);
                var a2 = this.GetAccessibilityWeight(m2.DeclaredAccessibility);
                if (a1 != a2)
                {
                    return a1.CompareTo(a2);
                }

                var v1 = m1 is IFieldSymbol ? 1 : (m1 is IEventSymbol ? 2 : (m1 is IPropertySymbol ? 3 : (m1 is IMethodSymbol ? 4 : 5)));
                var v2 = m2 is IFieldSymbol ? 1 : (m2 is IEventSymbol ? 2 : (m2 is IPropertySymbol ? 3 : (m2 is IMethodSymbol ? 4 : 5)));

                if (v1 != v2)
                {
                    return v1.CompareTo(v2);
                }

                var name1 = this.MemberToString(m1);
                var name2 = this.MemberToString(m2);

                return string.Compare(name1, name2);
            });
        }

        private static int GetExplicitInterfaceImplementationsCount(ISymbol symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol method:
                    return method.ExplicitInterfaceImplementations.Length;
                case IPropertySymbol property:
                    return property.ExplicitInterfaceImplementations.Length;
                case IEventSymbol eventSymbol:
                    return eventSymbol.ExplicitInterfaceImplementations.Length;
                default:
                    return 0; // Fields and other symbols don't have explicit interface implementations
            }
        }

        private static IEnumerable<ISymbol> GetExplicitInterfaceImplementations(ISymbol symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol method:
                    return method.ExplicitInterfaceImplementations.Cast<ISymbol>();
                case IPropertySymbol property:
                    return property.ExplicitInterfaceImplementations.Cast<ISymbol>();
                case IEventSymbol eventSymbol:
                    return eventSymbol.ExplicitInterfaceImplementations.Cast<ISymbol>();
                default:
                    return Enumerable.Empty<ISymbol>(); // Fields and other symbols don't have explicit interface implementations
            }
        }

        protected virtual int GetAccessibilityWeight(Accessibility a)
        {
            int w = 0;
            switch (a)
            {
                case Accessibility.NotApplicable:
                    w = 4;
                    break;

                case Accessibility.Private:
                    w = 4;
                    break;

                case Accessibility.Public:
                    w = 1;
                    break;

                case Accessibility.Protected:
                    w = 3;
                    break;

                case Accessibility.Internal:
                    w = 2;
                    break;

                case Accessibility.ProtectedOrInternal:
                    w = 2;
                    break;

                case Accessibility.ProtectedAndInternal:
                    w = 3;
                    break;
            }

            return w;
        }

        protected virtual string MemberToString(ISymbol member)
        {
            if (member is IMethodSymbol)
            {
                return this.MethodToString((IMethodSymbol)member);
            }

            return member.Name;
        }

        protected virtual string MethodToString(IMethodSymbol m)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m.ReturnType.ToString()).Append(" ");
            sb.Append(m.Name).Append(" ");
            sb.Append(m.TypeParameters.Length).Append(" ");

            foreach (var p in m.Parameters)
            {
                sb.Append(p.Type.ToString()).Append(" ");
            }

            return sb.ToString();
        }

        public virtual bool IsTemplateOverride(ISymbol member)
        {
            if (member.IsOverride)
            {
                member = Helpers.GetBaseMember(member);

                if (member != null)
                {
                    var inline = this.Emitter.GetInline(member);
                    bool isInline = !string.IsNullOrWhiteSpace(inline);
                    if (isInline)
                    {
                        if (member.IsOverride)
                        {
                            return this.IsTemplateOverride(member);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual List<IMethodSymbol> GetMethodOverloads(List<IMethodSymbol> list = null, INamedTypeSymbol typeDef = null)
        {
            typeDef = typeDef ?? this.TypeDefinition;

            bool isTop = list == null;
            list = list ?? new List<IMethodSymbol>();
            var toStringOverride = (this.JsName == "toString" && this.Member is IMethodSymbol && ((IMethodSymbol)this.Member).Parameters.Length == 0);
            if (this.Member != null && this.Member.IsOverride && (!this.IsTemplateOverride(this.Member) || toStringOverride))
            {
                if (this.OriginalMember == null)
                {
                    this.OriginalMember = this.Member;
                }

                this.Member = Helpers.GetBaseMember(this.Member);
                typeDef = this.Member.ContainingType;
            }

            if (typeDef != null)
            {
                var isExternalType = typeDef.IsExtern;
                bool externalFound = false;

                var oldIncludeInline = this.IncludeInline;
                if (toStringOverride)
                {
                    this.IncludeInline = true;
                }

                var methods = typeDef.GetMembers().OfType<IMethodSymbol>().Where(m =>
                {
                    if (m.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return false;
                    }

                    if (!this.IncludeInline)
                    {
                        var inline = this.Emitter.GetInline(m);
                        if (!string.IsNullOrWhiteSpace(inline) && !(m.Name == "ToString" && m.Parameters.Length == 0 && !m.IsOverride))
                        {
                            return false;
                        }
                    }

                    var name = this.Emitter.GetEntityName(m);
                    if ((name == this.JsName || name == this.AltJsName || name == this.FieldJsName) && m.IsStatic == this.Static &&
                        (m.MethodKind == MethodKind.Constructor && this.JsName == JS.Funcs.CONSTRUCTOR || (m.MethodKind == MethodKind.Constructor) == this.Constructor))
                    {
                        if ((m.MethodKind == MethodKind.Constructor) != this.Constructor && (m.Parameters.Length > 0 || !SymbolEqualityComparer.Default.Equals(m.ContainingType, this.TypeDefinition)))
                        {
                            return false;
                        }

                        if (m.IsOverride && (!this.IsTemplateOverride(m) || m.Name == "ToString" && m.Parameters.Length == 0))
                        {
                            return false;
                        }

                        if (!isExternalType)
                        {
                            var isExtern = m.IsAbstract || this.Emitter.Validator.IsExternalType(m);
                            if (isExtern)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (externalFound)
                            {
                                return false;
                            }

                            externalFound = true;
                        }

                        return true;
                    }

                    return false;
                });

                this.IncludeInline = oldIncludeInline;

                list.AddRange(methods);

                if (this.Inherit)
                {
                    var baseTypes = Helpers.GetBaseTypesAndThis(typeDef).Where(t => t.TypeKind == typeDef.TypeKind || (typeDef.TypeKind == TypeKind.Struct && t.TypeKind == TypeKind.Class));

                    foreach (var baseTypeDef in baseTypes.Skip(1)) // Skip self
                    {
                        list = this.GetMethodOverloads(list, baseTypeDef);
                    }
                }
            }

            var returnMethods = isTop ? list.Distinct(SymbolEqualityComparer.Default).Cast<IMethodSymbol>().ToList() : list;
            return returnMethods;
        }

        protected virtual List<IPropertySymbol> GetPropertyOverloads(List<IPropertySymbol> list = null, INamedTypeSymbol typeDef = null)
        {
            typeDef = typeDef ?? this.TypeDefinition;

            bool isTop = list == null;
            list = list ?? new List<IPropertySymbol>();

            if (this.Member != null && this.Member.IsOverride && !this.IsTemplateOverride(this.Member))
            {
                if (this.OriginalMember == null)
                {
                    this.OriginalMember = this.Member;
                }

                this.Member = Helpers.GetBaseMember(this.Member);
                typeDef = this.Member.ContainingType;
            }

            if (typeDef != null)
            {
                bool isMember = this.Member is IMethodSymbol;
                var properties = typeDef.GetMembers().OfType<IPropertySymbol>().Where(p =>
                {
                    if (p.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return false;
                    }

                    var canGet = p.GetMethod != null;
                    var canSet = p.SetMethod != null;

                    if (!this.IncludeInline)
                    {
                        var inline = canGet ? this.Emitter.GetInline(p.GetMethod) : null;
                        if (!string.IsNullOrWhiteSpace(inline))
                        {
                            return false;
                        }

                        inline = canSet ? this.Emitter.GetInline(p.SetMethod) : null;
                        if (!string.IsNullOrWhiteSpace(inline))
                        {
                            return false;
                        }

                        if (p.IsIndexer && canGet && p.GetMethod.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.ExternalAttribute"))
                        {
                            return false;
                        }
                    }

                    bool eq = false;
                    bool? equalsByGetter = null;

                    if (p.IsStatic == this.Static)
                    {
                        var fieldName = this.Emitter.GetEntityName(p);

                        if (fieldName != null && (fieldName == this.JsName || fieldName == this.AltJsName || fieldName == this.FieldJsName))
                        {
                            eq = true;
                        }

                        if (!eq && p.IsIndexer)
                        {
                            var getterIgnore = canGet && p.IsExtern;
                            var setterIgnore = canSet && p.IsExtern;
                            var getterName = canGet ? Helpers.GetPropertyRef(p, this.Emitter, false, true, true) : null;
                            var setterName = canSet ? Helpers.GetPropertyRef(p, this.Emitter, true, true, true) : null;

                            if (!getterIgnore && getterName != null && (getterName == this.JsName || getterName == this.AltJsName || getterName == this.FieldJsName))
                            {
                                eq = true;
                                equalsByGetter = true;
                            }
                            else if (!setterIgnore && setterName != null && (setterName == this.JsName || setterName == this.AltJsName || setterName == this.FieldJsName))
                            {
                                eq = true;
                                equalsByGetter = false;
                            }
                        }
                    }

                    if (eq)
                    {
                        if (p.IsOverride && !this.IsTemplateOverride(p))
                        {
                            return false;
                        }

                        if (equalsByGetter.HasValue && isMember && this.AltJsName == null)
                        {
                            this.AltJsName = Helpers.GetPropertyRef(p, this.Emitter, equalsByGetter.Value, true, true);
                        }

                        return true;
                    }

                    return false;
                });

                list.AddRange(properties);

                if (this.Inherit)
                {
                    var baseTypes = Helpers.GetBaseTypesAndThis(typeDef).Where(t => t.TypeKind == typeDef.TypeKind || (typeDef.TypeKind == TypeKind.Struct && t.TypeKind == TypeKind.Class));

                    foreach (var baseTypeDef in baseTypes.Skip(1)) // Skip self
                    {
                        list = this.GetPropertyOverloads(list, baseTypeDef);
                    }
                }
            }

            var returnProperties = isTop ? list.Distinct(SymbolEqualityComparer.Default).Cast<IPropertySymbol>().ToList() : list;
            return returnProperties;
        }

        protected virtual List<IFieldSymbol> GetFieldOverloads(List<IFieldSymbol> list = null, INamedTypeSymbol typeDef = null)
        {
            typeDef = typeDef ?? this.TypeDefinition;

            bool isTop = list == null;
            list = list ?? new List<IFieldSymbol>();

            if (typeDef != null)
            {
                var fields = typeDef.GetMembers().OfType<IFieldSymbol>().Where(f =>
                {
                    if (GetExplicitInterfaceImplementationsCount(f) > 0)
                    {
                        return false;
                    }

                    var inline = this.Emitter.GetInline(f);
                    if (!string.IsNullOrWhiteSpace(inline))
                    {
                        return false;
                    }

                    var name = this.Emitter.GetEntityName(f);
                    if ((name == this.JsName || name == this.AltJsName || name == this.FieldJsName) && f.IsStatic == this.Static)
                    {
                        return true;
                    }

                    return false;
                });

                list.AddRange(fields);

                if (this.Inherit)
                {
                    var baseTypes = Helpers.GetBaseTypesAndThis(typeDef).Where(t => t.TypeKind == typeDef.TypeKind || (typeDef.TypeKind == TypeKind.Struct && t.TypeKind == TypeKind.Class));

                    foreach (var baseTypeDef in baseTypes.Skip(1)) // Skip self
                    {
                        list = this.GetFieldOverloads(list, baseTypeDef);
                    }
                }
            }

            var returnFields = isTop ? list.Distinct(SymbolEqualityComparer.Default).Cast<IFieldSymbol>().ToList() : list;
            return returnFields;
        }

        protected virtual List<IEventSymbol> GetEventOverloads(List<IEventSymbol> list = null, INamedTypeSymbol typeDef = null)
        {
            typeDef = typeDef ?? this.TypeDefinition;

            bool isTop = list == null;
            list = list ?? new List<IEventSymbol>();

            if (typeDef != null)
            {
                var events = typeDef.GetMembers().OfType<IEventSymbol>().Where(e =>
                {
                    if (e.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return false;
                    }

                    var inline = e.AddMethod != null ? this.Emitter.GetInline(e.AddMethod) : null;
                    if (!string.IsNullOrWhiteSpace(inline))
                    {
                        return false;
                    }

                    inline = e.RemoveMethod != null ? this.Emitter.GetInline(e.RemoveMethod) : null;
                    if (!string.IsNullOrWhiteSpace(inline))
                    {
                        return false;
                    }

                    bool eq = false;
                    bool? equalsByAdd = null;
                    if (e.IsStatic == this.Static)
                    {
                        var addName = e.AddMethod != null ? Helpers.GetEventRef(e, this.Emitter, false, true, true) : null;
                        var removeName = e.RemoveMethod != null ? Helpers.GetEventRef(e, this.Emitter, true, true, true) : null;
                        var fieldName = Helpers.GetEventRef(e, this.Emitter, true, true, true, false, true);
                        if (addName != null && (addName == this.JsName || addName == this.AltJsName || addName == this.FieldJsName))
                        {
                            eq = true;
                            equalsByAdd = true;
                        }
                        else if (removeName != null && (removeName == this.JsName || removeName == this.AltJsName || removeName == this.FieldJsName))
                        {
                            eq = true;
                            equalsByAdd = false;
                        }
                        else if (fieldName != null && (fieldName == this.JsName || fieldName == this.AltJsName || fieldName == this.FieldJsName))
                        {
                            eq = true;
                        }
                    }

                    if (eq)
                    {
                        if (e.IsOverride && !this.IsTemplateOverride(e))
                        {
                            return false;
                        }

                        if (equalsByAdd.HasValue && this.Member is IMethodSymbol && this.AltJsName == null)
                        {
                            this.AltJsName = Helpers.GetEventRef(e, this.Emitter, equalsByAdd.Value, true, true);
                        }

                        return true;
                    }

                    return false;
                });

                list.AddRange(events);

                if (this.Inherit)
                {
                    var baseTypes = Helpers.GetBaseTypesAndThis(typeDef).Where(t => t.TypeKind == typeDef.TypeKind || (typeDef.TypeKind == TypeKind.Struct && t.TypeKind == TypeKind.Class));

                    foreach (var baseTypeDef in baseTypes.Skip(1)) // Skip self
                    {
                        list = this.GetEventOverloads(list, baseTypeDef);
                    }
                }
            }

            var returnEvents = isTop ? list.Distinct(SymbolEqualityComparer.Default).Cast<IEventSymbol>().ToList() : list;
            return returnEvents;
        }

        private Dictionary<Tuple<bool, string, bool, bool>, string> overloadName = new Dictionary<Tuple<bool, string, bool, bool>, string>();

        public string GetOverloadName(bool skipInterfaceName = false, string prefix = null, bool withoutTypeParams = false, bool isObjectLiteral = false, bool excludeTypeOnly = false)
        {
            if (this.Member == null)
            {
                if (this.Members.Count == 1)
                {
                    this.Member = this.Members[0];
                }
                else
                {
                    return this.JsName;
                }
            }

            var key = new Tuple<bool, string, bool, bool>(skipInterfaceName, prefix, withoutTypeParams, isObjectLiteral);
            string name = null;
            var contains = this.overloadName.ContainsKey(key);
            if (!contains && this.Member != null)
            {
                name = this.GetOverloadName(this.Member, skipInterfaceName, prefix, withoutTypeParams, isObjectLiteral, excludeTypeOnly);
                this.overloadName[key] = name;
            }
            else if (contains)
            {
                name = this.overloadName[key];
            }

            return name;
        }

        public static string NormalizeInterfaceName(string interfaceName)
        {
            return Regex.Replace(interfaceName, @"[\.\(\)\,]", JS.Vars.D.ToString());
        }

        public static string GetInterfaceMemberName(IEmitter emitter, ISymbol interfaceMember, string name, string prefix, bool withoutTypeParams = false, bool isSetter = false, bool excludeTypeOnly = false)
        {
            var interfaceMemberName = name ?? OverloadsCollection.Create(emitter, interfaceMember, isSetter).GetOverloadName(true, prefix);
            var interfaceName = BridgeTypes.ToJsName(interfaceMember.ContainingType, emitter, false, false, true, withoutTypeParams, excludeTypeOnly: excludeTypeOnly);

            if (interfaceName.StartsWith("\""))
            {
                if (interfaceName.EndsWith(")"))
                {
                    return interfaceName + " + \"" + JS.Vars.D + interfaceMemberName + "\"";
                }

                if (interfaceName.EndsWith("\""))
                {
                    interfaceName = interfaceName.Substring(0, interfaceName.Length - 1);
                }

                return interfaceName + JS.Vars.D + interfaceMemberName + "\"";
            }

            return interfaceName + (interfaceName.EndsWith(JS.Vars.D.ToString()) ? "" : JS.Vars.D.ToString()) + interfaceMemberName;
        }

        public static bool ExcludeTypeParameterForDefinition(ISymbol member)
        {
            var explicitInterfaceImplementations = GetExplicitInterfaceImplementations(member);

            if (!explicitInterfaceImplementations.Any())
            {
                return false;
            }

            if (explicitInterfaceImplementations.Any(im =>
                {
                    var typeDef = im.ContainingType;
                    var type = im.ContainingType;

                    return typeDef != null && !Helpers.IsIgnoreGeneric(typeDef) && type != null &&
                           type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0 && Helpers.IsTypeParameterType(type);
                }))
            {
                return true;
            }

            return false;
        }

        public static bool NeedCreateAlias(ISymbol member)
        {
            var explicitInterfaceImplementations = GetExplicitInterfaceImplementations(member);
            
            if (member == null || !explicitInterfaceImplementations.Any())
            {
                return false;
            }

            if (explicitInterfaceImplementations.Any(im => im.ContainingType is INamedTypeSymbol namedType && namedType.TypeParameters.Any(tp => tp.Variance != VarianceKind.None)))
            {
                return true;
            }

            var explicitInterfaceMember = explicitInterfaceImplementations.First();
            var typeDef = explicitInterfaceMember.ContainingType;
            var type = explicitInterfaceMember.ContainingType;

            return typeDef != null && !Helpers.IsIgnoreGeneric(typeDef) && type != null && type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0 && Helpers.IsTypeParameterType(type);
        }

        protected virtual string GetOverloadName(ISymbol definition, bool skipInterfaceName = false, string prefix = null, bool withoutTypeParams = false, bool isObjectLiteral = false, bool excludeTypeOnly = false)
        {
            ISymbol interfaceMember = null;
            var explicitInterfaceImplementations = GetExplicitInterfaceImplementations(definition);
            
            if (explicitInterfaceImplementations.Any())
            {
                interfaceMember = explicitInterfaceImplementations.First();
            }
            else if (definition.ContainingType != null && definition.ContainingType.TypeKind == TypeKind.Interface)
            {
                interfaceMember = definition;
            }

            if (interfaceMember != null && !skipInterfaceName && !this.Emitter.Validator.IsObjectLiteral(interfaceMember.ContainingType))
            {
                return OverloadsCollection.GetInterfaceMemberName(this.Emitter, interfaceMember, null, prefix, withoutTypeParams, this.IsSetter, excludeTypeOnly);
            }

            string name = isObjectLiteral ? this.Emitter.GetLiteralEntityName(definition) : this.Emitter.GetEntityName(definition);
            if (name.StartsWith("." + JS.Funcs.CONSTRUCTOR))
            {
                name = JS.Funcs.CONSTRUCTOR;
            }

            var attr = Helpers.GetInheritedAttribute(definition, "Bridge.NameAttribute");

            var iProperty = definition as IPropertySymbol;

            if (attr == null && iProperty != null && !IsField)
            {
                var accessor = this.IsSetter ? iProperty.SetMethod : iProperty.GetMethod;

                if (accessor != null)
                {
                    attr = Helpers.GetInheritedAttribute(accessor, "Bridge.NameAttribute");

                    if (attr != null)
                    {
                        name = this.Emitter.GetEntityName(accessor);
                    }
                }
            }

            if (attr != null)
            {
                if (!(iProperty != null || definition is IEventSymbol))
                {
                    prefix = null;
                }
            }

            if (attr != null && explicitInterfaceImplementations.Any())
            {
                if (this.Members.Where(member => GetExplicitInterfaceImplementations(member).Any())
                        .Any(member => explicitInterfaceImplementations.Any(implementedInterfaceMember => GetExplicitInterfaceImplementations(member).Any(m => SymbolEqualityComparer.Default.Equals(m.ContainingType, implementedInterfaceMember.ContainingType)))))
                {
                    attr = null;
                }
            }

            bool skipSuffix = false;
            if (definition.ContainingType != null && definition.ContainingType.IsExtern)
            {
                if (definition.ContainingType.TypeKind == TypeKind.Interface)
                {
                    skipSuffix = definition.ContainingAssembly.Name != CS.NS.BRIDGE;
                }
                else
                {
                    skipSuffix = true;
                }
            }

            if (attr != null || skipSuffix)
            {
                return prefix != null ? prefix + name : name;
            }

            var iDefinition = definition as IMethodSymbol;
            var isCtor = iDefinition != null && iDefinition.MethodKind == MethodKind.Constructor;

            if (isCtor)
            {
                name = JS.Funcs.CONSTRUCTOR;
            }

            var index = this.GetIndex(definition);

            if (index > 0)
            {
                if (isCtor)
                {
                    name = JS.Vars.D + name + index;
                }
                else
                {
                    name += Helpers.PrefixDollar(index);
                    name = Helpers.ReplaceFirstDollar(name);
                }
            }

            return prefix != null ? prefix + name : name;
        }

        protected virtual ISymbol FindMember(SyntaxNode entity)
        {
            var symbolInfo = this.Emitter.Resolver.ResolveNode(entity, this.Emitter);
            return symbolInfo.Symbol;
        }
    }
}