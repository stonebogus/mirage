# Documenting the API

## Language and Style

- Write API documentation in English.
- Keep sentences brief, precise, and consistent across projects.
- Explain a symbol's responsibility in `<summary>`; do not restate its name just to create a link.
- Use `<remarks>` only for additional contracts that are not already clear from the summary.
- Use `<inheritdoc />` for overrides and interface implementations when the inherited contract applies. Add implementation-specific remarks only for meaningful differences.
- Avoid repeating the type's general behavior in every member comment.

## Coverage

- Document every public type and member, including enum values, interfaces, records, constructors, and protected members that define extension contracts.
- Add `<typeparam>` for every type parameter and `<param>` for every method, constructor, primary-constructor, and positional-record parameter.
- Add `<returns>` when the result needs clarification, such as nullability, lookup failure, or returned ownership.
- Add `<exception>` only for exceptions the implementation can actually throw and callers need to understand.
- Document constructors by stating what they initialize and describing their parameters. Do not repeat every initialized property's full description.
- Do not add XML documentation to private implementation details.

## XML References

- Use `<see cref="..."/>` when a link to another API symbol clarifies the contract. Verify that each reference resolves.
- Use `<see langword="null"/>`, `<see langword="true"/>`, and `<see langword="false"/>` for language keywords in prose.
- Use `<c>...</c>` for code identifiers, expressions, literals, and values that should not be linked.
- Avoid decorative links and self-references used only to repeat the symbol's own name.
- Keep XML tags correctly nested and closed. Use consistent `<inheritdoc />` formatting.

## Contracts and Concepts

- For options types, document each setting's purpose, default value, unit, and the meaning of `null` where applicable.
- Describe what a `Store`, `Signal`, or reactive collection represents. State notification timing and ordering when they are not obvious from the API.
- Use consistent terms for local and world positions, window and screen coordinates, radians, pixels, and application-defined coordinate units.
- State resource ownership explicitly: identify who creates, owns, borrows, and destroys each resource, and when native handles are valid.
- Keep ownership claims grounded in the implementation. Do not imply that a referenced object is owned or destroyed when the code only stores a reference.

## Comments and Verification

- Remove comments that merely narrate the next line of code.
- Keep ordinary comments for non-obvious implementation decisions; convert a comment to XML only when it describes a public contract.
- Do not change signatures, visibility, architecture, or behavior as part of a documentation-only change.
- Build the solution with XML documentation enabled and resolve all documentation warnings, including `CS1591`. Do not suppress them with `NoWarn` or disable `GenerateDocumentationFile`.
- Run the existing tests after editing documentation. Check the final diff to confirm that executable code was not changed.
