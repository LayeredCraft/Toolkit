# AGENTS.md

This repository publishes the `LayeredCraft.Toolkit.*` package family. The first package is
`LayeredCraft.Toolkit.Functional`.

## Rules

- `Maybe<T>` and `Result<TValue, TError>` are C# 15 union types (`[Union]` + `IUnion`) with a nested
  `IUnionMembers` provider that declares the non-boxing access pattern. Keep the union contract and
  keep the struct storage: no `object` payload, no boxing on `switch`, `HasValue`, or `TryGetValue`.
- Keep the strongly typed `Value` / `Error` properties; the `object?` union view stays explicit.
- Keep `default(Maybe<T>)` equal to `None` and `default(Result<TValue, TError>)` uninitialized
  (the `null` union contents).
- Payload types are `notnull`. Reject `null` in factories, constructors, and union `Create` members.
- Keep the package dependency-free and Native AOT/trim clean.
- Put additional packages under `src/LayeredCraft.Toolkit.<Name>` in the same solution.
- Use conventional commits. Keep CI workflows pinned to one `LayeredCraft/devops-templates`
  revision.

## Commands

```bash
dotnet build LayeredCraft.Toolkit.slnx -c Release
dotnet test --solution LayeredCraft.Toolkit.slnx -c Release --no-build
dotnet run --project samples/LayeredCraft.Toolkit.Functional.Sample -c Release
```
