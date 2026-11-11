# AGENTS.md

## Purpose

This repository contains `MockQueryable`, a .NET library for mocking `IQueryable<T>` and EF Core async query APIs in unit tests without a real database.

Use this file as the first-stop guide for future Codex tasks in this repo.

## Repository Layout

- `src/MockQueryable/MockQueryable.sln`
  The main solution.
- `src/MockQueryable/MockQueryable.Core`
  Core query-provider engine. Contains `TestQueryProvider<T, TExpressionVisitor>` and the default `TestExpressionVisitor`.
- `src/MockQueryable/MockQueryable.EntityFrameworkCore`
  EF Core integration layer. Adds `IAsyncEnumerable<T>`, `IAsyncQueryProvider`, async enumeration, and interception for `ExecuteUpdate` / `ExecuteDelete`.
- `src/MockQueryable/MockQueryable.Moq`
  Moq-specific extensions such as `BuildMockDbSet`.
- `src/MockQueryable/MockQueryable.NSubstitute`
  NSubstitute-specific extensions such as `BuildMockDbSet`.
- `src/MockQueryable/MockQueryable.FakeItEasy`
  FakeItEasy-specific extensions such as `BuildMockDbSet`.
- `src/MockQueryable/MockQueryable.Sample`
  Sample usage plus the test suite. Tests are written with NUnit and exercise all supported mocking libraries.
- `docs/ARCHITECTURE.md`
  Longer architecture explainer.
- `.github/workflows`
  CI and release workflows.

## Architecture

High-level flow:

1. Tests start from an in-memory `ICollection<T>` or `List<T>`.
2. `BuildMock()` or `BuildMockDbSet()` wraps that collection in custom query types.
3. `MockQueryable.Core` captures and executes LINQ expression trees in memory.
4. `MockQueryable.EntityFrameworkCore` provides EF Core-compatible async execution and async enumeration.
5. Optional custom `ExpressionVisitor` implementations rewrite provider-specific calls like `Like` / `ILike`.
6. Mocking-library adapters expose the same behavior through Moq, NSubstitute, and FakeItEasy.

Important implementation details:

- `TestQueryProvider<T, TExpressionVisitor>` is the core execution engine. It rewrites expressions with `TExpressionVisitor`, compiles them, and executes them against in-memory data.
- `TestAsyncEnumerableEfCore<T, TExpressionVisitor>` is the EF Core bridge. It implements `IAsyncEnumerable<T>` and `IAsyncQueryProvider`.
- `ExecuteUpdateAsync` and `ExecuteDeleteAsync` are simulated in memory by intercepting those method calls and mutating/removing items from the backing collection.
- `MockQueryableExtensions.BuildMock()` intentionally lives in namespace `MockQueryable`, not `MockQueryable.EntityFrameworkCore`, to preserve compatibility with earlier extension method locations.

## Build Commands

Run from repository root `C:\Project2\MockQueryable`.

Restore:

```powershell
dotnet restore src\MockQueryable\MockQueryable.sln
```

Build the solution in Release:

```powershell
dotnet build src\MockQueryable\MockQueryable.sln --configuration Release --no-restore
```

Pack NuGet packages:

```powershell
dotnet pack src\MockQueryable\MockQueryable.sln --configuration Release --no-build --output .\artifacts
```

Notes:

- CI uses .NET SDK `11.0.x`.
- All projects currently target `net11.0`.
- Library projects have `GeneratePackageOnBuild=true`, so `dotnet build` also produces package artifacts.

## Test Commands

Run the full test suite:

```powershell
dotnet test src\MockQueryable\MockQueryable.sln --configuration Release --no-build
```

Run tests with coverage, matching CI behavior:

```powershell
dotnet test src\MockQueryable\MockQueryable.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutput=TestResults\coverage.opencover.xml /p:CoverletOutputFormat=opencover
```

Run only the sample/test project:

```powershell
dotnet test src\MockQueryable\MockQueryable.Sample\MockQueryable.Sample.csproj --configuration Release
```

