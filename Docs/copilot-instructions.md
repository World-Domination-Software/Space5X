# Copilot Instructions – Space5X

## 1. Purpose

This project is a teaching-friendly space strategy game. All code should be **dead simple, readable, and approachable for new programmers**. Clarity always beats cleverness.

These instructions are for **GitHub Copilot and all AI coding agents** (and anyone else writing code here).
Any AI tool that generates or edits code in this repository should read and follow this file and all documents in the `Docs` folder.

## 2. Always Read the Design Docs First

Before generating or changing code, Copilot should:

- Treat everything in the `Docs` folder as the **source of truth** for game goals and design.
- Use `GDD.md` (Game Design Document) to understand:
  - the galaxy structure,
  - the Big Bang generator,
  - fuel / mass / impulse / warp rules,
  - factions, economy, and progression.
- Use `FishNet-Networking-Reference.md` to understand:
  - how we use FishNet for networking,
  - AOI and sector loading rules,
  - recommended transports and deployment patterns.
- Prefer to align new code with the concepts and names used in these documents.
- When working on networking code, also read the FishNet demo scripts under `Assets/FishNet/Demos` to see concrete examples of correct API usage and patterns.

If the design is unclear or missing, **prefer simple placeholders** and comments that explain assumptions.

## 3. Coding Style – Keep It Extremely Simple

When Copilot writes or edits code, it must:

- Prefer **straightforward, imperative code** over abstractions.
- Use **very clear and descriptive names** for classes, methods, variables, and fields.
- Favor **small, single-purpose methods** over large clever ones.
- Avoid fancy language features unless absolutely needed:
  - Avoid deep inheritance trees and complicated patterns.
  - Avoid clever LINQ chains; use simple loops instead.
  - Avoid unnecessary generics and reflection.
- Prefer **clear control flow**:
  - Simple `if` / `else` blocks.
  - Early returns to keep nesting shallow.
  - Minimal magic numbers; name constants instead.
- Optimize for **readability over performance** unless performance is explicitly called out in a doc.

If a simpler but slightly more verbose solution makes the intent clearer for a new programmer, **choose the simpler solution**.

## 4. Unity and C# Conventions

- Follow normal Unity patterns (MonoBehaviours, ScriptableObjects, etc.) but keep logic small and focused.
- Keep public APIs clear and minimal.
- Prefer composition over inheritance in gameplay code, but do it in a simple way.
- Use C# naming conventions:
  - `PascalCase` for classes, methods, and public properties.
  - `camelCase` for local variables and private fields (optionally `_camelCase` for private fields).
- Do not introduce new third-party libraries without being explicitly asked to.

## 5. Commenting Requirements

This project is meant for newer programmers. Copilot must **over-explain** code with comments.

For all new or modified code:

- **Every function / method** gets a short comment explaining:
  - what it does,
  - what inputs it expects,
  - what it returns or side effects it has.

  Example:
  ```csharp
  // Moves the ship one sector using impulse speed.
  // distanceSectors: how many sectors to move.
  // Returns true if the move was successful.
  bool MoveByImpulse(int distanceSectors)
  {
      // ...
  }
  ```

- **Every variable and field** gets a brief comment on its purpose, especially if its name is not self-explanatory or if it is part of core game logic.

  Example:
  ```csharp
  // Current amount of Helion fuel in this ship.
  private int currentHelionFuel;
  ```

- **Every important function call** should have a comment stating *why* it is being called and what the high-level effect is, especially in core systems (galaxy generation, fuel, travel, networking).

  Example:
  ```csharp
  // Consume Helion fuel based on distance, mass, and speed.
  ConsumeHelionFuel(distanceUnits, shipMassUnits, impulseSpeed);
  ```

- Complex or multi-step logic should be broken into clearly named helper methods and **step-by-step comments** rather than clever one-liners.

## 6. Simplicity Rules

When Copilot faces a design choice, use these priorities, in this order:

1. **Matches the Docs** – follow `Docs/GDD.md` and any other docs.
2. **Readable for Beginners** – a new C# / Unity programmer should be able to follow it.
3. **Simple First** – fewer concepts, fewer moving parts, smaller functions.
4. **Explicit Over Implicit** – be clear about types, units, and meanings.

Examples of what to prefer:

- Prefer: `for` loops over complex LINQ.
- Prefer: simple DTO-like data classes over heavy inheritance hierarchies.
- Prefer: direct, step-by-step calculations over compressed math expressions.
- Prefer: one clear responsibility per class or script.

## 7. Refactoring and Existing Code

When modifying existing code, Copilot should:

- Preserve the **overall structure and intent** of the code.
- Make changes **as small and local as possible**.
- When simplifying or refactoring, add comments that explain:
  - what changed,
  - why it is simpler or clearer now.

If existing code is complex, prefer **wrapping it with a simpler interface** and documenting that interface rather than rewriting everything at once.

## 8. Testing and Safety

- Where reasonable, add small, targeted tests or sample scenes that demonstrate how code is meant to be used.
- When in doubt about a behavior, add a `TODO` comment that clearly describes the question and references the relevant doc in `Docs`.

## 9. Summary for Copilot

- Read and follow all documents in `Docs`, especially `GDD.md`.
- Write **simple, explicit, commented** C# / Unity code.
- Comment **every function, variable, and important call** with its purpose.
- Prefer clarity and learning value over cleverness or micro-optimizations.
