# Bridge NRefactory to Roslyn Migration Plan

## Overview
The Bridge.Contract project needs to be migrated from using the older NRefactory library to the newer Microsoft Roslyn Analyzer. This is a significant change that affects many files and requires careful planning.

## Steps

### Phase 1: Setup and Planning
1. Create a new branch specifically for this migration
2. Document all NRefactory dependencies and their Roslyn equivalents
3. Identify core interfaces and classes that need migration first

### Phase 2: Core Interface Migration
1. Begin with migrating the core interfaces:
   - IMemberResolver.cs
   - IAstVisitor.cs
   - IEmitter.cs
   - TypeConfigInfo.cs

2. Create compatibility/wrapper classes:
   - MemberResolveResult.cs
   - Create simplified versions of complex types for initial compilation

### Phase 3: Full Implementation
1. Migrate all helper classes systematically
2. Update all files referencing NRefactory
3. Replace all type references with Roslyn equivalents:
   - AstNode ? SyntaxNode
   - EntityDeclaration ? MemberDeclarationSyntax
   - VariableInitializer ? VariableDeclaratorSyntax
   - IEntity/IMember ? ISymbol
   - IType ? ITypeSymbol
   - etc.

### Phase 4: Testing and Refinement
1. Ensure the project compiles
2. Run tests to verify functionality
3. Fix issues as discovered
4. Document any breaking changes for downstream dependencies

## Key Roslyn Equivalents

| NRefactory Type | Roslyn Type |
|-----------------|-------------|
| AstNode | SyntaxNode |
| EntityDeclaration | MemberDeclarationSyntax |
| Expression | ExpressionSyntax |
| IEntity | ISymbol |
| IMethod | IMethodSymbol |
| IType | ITypeSymbol |
| AstType | TypeSyntax |
| TypeDeclaration | TypeDeclarationSyntax |
| ResolveResult | (Need custom compatibility layer) |
| CSharpAstResolver | SemanticModel |
| ICompilation | Compilation |