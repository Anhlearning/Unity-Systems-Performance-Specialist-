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
- **No invented values.** Only the center and Vòng 1 are required (A14). Any
  other empty slot stays empty (A09): no form means one manifestation on the
  circle itself, no Vòng 2 means no solid core, no motion means it stands still,
  no lifetime means it burns until stopped. A rune that needs a partner (a speed
  with no path, a count with no form) is reported, not paired up.
- **Deterministic casts.** The only randomness is `Mulberry32`, seeded from the
  circle. Only `CastStage` reads a clock. `WebParityTests` pins seeds, layouts and
  instance motion to the web build's own output. Particles diverge on purpose:
  they follow per-element physics the web build does not have.
- **Looks are data.** One row per material in `Core/Cast/Looks.cs`, paint and
  motion alike (buoyancy, pinch, turbulence, swirl, tear height), after the
  per-element physics of the wha-spell-simulator reference.

## Extending

- **New rune**: add a row to `Runes.All` and its art to `Glyphs.RuneArt`.
  `EveryRuneInTheCatalogHasGlyphArt` fails until you do.
- **New material look**: edit its row in `Looks.cs`.
- **Bodies**: a material whose identity is a form rather than a spray (Lốc's
  funnel, Gió's streaming gusts, Sóng âm's wavefronts) carries a `BodySpec` in
  its look row. `Core/Cast/Bodies.cs` builds the form as 3D strokes, a pure
  function of the cast clock; `View/StrokeRenderer.cs` draws them as
  camera-facing brush ribbons. One body stands on every instance, a filled
  area gets one body sized to the area, and Vòng 4 moves bodies like
  instances. A new kind is a case in `BodyStrokes.Build` plus its spec factory.
- **Particle art**: every material draws its particles from a sprite atlas in
  `View/Resources/MagicCircleParticles/<material-key>.png` (flame flipbook,
  whirlwind spirals, stone facets, lightning forks...). The atlases are drawn
  by code: edit a specimen in `Art~/generate_particles.py` and run
  `python Art~/generate_particles.py` (numpy + Pillow). It also rewrites
  `Art~/contact-sheet.png`; `Art~/PHILOSOPHY.md` is the brief they follow. The
  `~` keeps Unity from importing that folder. `ParticleSprite` in the look row
  says how the atlas is read: its grid, animated or one frame per particle,
  roll and spin, additive light or alpha-blended matter.
