using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Bridge.Contract
{
    public interface IEmitter : ILog
    {
        string Tag
        {
            get;
            set;
        }

        IAssemblyInfo AssemblyInfo
        {
            get;
            set;
        }

        SyntaxKind AssignmentType
        {
            get;
            set;
        }

        SyntaxKind UnaryOperatorType
        {
            get;
            set;
        }

        bool IsUnaryAccessor
        {
            get;
            set;
        }

        IAsyncBlock AsyncBlock
        {
            get;
            set;
        }

        bool AsyncExpressionHandling
        {
            get;
            set;
        }

        SwitchStatementSyntax AsyncSwitch
        {
            get;
            set;
        }

        List<string> AsyncVariables
        {
            get;
            set;
        }

        bool Comma
        {
            get;
            set;
        }

        int CompareTypeInfosByName(ITypeInfo x, ITypeInfo y);

        int CompareTypeInfosByPriority(ITypeInfo x, ITypeInfo y);

        bool IsInheritedFrom(ITypeInfo x, ITypeInfo y);

        void SortTypesByInheritance();

        List<IPluginDependency> CurrentDependencies
        {
            get;
            set;
        }

        List<TranslatorOutputItem> Emit();

        bool EnableSemicolon
        {
            get;
            set;
        }

        AttributeData GetAttribute(IEnumerable<AttributeData> attributes, string name);

        CustomAttribute GetAttribute(IEnumerable<CustomAttribute> attributes, string name);

        TypeDefinition GetBaseMethodOwnerTypeDefinition(string methodName, int genericParamCount);

        TypeDefinition GetBaseTypeDefinition();

        TypeDefinition GetBaseTypeDefinition(TypeDefinition type);

        string GetEntityName(MemberDeclarationSyntax entity);

        string GetParameterName(ParameterSyntax entity);

        NameSemantic GetNameSemantic(ISymbol member);

        string GetEntityName(ISymbol member);

        string GetTypeName(INamedTypeSymbol type, TypeDefinition typeDefinition);

        string GetLiteralEntityName(ISymbol member);

        string GetInline(MemberDeclarationSyntax method);

        string GetInline(ISymbol entity);

        Tuple<bool, bool, string> GetInlineCode(InvocationExpressionSyntax node);

        Tuple<bool, bool, string> GetInlineCode(MemberAccessExpressionSyntax node);

        bool IsForbiddenInvocation(InvocationExpressionSyntax node);

        IEnumerable<string> GetScript(MemberDeclarationSyntax method);

        int GetPriority(TypeDefinition type);

        TypeDefinition GetTypeDefinition();

        TypeDefinition GetTypeDefinition(TypeSyntax reference, bool safe = false);

        TypeDefinition GetTypeDefinition(ITypeSymbol type);

        string GetTypeHierarchy();

        SyntaxNode IgnoreBlock
        {
            get;
            set;
        }

        bool IsAssignment
        {
            get;
            set;
        }

        bool IsAsync
        {
            get;
            set;
        }

        bool IsYield
        {
            get;
            set;
        }

        bool IsInlineConst(ISymbol member);

        bool IsMemberConst(ISymbol member);

        bool IsNativeMember(string fullName);

        bool IsNewLine
        {
            get;
            set;
        }

        int IteratorCount
        {
            get;
            set;
        }

        List<IJumpInfo> JumpStatements
        {
            get;
            set;
        }

        IWriterInfo LastSavedWriter
        {
            get;
            set;
        }

        int Level
        {
            get;
        }

        int InitialLevel
        {
            get;
        }

        int ResetLevel(int? level = null);

        InitPosition? InitPosition
        {
            get;
            set;
        }

        Dictionary<string, TypeSyntax> Locals
        {
            get;
            set;
        }

        Dictionary<ISymbol, string> LocalsMap //  ILocalSymbol and IParameterSymbol
        {
            get;
            set;
        }

        Dictionary<string, string> LocalsNamesMap
        {
            get;
            set;
        }

        Stack<Dictionary<string, TypeSyntax>> LocalsStack
        {
            get;
            set;
        }

        ILogger Log
        {
            get;
            set;
        }

        IEnumerable<MethodDefinition> MethodsGroup
        {
            get;
            set;
        }

        Dictionary<int, System.Text.StringBuilder> MethodsGroupBuilder
        {
            get;
            set;
        }

        SyntaxNode NoBraceBlock
        {
            get;
            set;
        }

        Action BeforeBlock
        {
            get;
            set;
        }

        System.Text.StringBuilder Output
        {
            get;
            set;
        }

        string SourceFileName
        {
            get;
            set;
        }

        int SourceFileNameIndex
        {
            get;
            set;
        }

        string LastSequencePoint
        {
            get;
            set;
        }

        IEmitterOutputs Outputs
        {
            get;
            set;
        }

        IEmitterOutput EmitterOutput
        {
            get;
            set;
        }

        IEnumerable<AssemblyDefinition> References
        {
            get;
            set;
        }

        bool ReplaceAwaiterByVar
        {
            get;
            set;
        }

        IMemberResolver Resolver
        {
            get;
            set;
        }

        bool SkipSemiColon
        {
            get;
            set;
        }

        IList<string> SourceFiles
        {
            get;
            set;
        }

        int ThisRefCounter
        {
            get;
            set;
        }

        string ToJavaScript(object value);

        IDictionary<string, TypeDefinition> TypeDefinitions
        {
            get;
        }

        ITypeInfo TypeInfo
        {
            get;
            set;
        }

        Dictionary<string, ITypeInfo> TypeInfoDefinitions
        {
            get;
            set;
        }

        List<ITypeInfo> Types
        {
            get;
            set;
        }

        IValidator Validator
        {
            get;
        }

        Stack<IWriter> Writers
        {
            get;
            set;
        }

        IVisitorException CreateException(SyntaxNode node);

        IVisitorException CreateException(SyntaxNode node, string message);

        IPlugins Plugins
        {
            get;
            set;
        }

        EmitterCache Cache
        {
            get;
        }

        string GetFieldName(FieldDeclarationSyntax field);

        string GetEventName(EventFieldDeclarationSyntax evt);

        Dictionary<string, bool> TempVariables
        {
            get;
            set;
        }

        Dictionary<string, string> NamedTempVariables
        {
            get;
            set;
        }

        Dictionary<string, bool> ParentTempVariables
        {
            get;
            set;
        }

        Tuple<bool, string> IsGlobalTarget(ISymbol member);

        BridgeTypes BridgeTypes
        {
            get;
            set;
        }

        ITranslator Translator
        {
            get;
            set;
        }

        void InitEmitter();

        IJsDoc JsDoc
        {
            get;
            set;
        }

        ITypeSymbol ReturnType
        {
            get;
            set;
        }

        string GetEntityNameFromAttr(ISymbol member, bool setter = false);

        bool ReplaceJump
        {
            get;
            set;
        }

        string CatchBlockVariable
        {
            get;
            set;
        }

        Dictionary<string, string> NamedFunctions
        {
            get; set;
        }

        Dictionary<ITypeSymbol, Dictionary<string, string>> NamedBoxedFunctions
        {
            get; set;
        }

        bool StaticBlock
        {
            get;
            set;
        }

        bool IsJavaScriptOverflowMode
        {
            get;
        }

        bool IsRefArg
        {
            get;
            set;
        }

        Dictionary<INamedTypeSymbol, IAnonymousTypeConfig> AnonymousTypes
        {
            get; set;
        }

        List<string> AutoStartupMethods
        {
            get;
            set;
        }

        bool IsAnonymousReflectable
        {
            get; set;
        }

        string MetaDataOutputName
        {
            get; set;
        }

        ITypeSymbol[] ReflectableTypes
        {
            get; set;
        }

        Dictionary<string, int> NamespacesCache
        {
            get; set;
        }

        bool DisableDependencyTracking { get; set; }

        void WriteIndented(string s, int? position = null);

        string GetReflectionName(ITypeSymbol type);

        bool ForbidLifting { get; set; }

        Dictionary<IAssemblySymbol, NameRule[]> AssemblyNameRuleCache
        {
            get;
        }

        Dictionary<INamedTypeSymbol, NameRule[]> ClassNameRuleCache
        {
            get;
        }

        Dictionary<IAssemblySymbol, CompilerRule[]> AssemblyCompilerRuleCache
        {
            get;
        }

        Dictionary<INamedTypeSymbol, CompilerRule[]> ClassCompilerRuleCache
        {
            get;
        }

        bool InConstructor { get; set; }

        CompilerRule Rules { get; set; }

        bool HasModules { get; set; }

        string TemplateModifier { get; set; }

        int WrapRestCounter { get; set; }
    }
}