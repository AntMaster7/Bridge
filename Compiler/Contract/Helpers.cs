using Bridge.Contract.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bridge.Contract
{
    public static partial class Helpers
    {
        public static void AcceptChildren(this SyntaxNode node, CSharpSyntaxVisitor visitor)
        {
            foreach (SyntaxNode child in node.ChildNodes())
            {
                visitor.Visit(child);
            }
        }

        public static void AcceptChildren<T>(this SyntaxNode node, CSharpSyntaxVisitor<T> visitor)
        {
            foreach (SyntaxNode child in node.ChildNodes())
            {
                visitor.Visit(child);
            }
        }

        public static void AcceptChildren(this SyntaxNode node, CSharpSyntaxWalker walker)
        {
            foreach (SyntaxNode child in node.ChildNodes())
            {
                walker.Visit(child);
            }
        }

        public static string ReplaceSpecialChars(string name)
        {
            return name.Replace('`', JS.Vars.D).Replace('/', '.').Replace("+", ".");
        }

        public static bool HasGenericArgument(GenericInstanceType type, TypeDefinition searchType, IEmitter emitter, bool deep)
        {
            foreach (var gArg in type.GenericArguments)
            {
                var orig = gArg;

                var gArgDef = gArg;
                if (gArgDef.IsGenericInstance)
                {
                    gArgDef = gArgDef.GetElementType();
                }

                TypeDefinition gTypeDef = null;
                try
                {
                    gTypeDef = Helpers.ToTypeDefinition(gArgDef, emitter);
                }
                catch
                {
                }

                if (gTypeDef == searchType)
                {
                    return true;
                }

                if (deep && gTypeDef != null && (Helpers.IsSubclassOf(gTypeDef, searchType, emitter) ||
                    (searchType.IsInterface && Helpers.IsImplementationOf(gTypeDef, searchType, emitter))))
                {
                    return true;
                }

                if (orig.IsGenericInstance)
                {
                    var result = Helpers.HasGenericArgument((GenericInstanceType)orig, searchType, emitter, deep);

                    if (result)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsTypeArgInSubclass(TypeDefinition thisTypeDefinition, TypeDefinition typeArgDefinition, IEmitter emitter, bool deep = true)
        {
            foreach (InterfaceImplementation interfaceImplementation in thisTypeDefinition.Interfaces)
            {
                var interfaceReference = interfaceImplementation.InterfaceType;
                var gBaseType = interfaceReference as GenericInstanceType;
                if (gBaseType != null && Helpers.HasGenericArgument(gBaseType, typeArgDefinition, emitter, deep))
                {
                    return true;
                }
            }

            if (thisTypeDefinition.BaseType != null)
            {
                TypeDefinition baseTypeDefinition = null;

                var gBaseType = thisTypeDefinition.BaseType as GenericInstanceType;
                if (gBaseType != null && Helpers.HasGenericArgument(gBaseType, typeArgDefinition, emitter, deep))
                {
                    return true;
                }

                try
                {
                    baseTypeDefinition = Helpers.ToTypeDefinition(thisTypeDefinition.BaseType, emitter);
                }
                catch
                {
                }

                if (baseTypeDefinition != null && deep)
                {
                    return Helpers.IsTypeArgInSubclass(baseTypeDefinition, typeArgDefinition, emitter);
                }
            }
            return false;
        }

        public static bool IsSubclassOf(TypeDefinition thisTypeDefinition, TypeDefinition typeDefinition, IEmitter emitter)
        {
            if (thisTypeDefinition.BaseType != null)
            {
                TypeDefinition baseTypeDefinition = null;

                try
                {
                    baseTypeDefinition = Helpers.ToTypeDefinition(thisTypeDefinition.BaseType, emitter);
                }
                catch
                {
                }

                if (baseTypeDefinition != null)
                {
                    return (baseTypeDefinition == typeDefinition || Helpers.IsSubclassOf(baseTypeDefinition, typeDefinition, emitter));
                }
            }
            return false;
        }

        public static bool IsImplementationOf(TypeDefinition thisTypeDefinition, TypeDefinition interfaceTypeDefinition, IEmitter emitter)
        {
            foreach (TypeReference interfaceReference in thisTypeDefinition.Interfaces)
            {
                var iref = interfaceReference;
                if (interfaceReference.IsGenericInstance)
                {
                    iref = interfaceReference.GetElementType();
                }

                if (iref == interfaceTypeDefinition)
                {
                    return true;
                }

                TypeDefinition interfaceDefinition = null;

                try
                {
                    interfaceDefinition = Helpers.ToTypeDefinition(iref, emitter);
                }
                catch
                {
                }

                if (interfaceDefinition != null && Helpers.IsImplementationOf(interfaceDefinition, interfaceTypeDefinition, emitter))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsAssignableFrom(TypeDefinition thisTypeDefinition, TypeDefinition typeDefinition, IEmitter emitter)
        {
            return (thisTypeDefinition == typeDefinition
                    || (typeDefinition.IsClass && !typeDefinition.IsValueType && Helpers.IsSubclassOf(typeDefinition, thisTypeDefinition, emitter))
                    || (typeDefinition.IsInterface && Helpers.IsImplementationOf(typeDefinition, thisTypeDefinition, emitter)));
        }

        public static TypeDefinition ToTypeDefinition(TypeReference reference, IEmitter emitter)
        {
            if (reference == null)
            {
                return null;
            }

            try
            {
                if (reference.IsGenericInstance)
                {
                    reference = reference.GetElementType();
                }

                if (emitter.TypeDefinitions.ContainsKey(reference.FullName))
                {
                    return emitter.TypeDefinitions[reference.FullName];
                }

                return reference.Resolve();
            }
            catch
            {
            }

            return null;
        }

        public static bool IsIgnoreGeneric(TypeDefinition type)
        {
            return type.CustomAttributes.Any(a => a.AttributeType.FullName == "Bridge.IgnoreGenericAttribute") || type.DeclaringType != null && Helpers.IsIgnoreGeneric(type.DeclaringType);
        }

        public static bool IsIgnoreGeneric(ITypeSymbol type, bool allowInTypeScript = false)
        {
            if (type is INamedTypeSymbol namedType)
            {
                var attrs = namedType.GetAttributes();
                var attr = attrs.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bridge.IgnoreGenericAttribute");

                if (attr != null)
                {
                    if (allowInTypeScript)
                    {
                        var allowInTsArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "AllowInTypeScript");
                        if (allowInTsArg.Key != null && allowInTsArg.Value.Value is bool allowValue)
                        {
                            return !allowValue;
                        }
                    }

                    return true;
                }

                return namedType.ContainingType != null && Helpers.IsIgnoreGeneric(namedType.ContainingType, allowInTypeScript);
            }

            // For other type symbols that aren't named types, check if they have an equivalent check
            // Type parameters don't have attributes in the same way, so we default to false
            return false;
        }

        public static bool IsIgnoreGeneric(ISymbol member, IEmitter emitter)
        {
            return emitter.Validator.HasAttribute(member.GetAttributes(), "Bridge.IgnoreGenericAttribute");
        }

        public static bool IsIgnoreGeneric(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method, IEmitter emitter)
        {
            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(method);
            if (symbolInfo is IMethodSymbol methodSymbol)
            {
                return Helpers.IsIgnoreGeneric(methodSymbol, emitter);
            }

            return false;
        }

        public static bool IsIgnoreCast(Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax typeSyntax, IEmitter emitter)
        {
            if (emitter.AssemblyInfo.IgnoreCast)
            {
                return true;
            }

            var typeInfo = emitter.Resolver.SemanticModel.GetTypeInfo(typeSyntax);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            var attrs = typeSymbol.GetAttributes();
            var ctorAttr = attrs.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bridge.ConstructorAttribute");

            if (ctorAttr != null && ctorAttr.ConstructorArguments.Length > 0)
            {
                var inline = ctorAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(inline) && Regex.IsMatch(inline, @"\s*\{\s*\}\s*"))
                {
                    return true;
                }
            }

            return attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.IgnoreCastAttribute") ||
                   attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.ObjectLiteralAttribute");
        }

        public static bool IsIgnoreCast(ITypeSymbol typeSymbol, IEmitter emitter)
        {
            if (emitter.AssemblyInfo.IgnoreCast)
            {
                return true;
            }

            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            var attrs = typeSymbol.GetAttributes();
            var ctorAttr = attrs.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bridge.ConstructorAttribute");

            if (ctorAttr != null && ctorAttr.ConstructorArguments.Length > 0)
            {
                var inline = ctorAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(inline) && Regex.IsMatch(inline, @"\s*\{\s*\}\s*"))
                {
                    return true;
                }
            }

            return attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.IgnoreCastAttribute");
        }

        public static bool IsIntegerType(ITypeSymbol type, Compilation compilation)
        {
            // Handle nullable types
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType)
            {
                type = namedType.TypeArguments[0];
            }

            return type.SpecialType == SpecialType.System_Byte
                || type.SpecialType == SpecialType.System_SByte
                || type.SpecialType == SpecialType.System_Char
                || type.SpecialType == SpecialType.System_Int16
                || type.SpecialType == SpecialType.System_UInt16
                || type.SpecialType == SpecialType.System_Int32
                || type.SpecialType == SpecialType.System_UInt32
                || type.SpecialType == SpecialType.System_Int64
                || type.SpecialType == SpecialType.System_UInt64;
        }

        public static bool IsInteger32Type(ITypeSymbol type, Compilation compilation)
        {
            // Handle nullable types
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType)
            {
                type = namedType.TypeArguments[0];
            }

            return type.SpecialType == SpecialType.System_Int32
                || type.SpecialType == SpecialType.System_UInt32;
        }

        public static bool IsFloatType(ITypeSymbol type, Compilation compilation)
        {
            // Handle nullable types
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType)
            {
                type = namedType.TypeArguments[0];
            }

            return type.SpecialType == SpecialType.System_Decimal
                || type.SpecialType == SpecialType.System_Double
                || type.SpecialType == SpecialType.System_Single;
        }

        public static bool IsDecimalType(ITypeSymbol type, Compilation compilation, bool allowArray = false)
        {
            return Helpers.IsKnownType(SpecialType.System_Decimal, type, compilation, allowArray);
        }

        public static bool IsKnownType(SpecialType specialType, ITypeSymbol type, Compilation compilation, bool allowArray = false)
        {
            if (allowArray && type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol arrayType)
            {
                type = arrayType.ElementType;
            }

            // Handle nullable types
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType)
            {
                type = namedType.TypeArguments[0];
            }

            return type.SpecialType == specialType;
        }

        public static bool IsLongType(ITypeSymbol type, Compilation compilation, bool allowArray = false)
        {
            return Helpers.IsKnownType(SpecialType.System_Int64, type, compilation, allowArray);
        }

        public static bool IsULongType(ITypeSymbol type, Compilation compilation, bool allowArray = false)
        {
            return Helpers.IsKnownType(SpecialType.System_UInt64, type, compilation, allowArray);
        }

        public static bool Is64Type(ITypeSymbol type, Compilation compilation, bool allowArray = false)
        {
            return Helpers.IsKnownType(SpecialType.System_UInt64, type, compilation, allowArray) || 
                   Helpers.IsKnownType(SpecialType.System_Int64, type, compilation, allowArray);
        }

        public static void CheckValueTypeClone(SyntaxNode syntaxNode, ITypeSymbol resolvedType, IAbstractEmitterBlock block, int insertPosition, IEmitter emitter)
        {
            if (resolvedType == null)
            {
                return;
            }

            if (block.Emitter.IsAssignment)
            {
                return;
            }

            // Get conversion information using Roslyn's semantic model
            var semanticModel = emitter.Resolver.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(syntaxNode);
            var conversion = semanticModel.GetConversion(syntaxNode);
            
            if (block.Emitter.Rules.Boxing == BoxingRule.Managed && (conversion.IsBoxing || conversion.IsUnboxing))
            {
                return;
            }

            bool writeClone = false;
            
            // Check if this is a method invocation
            if (syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocation)
            {
                bool ret = true;
                if (invocation.ArgumentList?.Arguments.Any(a => a.Expression == syntaxNode) == true)
                {
                    ret = false;
                }
                else if (syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax ||
                         syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax)
                {
                    ret = false;
                }
                else
                {
                    // Check for indexer property access
                    var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                    if (symbolInfo.Symbol is IPropertySymbol prop && prop.IsIndexer)
                    {
                        ret = false;
                        writeClone = true;
                    }
                }

                if (ret)
                {
                    return;
                }
            }

            var rrtype = resolvedType;
            var nullable = rrtype.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

            // Handle foreach element type
            if (syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax)
            {
                if (rrtype is IArrayTypeSymbol arrayType)
                {
                    rrtype = arrayType.ElementType;
                }
                else if (rrtype is INamedTypeSymbol namedType && namedType.IsGenericType)
                {
                    // Try to get the element type from IEnumerable<T>
                    var enumerableInterface = namedType.AllInterfaces
                        .FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                    if (enumerableInterface != null)
                    {
                        rrtype = enumerableInterface.TypeArguments[0];
                    }
                }
            }

            var type = nullable && rrtype is INamedTypeSymbol nullableType ? nullableType.TypeArguments[0] : rrtype;
            if (type.TypeKind == TypeKind.Struct)
            {
                if (Helpers.IsImmutableStruct(block.Emitter, type))
                {
                    return;
                }

                if (writeClone)
                {
                    Helpers.WriteClone(block, insertPosition, nullable);
                    return;
                }

                // Check for readonly field
                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                if (symbolInfo.Symbol is IFieldSymbol field && field.IsReadOnly)
                {
                    Helpers.WriteClone(block, insertPosition, nullable);
                    return;
                }

                var isOperator = false;
                if (syntaxNode != null &&
                    (syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax || 
                     syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.PrefixUnaryExpressionSyntax ||
                     syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.PostfixUnaryExpressionSyntax))
                {
                    var operatorSymbol = semanticModel.GetSymbolInfo(syntaxNode.Parent).Symbol as IMethodSymbol;
                    isOperator = operatorSymbol != null && operatorSymbol.MethodKind == MethodKind.UserDefinedOperator;
                }

                if (syntaxNode == null || isOperator ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InitializerExpressionSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax ||
                    syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax)
                {
                    if (syntaxNode != null && syntaxNode.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocationParent)
                    {
                        if (invocationParent.Expression == syntaxNode)
                        {
                            return;
                        }
                    }

                    Helpers.WriteClone(block, insertPosition, nullable);
                }
            }
        }

        private static void WriteClone(IAbstractEmitterBlock block, int insertPosition, bool nullable)
        {
            if (nullable)
            {
                block.Emitter.Output.Insert(insertPosition,
                    JS.Types.SYSTEM_NULLABLE + "." + JS.Funcs.Math.LIFT1 + "(\"" + JS.Funcs.CLONE + "\", ");
                block.WriteCloseParentheses();
            }
            else
            {
                block.Write("." + JS.Funcs.CLONE + "()");
            }
        }

        public static bool IsImmutableStruct(IEmitter emitter, ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Struct)
            {
                return true;
            }

            var typeDef = emitter.GetTypeDefinition(type);
            if (emitter.Validator.IsExternalType(typeDef) || emitter.Validator.IsImmutableType(typeDef))
            {
                return true;
            }

            // Get mutable fields using Roslyn's symbol model
            var mutableFields = type.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsReadOnly && !f.IsConst && !f.IsStatic);

            // Get auto properties using Roslyn symbols
            var autoProps = type.GetMembers().OfType<IPropertySymbol>()
                .Where(p => Helpers.IsAutoProperty(p, emitter));

            // Get auto events
            var autoEvents = type.GetMembers().OfType<IEventSymbol>();

            if (!mutableFields.Any() && !autoProps.Any() && !autoEvents.Any())
            {
                return true;
            }
            return false;
        }

        public static bool IsScript(IMethodSymbol method)
        {
            return method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.ScriptAttribute");
        }

        public static bool IsScript(MethodDefinition method)
        {
            return method.CustomAttributes.Any(a => a.AttributeType.FullName == CS.NS.BRIDGE + ".ScriptAttribute");
        }

        public static bool IsAutoProperty(IPropertySymbol propertySymbol, IEmitter emitter)
        {
            if (propertySymbol.GetMethod != null && Helpers.IsScript(propertySymbol.GetMethod))
            {
                return false;
            }

            if (propertySymbol.SetMethod != null && Helpers.IsScript(propertySymbol.SetMethod))
            {
                return false;
            }

            // Check if it's a compiler-generated auto property
            if (propertySymbol.GetMethod != null && 
                propertySymbol.GetMethod.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            {
                return true;
            }

            if (propertySymbol.SetMethod != null && 
                propertySymbol.SetMethod.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            {
                return true;
            }

            // Check for backing field pattern
            var containingType = propertySymbol.ContainingType;
            if (containingType != null)
            {
                var backingFieldName = $"<{propertySymbol.Name}>k__BackingField";
                var hasBackingField = containingType.GetMembers().OfType<IFieldSymbol>()
                    .Any(f => !f.IsPublic && !f.IsStatic && f.Name == backingFieldName);

                if (hasBackingField)
                {
                    return true;
                }
            }

            // For properties without accessors or with expression bodies, check if they don't have custom implementations
            return (propertySymbol.GetMethod != null && propertySymbol.GetMethod.IsAbstract) ||
                   (propertySymbol.SetMethod != null && propertySymbol.SetMethod.IsAbstract) ||
                   propertySymbol.IsAbstract;
        }

        public static bool IsAutoProperty(PropertyDefinition propDef)
        {
            if (propDef.GetMethod != null && Helpers.IsScript(propDef.GetMethod))
            {
                return false;
            }

            if (propDef.SetMethod != null && Helpers.IsScript(propDef.SetMethod))
            {
                return false;
            }

            if (propDef.GetMethod == null || propDef.SetMethod == null)
            {
                return false;
            }
            if (AttributeHelper.HasCompilerGeneratedAttribute(propDef.GetMethod))
            {
                return true;
            }

            var typeDef = propDef.DeclaringType;
            return typeDef != null && typeDef.Fields.Any(f => !f.IsPublic && !f.IsStatic && f.Name.Contains("BackingField") && f.Name.Contains("<" + propDef.Name + ">"));
        }

        public static string GetAddOrRemove(bool isAdd, string name = null)
        {
            return (isAdd ? JS.Funcs.Event.ADD : JS.Funcs.Event.REMOVE) + name;
        }

        public static string GetEventRef(Microsoft.CodeAnalysis.CSharp.Syntax.EventDeclarationSyntax eventDeclaration, IEmitter emitter, bool remove = false, bool noOverload = false, bool ignoreInterface = false, bool withoutTypeParams = false)
        {
            // Use semantic model to get symbol information
            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(eventDeclaration);
            if (symbolInfo is IEventSymbol eventSymbol)
            {
                return Helpers.GetEventRef(eventSymbol, emitter, remove, noOverload, ignoreInterface, withoutTypeParams);
            }

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, eventDeclaration, remove);
                return overloads.GetOverloadName(ignoreInterface, Helpers.GetAddOrRemove(!remove), withoutTypeParams);
            }

            var name = emitter.GetEntityName(eventDeclaration);
            return Helpers.GetAddOrRemove(!remove, name);
        }

        public static string GetEventRef(IEventSymbol eventSymbol, IEmitter emitter, bool remove = false, bool noOverload = false, bool ignoreInterface = false, bool withoutTypeParams = false, bool skipPrefix = false)
        {
            var attrName = emitter.GetEntityNameFromAttr(eventSymbol, remove);

            if (!String.IsNullOrEmpty(attrName))
            {
                return Helpers.AddInterfacePrefix(eventSymbol, emitter, ignoreInterface, attrName, remove);
            }

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, eventSymbol, remove);
                return overloads.GetOverloadName(ignoreInterface, skipPrefix ? null : Helpers.GetAddOrRemove(!remove), withoutTypeParams);
            }

            var name = emitter.GetEntityName(eventSymbol);
            return skipPrefix ? name : Helpers.GetAddOrRemove(!remove, name);
        }

        public static string GetSetOrGet(bool isSetter, string name = null)
        {
            return (isSetter ? JS.Funcs.Property.SET : JS.Funcs.Property.GET) + name;
        }

        public static string GetPropertyRef(Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false, bool withoutTypeParams = false, bool skipPrefix = true)
        {
            // Use semantic model to get symbol information
            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(property);
            if (symbolInfo is IPropertySymbol propertySymbol)
            {
                return Helpers.GetPropertyRef(propertySymbol, emitter, isSetter, noOverload, ignoreInterface, withoutTypeParams, skipPrefix);
            }

            string name;

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                return overloads.GetOverloadName(ignoreInterface, skipPrefix ? null : Helpers.GetSetOrGet(isSetter), withoutTypeParams);
            }

            name = emitter.GetEntityName(property);
            return skipPrefix ? name : Helpers.GetSetOrGet(isSetter, name);
        }

        public static string GetPropertyRef(Microsoft.CodeAnalysis.CSharp.Syntax.IndexerDeclarationSyntax property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            // Use semantic model to get symbol information
            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(property);
            if (symbolInfo is IPropertySymbol propertySymbol)
            {
                return Helpers.GetIndexerRef(propertySymbol, emitter, isSetter, noOverload, ignoreInterface);
            }

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                return overloads.GetOverloadName(ignoreInterface, Helpers.GetSetOrGet(isSetter));
            }

            var name = emitter.GetEntityName(property);
            return Helpers.GetSetOrGet(isSetter, name);
        }

        public static string GetPropertyRef(IPropertySymbol property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false, bool withoutTypeParams = false, bool skipPrefix = true)
        {
            var attrName = emitter.GetEntityNameFromAttr(property, isSetter);

            if (!String.IsNullOrEmpty(attrName))
            {
                return Helpers.AddInterfacePrefix(property, emitter, ignoreInterface, attrName, isSetter);
            }

            string name = null;

            if (property.IsIndexer)
            {
                skipPrefix = false;
            }

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                return overloads.GetOverloadName(ignoreInterface, skipPrefix ? null : Helpers.GetSetOrGet(isSetter), withoutTypeParams);
            }

            name = emitter.GetEntityName(property);
            return skipPrefix ? name : Helpers.GetSetOrGet(isSetter, name);
        }

        public static string GetIndexerRef(IPropertySymbol property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var attrName = emitter.GetEntityNameFromAttr(property, isSetter);

            if (!String.IsNullOrEmpty(attrName))
            {
                return Helpers.AddInterfacePrefix(property, emitter, ignoreInterface, attrName, isSetter);
            }

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                return overloads.GetOverloadName(ignoreInterface, Helpers.GetSetOrGet(isSetter));
            }

            var name = emitter.GetEntityName(property);
            return Helpers.GetSetOrGet(isSetter, name);
        }

        private static string AddInterfacePrefix(IEventSymbol eventSymbol, IEmitter emitter, bool ignoreInterface, string attrName, bool remove)
        {
            IEventSymbol interfaceMember = null;

            // Check for explicit interface implementation
            if (eventSymbol.ExplicitInterfaceImplementations.Any())
            {
                interfaceMember = eventSymbol.ExplicitInterfaceImplementations.First();
            }
            else if (eventSymbol.ContainingType != null && eventSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                interfaceMember = eventSymbol;
            }

            if (interfaceMember != null && !ignoreInterface)
            {
                return OverloadsCollection.GetInterfaceMemberName(emitter, interfaceMember, attrName, null, false, remove);
            }

            return attrName;
        }

        private static string AddInterfacePrefix(IPropertySymbol property, IEmitter emitter, bool ignoreInterface, string attrName, bool isSetter)
        {
            IPropertySymbol interfaceMember = null;

            // Check for explicit interface implementation
            if (property.ExplicitInterfaceImplementations.Any())
            {
                interfaceMember = property.ExplicitInterfaceImplementations.First();
            }
            else if (property.ContainingType != null && property.ContainingType.TypeKind == TypeKind.Interface)
            {
                interfaceMember = property;
            }

            if (interfaceMember != null && !ignoreInterface)
            {
                return OverloadsCollection.GetInterfaceMemberName(emitter, interfaceMember, attrName, null, false, isSetter);
            }

            return attrName;
        }

        public static List<MethodDefinition> GetMethods(TypeDefinition typeDef, IEmitter emitter, List<MethodDefinition> list = null)
        {
            if (list == null)
            {
                list = new List<MethodDefinition>(typeDef.Methods);
            }
            else
            {
                list.AddRange(typeDef.Methods);
            }

            var baseTypeDefinition = Helpers.ToTypeDefinition(typeDef.BaseType, emitter);

            if (baseTypeDefinition != null)
            {
                Helpers.GetMethods(baseTypeDefinition, emitter, list);
            }

            return list;
        }

        public static bool IsReservedWord(IEmitter emitter, string word)
        {
            if (emitter != null && (emitter.TypeInfo.JsName == word || emitter.TypeInfo.JsName.StartsWith(word + ".")))
            {
                return true;
            }
            return JS.Reserved.Words.Contains(word);
        }

        public static string ChangeReservedWord(string name)
        {
            return Helpers.PrefixDollar(name);
        }

        public static object GetEnumValue(IEmitter emitter, ITypeSymbol type, object constantValue)
        {
            var enumMode = Helpers.EnumEmitMode(type);

            if ((emitter.Validator.IsExternalType(emitter.GetTypeDefinition(type)) && enumMode == -1) || enumMode == 2)
            {
                return constantValue;
            }

            if (enumMode >= 3 && enumMode < 7)
            {
                var member = type.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.IsStatic && f.HasConstantValue && f.ConstantValue != null && f.ConstantValue.Equals(constantValue));

                if (member == null)
                {
                    return constantValue;
                }

                string enumStringName = member.Name;
                var attr = emitter.GetAttribute(member.GetAttributes(), "Bridge.NameAttribute");

                if (attr != null)
                {
                    enumStringName = emitter.GetEntityName(member);
                }
                else
                {
                    switch (enumMode)
                    {
                        case 3:
                            enumStringName = member.Name.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + member.Name.Substring(1);
                            break;

                        case 4:
                            break;

                        case 5:
                            enumStringName = enumStringName.ToLowerInvariant();
                            break;

                        case 6:
                            enumStringName = enumStringName.ToUpperInvariant();
                            break;
                    }
                }

                return enumStringName;
            }

            return constantValue;
        }

        public static string GetBinaryOperatorMethodName(SyntaxKind operatorType)
        {
            switch (operatorType)
            {
                case SyntaxKind.AmpersandToken:
                    return "op_BitwiseAnd";

                case SyntaxKind.BarToken:
                    return "op_BitwiseOr";

                case SyntaxKind.AmpersandAmpersandToken:
                    return "op_LogicalAnd";

                case SyntaxKind.BarBarToken:
                    return "op_LogicalOr";

                case SyntaxKind.CaretToken:
                    return "op_ExclusiveOr";

                case SyntaxKind.GreaterThanToken:
                    return "op_GreaterThan";

                case SyntaxKind.GreaterThanEqualsToken:
                    return "op_GreaterThanOrEqual";

                case SyntaxKind.EqualsEqualsToken:
                    return "op_Equality";

                case SyntaxKind.ExclamationEqualsToken:
                    return "op_Inequality";

                case SyntaxKind.LessThanToken:
                    return "op_LessThan";

                case SyntaxKind.LessThanEqualsToken:
                    return "op_LessThanOrEqual";

                case SyntaxKind.PlusToken:
                    return "op_Addition";

                case SyntaxKind.MinusToken:
                    return "op_Subtraction";

                case SyntaxKind.AsteriskToken:
                    return "op_Multiply";

                case SyntaxKind.SlashToken:
                    return "op_Division";

                case SyntaxKind.PercentToken:
                    return "op_Modulus";

                case SyntaxKind.LessThanLessThanToken:
                    return "op_LeftShift";

                case SyntaxKind.GreaterThanGreaterThanToken:
                    return "op_RightShift";

                case SyntaxKind.QuestionQuestionToken:
                    return null;

                default:
                    return null;
            }
        }

        public static string GetUnaryOperatorMethodName(SyntaxKind operatorType)
        {
            switch (operatorType)
            {
                case SyntaxKind.ExclamationToken:
                    return "op_LogicalNot";

                case SyntaxKind.TildeToken:
                    return "op_OnesComplement";

                case SyntaxKind.MinusToken:
                    return "op_UnaryNegation";

                case SyntaxKind.PlusToken:
                    return "op_UnaryPlus";

                case SyntaxKind.PlusPlusToken:
                    return "op_Increment";

                case SyntaxKind.MinusMinusToken:
                    return "op_Decrement";

                case SyntaxKind.AsteriskToken: // Dereference
                    return null;

                case SyntaxKind.AmpersandToken: // AddressOf
                    return null;

                case SyntaxKind.AwaitKeyword:
                    return null;

                default:
                    return null;
            }
        }

        public static SyntaxKind TypeOfAssignment(SyntaxKind operatorType)
        {
            switch (operatorType)
            {
                case SyntaxKind.EqualsToken:
                    return SyntaxKind.None; // Equivalent to Any

                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.PlusToken;

                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.MinusToken;

                case SyntaxKind.AsteriskEqualsToken:
                    return SyntaxKind.AsteriskToken;

                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.SlashToken;

                case SyntaxKind.PercentEqualsToken:
                    return SyntaxKind.PercentToken;

                case SyntaxKind.LessThanLessThanEqualsToken:
                    return SyntaxKind.LessThanLessThanToken;

                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return SyntaxKind.GreaterThanGreaterThanToken;

                case SyntaxKind.AmpersandEqualsToken:
                    return SyntaxKind.AmpersandToken;

                case SyntaxKind.BarEqualsToken:
                    return SyntaxKind.BarToken;

                case SyntaxKind.CaretEqualsToken:
                    return SyntaxKind.CaretToken;

                default:
                    return SyntaxKind.None;
            }
        }

        public static AttributeData GetInheritedAttribute(ISymbol symbol, string attrName)
        {
            if (symbol is IMethodSymbol || symbol is IPropertySymbol || symbol is IFieldSymbol || symbol is IEventSymbol)
            {
                return Helpers.GetInheritedAttribute((ISymbol)symbol as dynamic, attrName);
            }

            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }
            return null;
        }

        public static AttributeData GetInheritedAttribute(IMethodSymbol method, string attrName)
        {
            foreach (var attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }

            if (method.IsOverride)
            {
                var baseMember = method.OverriddenMethod;
                if (baseMember != null)
                {
                    return Helpers.GetInheritedAttribute(baseMember, attrName);
                }
            }
            else if (method.ExplicitInterfaceImplementations.Any())
            {
                foreach (var interfaceMember in method.ExplicitInterfaceImplementations)
                {
                    var attr = Helpers.GetInheritedAttribute(interfaceMember, attrName);
                    if (attr != null)
                    {
                        return attr;
                    }
                }
            }

            return null;
        }

        public static AttributeData GetInheritedAttribute(IPropertySymbol property, string attrName)
        {
            foreach (var attr in property.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }

            if (property.IsOverride)
            {
                var baseMember = property.OverriddenProperty;
                if (baseMember != null)
                {
                    return Helpers.GetInheritedAttribute(baseMember, attrName);
                }
            }
            else if (property.ExplicitInterfaceImplementations.Any())
            {
                foreach (var interfaceMember in property.ExplicitInterfaceImplementations)
                {
                    var attr = Helpers.GetInheritedAttribute(interfaceMember, attrName);
                    if (attr != null)
                    {
                        return attr;
                    }
                }
            }

            return null;
        }

        public static AttributeData GetInheritedAttribute(IEventSymbol eventSymbol, string attrName)
        {
            foreach (var attr in eventSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }

            if (eventSymbol.IsOverride)
            {
                var baseMember = eventSymbol.OverriddenEvent;
                if (baseMember != null)
                {
                    return Helpers.GetInheritedAttribute(baseMember, attrName);
                }
            }
            else if (eventSymbol.ExplicitInterfaceImplementations.Any())
            {
                foreach (var interfaceMember in eventSymbol.ExplicitInterfaceImplementations)
                {
                    var attr = Helpers.GetInheritedAttribute(interfaceMember, attrName);
                    if (attr != null)
                    {
                        return attr;
                    }
                }
            }

            return null;
        }

        public static AttributeData GetInheritedAttribute(IFieldSymbol field, string attrName)
        {
            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }

            // Fields don't have inheritance like methods/properties, so we just return null
            return null;
        }

        public static AttributeData GetInheritedAttribute(INamedTypeSymbol typeSymbol, string attrName)
        {
            foreach (var attr in typeSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attrName)
                {
                    return attr;
                }
            }

            var baseType = typeSymbol.BaseType;
            if (baseType != null && baseType.TypeKind != TypeKind.Interface)
            {
                return Helpers.GetInheritedAttribute(baseType, attrName);
            }

            return null;
        }

        public static CustomAttribute GetInheritedAttribute(IEmitter emitter, IMemberDefinition member, string attrName)
        {
            foreach (var attr in member.CustomAttributes)
            {
                if (attr.AttributeType.FullName == attrName)
                {
                    return attr;
                }
            }

            var methodDefinition = member as MethodDefinition;
            if (methodDefinition != null)
            {
                var isOverride = methodDefinition.IsVirtual && methodDefinition.IsReuseSlot;

                if (isOverride)
                {
                    member = Helpers.GetBaseMethod(methodDefinition, emitter);

                    if (member != null)
                    {
                        return Helpers.GetInheritedAttribute(emitter, member, attrName);
                    }
                }
            }

            return null;
        }

       public static string GetTypedArrayName(ITypeSymbol elementType)
        {
            if (elementType == null)
            {
                return null;
            }

            // Handle nullable types by getting the underlying type
            if (elementType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && elementType is INamedTypeSymbol namedType)
            {
                elementType = namedType.TypeArguments[0];
            }

            // Use SpecialType for built-in types instead of string comparison
            switch (elementType.SpecialType)
            {
                case SpecialType.System_Byte:
                    return JS.Types.Uint8Array;

                case SpecialType.System_SByte:
                    return JS.Types.Int8Array;

                case SpecialType.System_Int16:
                    return JS.Types.Int16Array;

                case SpecialType.System_UInt16:
                    return JS.Types.Uint16Array;

                case SpecialType.System_Int32:
                    return JS.Types.Int32Array;

                case SpecialType.System_UInt32:
                    return JS.Types.Uint32Array;

                case SpecialType.System_Single:
                    return JS.Types.Float32Array;

                case SpecialType.System_Double:
                    return JS.Types.Float64Array;

                default:
                    return null;
            }
        }

        public static string PrefixDollar(params object[] parts)
        {
            return JS.Vars.D + String.Join("", parts);
        }

        public static string ReplaceFirstDollar(string s)
        {
            if (s == null)
            {
                return s;
            }

            if (s.StartsWith(JS.Vars.D.ToString()))
            {
                return s.Substring(1);
            }

            return s;
        }

        public static bool IsNonScriptable(ISymbol type)
        {
            return Helpers.GetInheritedAttribute(type, "Bridge.NonScriptableAttribute") != null;
        }

        //public static bool IsNonScriptable(IEntity entity)
        //{
        //    return Helpers.GetInheritedAttribute(entity, "Bridge.NonScriptableAttribute") != null;
        //}

        public static bool IsEntryPointMethod(IEmitter emitter, Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
        {
            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (symbolInfo is IMethodSymbol methodSymbol)
            {
                return Helpers.IsEntryPointMethod(methodSymbol);
            }

            return false;
        }

        public static bool IsEntryPointMethod(IMethodSymbol method)
        {
            if (method != null && method.Name == CS.Methods.AUTO_STARTUP_METHOD_NAME &&
                method.IsStatic &&
                !method.IsAbstract &&
                Helpers.IsEntryPointCandidate(method))
            {
                bool isReady = false;
                foreach (var attr in method.GetAttributes())
                {
                    if (attr.AttributeClass?.ToDisplayString() == CS.Attributes.READY_ATTRIBUTE_NAME)
                    {
                        isReady = true;
                        break;
                    }
                }

                if (!isReady)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEntryPointCandidate(IEmitter emitter, Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration == null)
            {
                return false;
            }

            var symbolInfo = emitter.Resolver.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (symbolInfo is IMethodSymbol methodSymbol)
            {
                return Helpers.IsEntryPointCandidate(methodSymbol);
            }

            return false;
        }

        public static bool IsEntryPointCandidate(IMethodSymbol method)
        {
            if (method.Name != CS.Methods.AUTO_STARTUP_METHOD_NAME || !method.IsStatic || 
                method.ContainingType.TypeParameters.Length > 0 || method.TypeParameters.Length > 0)
            {
                // Must be a static, non-generic Main
                return false;
            }

            // Check return type: must be void, int, or async Task/Task<int>
            if (method.ReturnType.SpecialType != SpecialType.System_Void && 
                method.ReturnType.SpecialType != SpecialType.System_Int32)
            {
                // Check for async Task or Task<int>
                if (method.IsAsync)
                {
                    var returnTypeName = method.ReturnType.ToDisplayString();
                    if (returnTypeName == "System.Threading.Tasks.Task")
                    {
                        // async Task Main() is valid
                    }
                    else if (method.ReturnType is INamedTypeSymbol namedReturnType && 
                             namedReturnType.Name == "Task" && 
                             namedReturnType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
                             namedReturnType.TypeArguments.Length == 1 && 
                             namedReturnType.TypeArguments[0].SpecialType == SpecialType.System_Int32)
                    {
                        // async Task<int> Main() is valid
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // Check parameter count
            if (method.Parameters.Length == 0)
            {
                // Can have 0 parameters
                return true;
            }
            
            if (method.Parameters.Length > 1)
            {
                // May not have more than 1 parameter
                return false;
            }

            var parameter = method.Parameters[0];
            
            // The single parameter must not be ref or out
            if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
            {
                return false;
            }

            // The single parameter must be a one-dimensional array of strings
            if (parameter.Type is IArrayTypeSymbol arrayType)
            {
                return arrayType.Rank == 1 && arrayType.ElementType.SpecialType == SpecialType.System_String;
            }

            return false;
        }

        public static bool IsTypeParameterType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                var typeDef = namedType.OriginalDefinition;
                if (typeDef != null && Helpers.IsIgnoreGeneric(typeDef))
                {
                    return false;
                }
            }

            if (type is INamedTypeSymbol genericType && genericType.IsGenericType)
            {
                return genericType.TypeArguments.Any(Helpers.HasTypeParameters);
            }

            return false;
        }

        public static bool HasTypeParameters(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return true;
            }

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (typeArgument is INamedTypeSymbol argNamedType)
                    {
                        var typeDef = argNamedType.OriginalDefinition;
                        if (typeDef != null && Helpers.IsIgnoreGeneric(typeDef))
                        {
                            continue;
                        }
                    }

                    if (Helpers.HasTypeParameters(typeArgument))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Regex validIdentifier = new Regex("^[$A-Z_][0-9A-Z_$]*$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static bool IsValidIdentifier(string name)
        {
            return Helpers.validIdentifier.IsMatch(name);
        }

        public static int EnumEmitMode(ITypeSymbol type)
        {
            string enumAttr = "Bridge.EnumAttribute";
            int result = 7;

            var namedType = type as INamedTypeSymbol;
            if (namedType != null)
            {
                namedType.GetAttributes().Any(attr =>
                {
                    if (attr.AttributeClass?.ToDisplayString() == enumAttr && attr.ConstructorArguments.Length > 0)
                    {
                        result = (int)attr.ConstructorArguments.First().Value;
                        return true;
                    }

                    return false;
                });
            }

            return result;
        }

        public static bool IsValueEnum(ITypeSymbol type)
        {
            return Helpers.EnumEmitMode(type) == 2;
        }

        public static bool IsNameEnum(ITypeSymbol type)
        {
            var enumEmitMode = Helpers.EnumEmitMode(type);
            return enumEmitMode == 1 || enumEmitMode > 6;
        }

        public static bool IsStringNameEnum(ITypeSymbol type)
        {
            var mode = Helpers.EnumEmitMode(type);
            return mode >= 3 && mode <= 6;
        }

        public static bool IsReservedStaticName(string name, bool ignoreCase = true)
        {
            return JS.Reserved.StaticNames.Any(n => String.Equals(name, n, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture));
        }

        public static string GetFunctionName(NamedFunctionMode mode, ISymbol member, IEmitter emitter, bool isSetter = false)
        {
            var overloads = OverloadsCollection.Create(emitter, member, isSetter);
            string name = null;
            switch (mode)
            {
                case NamedFunctionMode.None:
                    break;
                case NamedFunctionMode.Name:
                    name = overloads.GetOverloadName(false, null, true);
                    break;
                case NamedFunctionMode.FullName:
                    var td = member.ContainingType;
                    name = td != null ? BridgeTypes.ToJsName(td, emitter, true) : "";
                    name = name.Replace(".", "_");
                    name += "_" + overloads.GetOverloadName(false, null, true);
                    break;
                case NamedFunctionMode.ClassName:
                    var t = member.ContainingType;
                    name = BridgeTypes.ToJsName(t, emitter, true, true);
                    name = name.Replace(".", "_");
                    name += "_" + overloads.GetOverloadName(false, null, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (name != null)
            {
                if (member is IPropertySymbol)
                {
                    name = name + "_" + (isSetter ? "set" : "get");
                }
                else if (member is IEventSymbol)
                {
                    name = name + "_" + (isSetter ? "remove" : "add");
                }
            }

            return name;
        }

        public static bool HasThis(string template)
        {
            return template.IndexOf("{this}", StringComparison.Ordinal) > -1 || template.IndexOf("{$}", StringComparison.Ordinal) > -1;
        }

        public static string ConvertTokens(IEmitter emitter, string template, ISymbol member)
        {
            string name = OverloadsCollection.Create(emitter, member).GetOverloadName(true);
            return template.Replace("{@}", name).Replace("{$}", "{this}." + name);
        }

        public static string ConvertNameTokens(string name, string replacer)
        {
            return name.Replace("{@}", replacer).Replace("{$}", replacer);
        }

        public static string ReplaceThis(IEmitter emitter, string template, string replacer, ISymbol member)
        {
            template = Helpers.ConvertTokens(emitter, template, member);
            return template.Replace("{this}", replacer);
        }

        public static string DelegateToTemplate(string tpl, IMethodSymbol method, IEmitter emitter)
        {
            bool addThis = !method.IsStatic;

            StringBuilder sb = new StringBuilder(tpl);
            sb.Append("(");

            bool comma = false;
            if (addThis)
            {
                sb.Append("{this}");
                comma = true;
            }

            if (!Helpers.IsIgnoreGeneric(method, emitter) && method.TypeArguments.Length > 0)
            {
                foreach (var typeParameter in method.TypeArguments)
                {
                    if (comma)
                    {
                        sb.Append(", ");
                    }

                    if (typeParameter.TypeKind == TypeKind.TypeParameter)
                    {
                        sb.Append("{");
                        sb.Append(typeParameter.Name);
                        sb.Append("}");
                    }
                    else
                    {
                        sb.Append(BridgeTypes.ToJsName(typeParameter, emitter));
                    }
                    comma = true;
                }
            }

            foreach (var parameter in method.Parameters)
            {
                if (comma)
                {
                    sb.Append(", ");
                }

                sb.Append("{");

                if (parameter.IsParams &&
                    method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Bridge.ExpandParamsAttribute"))
                {
                    sb.Append("*");
                }

                sb.Append(parameter.Name);
                sb.Append("}");
                comma = true;
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}