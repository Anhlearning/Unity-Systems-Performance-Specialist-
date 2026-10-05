# Magic Circle (Unity port)

Port of `magic-circle-simulator` (SvelteKit + three.js): build a layered magic
circle, compile it, and cast it as a deterministic 3D effect. Open
`Assets/Scene/MagicCirclePreview.unity` (or rebuild it with
**Tools > Magic Circle > Create Preview Scene**) and press Play.

```
MagicCircle ──SpellCompiler──► SpellIR ──SpellPerformance──► instances + particles ──CastStage──► Unity
 (what is on the circle)   (rules + values with units)   (fixed 60 Hz steps, seeded)          (draws)
```

| Folder              | Assembly                 | Web build equivalent | Role                                                    |
| ------------------- | ------------------------ | -------------------- | ------------------------------------------------------- |
| `Core/Design`       | `MagicCircleSim.Core`    | `lib/design`         | Workbook data. `Assumptions.cs` holds every A-xx.       |
| `Core/Circle`       | `MagicCircleSim.Core`    | `lib/circle`         | The circle model, glyph/ring art, vector rasterizer.    |
| `Core/Compiler`     | `MagicCircleSim.Core`    | `lib/compiler`       | `SpellCompiler`, `SpawnLayout`, `Mulberry32`, `Fnv1a`.  |
| `Core/Cast`         | `MagicCircleSim.Core`    | `lib/cast`           | Timeline, trajectory, particles, look table.            |
| `View`              | `MagicCircleSim.View`    | `lib/cast/three`     | `CastStage`, ground circle, visuals, URP shaders.       |
| `Preview`           | `MagicCircleSim.Preview` | `lib/editor`         | IMGUI palette, presets, summary, session state.         |
| `Editor`            | `MagicCircleSim.Editor`  | –                    | Scene builder menu item.                                |
| `Tests`             | `MagicCircleSim.Tests`   | `tests/`             | EditMode tests, including parity with the web build.    |

## Laws (same as the web build)

- **The workbook is the source of truth.** Open questions live only in
  `Core/Design/Assumptions.cs`.
- **Layers stay apart.** `Core` has `noEngineReferences`, so it cannot touch
  UnityEngine. Circle code says nothing about meaning, and cast code never reads a rune.
- **Deterministic casts.** The only randomness is `Mulberry32`, seeded from the
  circle. Only `CastStage` reads a clock. `WebParityTests` pins seeds, layouts and
  particle state to the web build's own output.
- **Looks are data.** One row per material in `Core/Cast/Looks.cs`.

## Extending

- **New rune**: add a row to `Runes.All` and its art to `Glyphs.RuneArt`.
  `EveryRuneInTheCatalogHasGlyphArt` fails until you do.
- **New material look**: edit its row in `Looks.cs`.