## Coding Conventions

Follow the existing codebase rather than introducing a new style system.

- Language/runtime:
  Use modern C# compatible with `.NET 11`.
- Namespaces:
  Prefer file-scoped namespaces where the surrounding project already uses them. Some older files still use block-scoped namespaces; preserve local style when editing.
- Usings:
  Keep explicit `using` directives. There is no repo-level config enabling implicit usings.
- Typing:
  `var` is used when the type is obvious; explicit types are also common. Match nearby code.
- Formatting:
  Use 4-space indentation and keep braces/spacing consistent with the file you are editing.
- Comments:
  Keep comments sparse and only where behavior is non-obvious.
- Tests:
  NUnit is the test framework. Test names are descriptive and commonly use patterns like `Method_Scenario_ExpectedResult`.
- API design:
  Keep the public surface small and framework-agnostic where possible. Mocking-library-specific behavior belongs in the adapter projects, not in `Core`.

## Dangerous Areas To Avoid

- `TestQueryProvider<T, TExpressionVisitor>`:
  Small changes here affect all query execution paths. Be careful with expression compilation, deferred execution, and provider creation.
- `TestAsyncEnumerableEfCore<T, TExpressionVisitor>`:
  This class simulates EF Core behavior with reflection and expression inspection. Changes can silently break async operators, `ExecuteDeleteAsync`, or `ExecuteUpdateAsync`.
- Namespace placement of `BuildMock()`:
  The extension method in `MockQueryable.EntityFrameworkCore/MockQueryableExtensions.cs` is intentionally declared under `namespace MockQueryable` for backward compatibility. Do not "clean this up" without treating it as a breaking API change.
- Expression visitor behavior:
  The sample `SampleLikeExpressionVisitor` demonstrates provider-specific rewriting. Changes to visitor flow can break `Like` / `ILike` simulation and other custom visitor scenarios.
- Backing collection mutation:
  `BuildMockDbSet()` implementations rely on the original `ICollection<T>` remaining mutable so delete/update behavior can reflect back into the source list.
- Cross-framework parity:
  Moq, NSubstitute, and FakeItEasy adapters should behave consistently. When changing one adapter, review the others.
- Packaging metadata:
  Version, package identity, README/license packaging, and icons are duplicated across multiple `.csproj` files. Keep them aligned.


## Practical Guidance For Codex Tasks

- Read `docs/ARCHITECTURE.md` before changing core behavior.
- For feature work, start by identifying whether the change belongs in:
  `Core`, `EntityFrameworkCore`, or one of the mocking adapters.
- For bug fixes involving query behavior, add or update tests in `MockQueryable.Sample` first.
- For API changes, check whether README examples and all three mocking adapters need updates.
- Avoid broad refactors across all projects unless the task explicitly asks for them.

## Verification Expectations

Before finishing a task when possible:

1. Run the most targeted `dotnet test` command that covers the change.
2. If public API or packaging changed, run solution-level `dotnet build` or `dotnet pack`.
3. If build/test is blocked by the current `obj/` write issue, report that clearly instead of guessing.


## Public API rules

- This is a NuGet library.
- Do not rename public types, methods, namespaces, or packages unless explicitly requested.
- Preserve backward compatibility.
- Any behavioral change must include tests.
- Prefer additive changes over breaking changes.


## Test coverage tasks

When asked to increase coverage:
- Prefer adding tests for existing public behavior.
- Do not change production code unless explicitly required.
- Add regression tests around async query execution and provider-specific behavior.
- Keep tests focused and readable.
- Avoid snapshot-style tests unless already used.
- Do not run `dotnet build` or `dotnet test`.

## Documentation changes

- Do not create, edit, or delete README files or any other project documentation without the user's explicit approval for those documentation changes.
- Authorization to change code, dependencies, build settings, or CI does not authorize documentation changes. Propose the documentation changes and obtain approval first.
- This rule also applies to AGENTS.md. Apply documentation changes directly when the user has explicitly requested or already approved them.
