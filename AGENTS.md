# AGENTS.md

This repository publishes the `LayeredCraft.Toolkit.*` package family. The first package is
`LayeredCraft.Toolkit.Functional`.

## Rules

- Keep `Maybe<T>` and `Result<TValue, TError>` non-boxing `readonly struct` types. Do not add an
  `object` payload, a union interface, or an implicit interface conversion.
- Keep `default(Maybe<T>)` equal to `None` and `default(Result<TValue, TError>)` uninitialized.
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
