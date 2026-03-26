# HumanGL — Planning Document
> C# · .NET 8 · OpenGL 4.1 Core · OpenTK 4 · Allman brace style

---

## Table of Contents

1. [Project Summary](#1-project-summary)
2. [Reuse Analysis — SCOP vs HumanGL](#2-reuse-analysis)
3. [Directory Structure](#3-directory-structure)
4. [Architecture Overview](#4-architecture-overview)
5. [Class Catalogue](#5-class-catalogue)
6. [Body Hierarchy & Matrix Stack](#6-body-hierarchy--matrix-stack)
7. [Animation System](#7-animation-system)
8. [Texture System (Bonus)](#8-texture-system-bonus)
9. [Side Panel UI (Bonus)](#9-side-panel-ui-bonus)
10. [Development Phases](#10-development-phases)
11. [Shader Plan](#11-shader-plan)
12. [Controls Specification](#12-controls-specification)
13. [Bonus Summary](#13-bonus-summary)
14. [Acceptance Checklist](#14-acceptance-checklist)

---

## 1. Project Summary

HumanGL is a skeletal animation viewer built on top of a hand-rolled matrix stack.
A humanoid figure made of **10 rectangular boxes** must walk, jump, and idle.
Every body part is drawn by **one and only one** function call that renders a unit cube
(1 × 1 × 1) at the origin of the current matrix.
All positioning, rotation, and scaling is achieved purely through matrix transforms
pushed onto and popped from a custom `MatrixStack`.

No `glRotate`, `glPushMatrix`, or any deprecated fixed-function API may be used.

**This is NOT a physics simulation.** Joint angles are driven entirely by hand-written
`sin()` curves inside `Animator`. There is no collision, no rigid-body dynamics, no
ragdoll. You control every angle every frame.

---

## 2. Reuse Analysis

Real SCOP directory structure (from `SCOP-42` workspace):

```
src/
  Math/           Vec2 Vec3 Vec4 Mat4
  Parsing/
    Interfaces/   ITriangulator IUvMapper
    Triangulation/ EarClipper FanTriangulator
    UvMapping/    BoxUvMapper FaceNormalUvMapper
    ObjParser Vertex
  Rendering/
    Interfaces/   ITexture
    Lightsphere NullTexture Mesh Renderer Shader Texture
  utils/          FileParser FileValidator
  App AppState InputHandler ModelState Program Screenshot
shaders/          vertex.glsl fragment.glsl
```

### ✅ Copy as-is (rename namespace to `HumanGL`)

| File | Why |
|---|---|
| `Mat4.cs` | Column-major mat4 — Translate / Scale / RotateX/Y/Z / Perspective / LookAt |
| `Vec2.cs` | Unchanged |
| `Vec3.cs` | Unchanged |
| `Vec4.cs` | Unchanged |
| `Shader.cs` | `Load / Use / SetMat4 / SetFloat / SetInt / SetVec3` — identical contract |
| `ITexture.cs` | Interface used by `BodyNode` |
| `NullTexture.cs` | Default for untextured parts |
| `Texture.cs` | Full texture loader — reused for per-part textures |
| `Program.cs` | Entry point pattern; swap arg validation |

### ⚠️ Adapt (significant changes)

| File | What changes |
|---|---|
| `App.cs` | Remove OBJ/texture loading; add `HumanModel`, `Animator`, `UiPanel` init; rename title |
| `AppState.cs` | Replace mesh/obj fields with animation state, limb descriptors, camera, UI state |
| `InputHandler.cs` | Keep mouse-drag + scroll zoom; add keyboard animation switching + UI interaction |
| `Renderer.cs` | Replace single mesh draw with `DrawNode` tree traversal + side panel pass |
| `ModelState.cs` | Rename/repurpose for `AnimationState` enum + limb runtime data |

### ❌ Not needed

`ObjParser`, `FileParser`, `FileValidator`, `BoxUvMapper`, `FaceNormalUvMapper`,
`EarClipper`, `FanTriangulator`, `ITriangulator`, `IUvMapper`, `Lightsphere`, `Screenshot`

---

## 3. Directory Structure

```
humangl/
├── src/
│   ├── Core/
│   │   ├── App.cs                  # GameWindow subclass, orchestrator
│   │   ├── AppState.cs             # Anim state, limb descriptors, camera, UI state
│   │   ├── Program.cs              # Entry point
│   │   └── InputHandler.cs        # Keyboard + mouse
│   │
│   ├── Math/
│   │   ├── Mat4.cs                 # ← copied from SCOP
│   │   ├── Vec2.cs                 # ← copied from SCOP
│   │   ├── Vec3.cs                 # ← copied from SCOP
│   │   └── Vec4.cs                 # ← copied from SCOP
│   │
│   ├── Rendering/
│   │   ├── Interfaces/
│   │   │   └── ITexture.cs         # ← copied from SCOP
│   │   ├── Shader.cs               # ← copied from SCOP
│   │   ├── Texture.cs              # ← copied from SCOP
│   │   ├── NullTexture.cs          # ← copied from SCOP
│   │   ├── CubeMesh.cs             # Unit cube VAO/VBO/EBO with UVs
│   │   └── Renderer.cs             # Scene pass + UI pass
│   │
│   ├── Scene/
│   │   ├── MatrixStack.cs          # Stack<Mat4>: Push / Pop / Top / Multiply
│   │   ├── BodyNode.cs             # Tree node: colour, size, angles, texture
│   │   └── HumanModel.cs          # Wires all nodes; exposes GetNode(name)
│   │
│   ├── Animation/
│   │   ├── AnimationState.cs       # Enum: Idle Walk Jump Disco KungFu TPose
│   │   └── Animator.cs             # Per-frame angle computation
│   │
│   └── UI/
│       ├── BitmapFont.cs           # Loads font atlas PNG, renders quads per glyph
│       ├── Slider.cs               # A single labelled slider widget
│       └── UiPanel.cs             # Side panel layout + input hit-testing
│
├── assets/
│   ├── textures/
│   │   ├── head.png
│   │   ├── neck.png
│   │   ├── torso.png
│   │   ├── upper_arm.png
│   │   ├── forearm.png
│   │   ├── hand.png
│   │   ├── thigh.png
│   │   ├── shin.png
│   │   └── foot.png
│   └── fonts/
│       └── font_atlas.png          # 16×8 ASCII bitmap font (128×128 px)
│
├── shaders/
│   ├── vertex.glsl
│   ├── fragment.glsl
│   ├── ui_vertex.glsl              # Orthographic 2D pass
│   └── ui_fragment.glsl
│
├── humangl.csproj
└── README.md
```

---

## 4. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│  App  (GameWindow)                                                    │
│                                                                       │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────────────────┐ │
│  │  AppState   │  │ InputHandler │  │  Renderer                    │ │
│  │  anim       │◄─┤  keyboard    │  │                              │ │
│  │  limbData[] │  │  mouse drag  │  │  ┌──────────┐  ┌──────────┐ │ │
│  │  camera     │  │  scroll zoom │  │  │ Scene    │  │ UI Pass  │ │ │
│  │  uiState    │  │  panel click │  │  │ Pass     │  │ (ortho)  │ │ │
│  └─────────────┘  └──────────────┘  │  │ DrawNode │  │ UiPanel  │ │ │
│                                      │  └──────────┘  └──────────┘ │ │
│                                      └──────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
         │
         ▼
  ┌─────────────────────┐        ┌──────────────────────┐
  │  HumanModel         │        │  Animator             │
  │  root = Torso       │◄───────┤  Update(model,state,  │
  │  15 nodes, tree     │        │         deltaTime)    │
  └─────────────────────┘        └──────────────────────┘
```

### Viewport layout (split-screen)

```
┌────────────────────────────┬───────────┐
│                            │  PANEL    │
│   3D scene (perspective)   │  sliders  │
│                            │  labels   │
│   ~75% of window width     │  ~25%     │
└────────────────────────────┴───────────┘
```

The split is implemented by rendering the 3D scene into a `GL.Viewport` covering
the left 75%, then switching to a full-window orthographic viewport for the UI pass.
No framebuffer objects required.

---

## 5. Class Catalogue

### 5.1 `MatrixStack`

```
+ Push()            — duplicates Top onto the stack
+ Pop()             — removes the top entry
+ Top : Mat4        — current accumulated world transform (read-only)
+ Multiply(Mat4 m)  — Top = Top * m
+ Reset()           — clears stack, pushes Identity
```

### 5.2 `BodyNode`

```
+ Name          : string
+ Parent        : BodyNode?
+ Children      : List<BodyNode>
+ LocalOffset   : Vec3          — joint position relative to parent (pre-scale space)
+ Size          : Vec3          — current scale (modified by UI panel sliders)
+ RotationX/Y/Z : float         — current joint angles in radians (set by Animator)
+ Colour        : Vec3          — fallback flat RGB when no texture is active
+ Texture       : ITexture      — NullTexture by default; replaced for bonus
```

The node holds **parameters only**. Matrices are computed at draw time.

### 5.3 `HumanModel`

Assembles all 15 nodes and wires parent-child relationships.

```
+ Root                      : BodyNode
+ GetNode(string name)      : BodyNode
+ AllNodes                  : IReadOnlyList<BodyNode>
```

### 5.4 `Animator`

```
+ Update(HumanModel model, AppState state, float deltaTime)
```

Drives joint angles per `AnimationState`. Uses linear interpolation between states
over a 0.15 s blend window. Writes directly to `BodyNode.RotationX/Y/Z` and to
`AppState.TorsoOffsetY`.

### 5.5 `CubeMesh`

Hardcoded 1 × 1 × 1 cube centred at the origin.

- 24 vertices (4 per face — needed for distinct per-face UV coordinates)
- 36 indices (6 faces × 2 triangles × 3 indices)
- Vertex layout: `Position (vec3)` + `UV (vec2)`
- Single VAO / VBO / EBO
- `Draw()` calls `GL.DrawElements(Triangles, 36, UnsignedInt, 0)`

All 15 body parts share **one** `CubeMesh` instance.

### 5.6 `Renderer`

```
Draw(AppState state, int windowWidth, int windowHeight)
  ├── ScenePass(state, sceneWidth, windowHeight)
  │     ├── GL.Viewport(0, 0, sceneWidth, windowHeight)
  │     ├── GL.Clear(Color | Depth)
  │     ├── sceneShader.Use()
  │     ├── upload u_view, u_projection
  │     └── DrawNode(model.Root, matrixStack)
  │
  └── UiPass(state, sceneWidth, windowWidth, windowHeight)
        ├── GL.Viewport(0, 0, windowWidth, windowHeight)
        ├── GL.Disable(DepthTest)
        ├── uiShader.Use()
        ├── upload u_ortho
        ├── uiPanel.Draw(state)
        └── GL.Enable(DepthTest)

DrawNode(BodyNode node, MatrixStack stack)
  ├── stack.Push()
  ├── stack.Multiply( Translate(node.LocalOffset) )
  ├── stack.Multiply( RotateX(node.RotationX) )
  ├── stack.Multiply( RotateY(node.RotationY) )
  ├── stack.Multiply( RotateZ(node.RotationZ) )
  ├── stack.Multiply( Scale(node.Size) )           ← scale LAST
  ├── upload u_model = stack.Top
  ├── upload u_colour = node.Colour
  ├── node.Texture.Bind(0)
  ├── cubeMesh.Draw()                              ← ONE call per body part
  ├── node.Texture.Unbind()
  ├── foreach child → DrawNode(child, stack)
  └── stack.Pop()
```

### 5.7 `AppState`

```
+ AnimationMode      : AnimationState
+ PreviousAnim       : AnimationState
+ Time               : float
+ TransitionTimer    : float
+ TorsoOffsetY       : float
+ CameraZ            : float
+ ManualRotY         : float
+ ManualRotX         : float
+ TexturesEnabled    : bool
+ SelectedNodeIndex  : int
```

### 5.8 `BitmapFont`

Loads a 16×8-glyph ASCII atlas (128×128 px). Each glyph = 8×16 px.
Uses `Texture.FromFile` — zero new loader code.

```
+ DrawString(string text, float x, float y, float scale, Vec3 colour)
```

UV per glyph: `col = asciiCode % 16`, `row = asciiCode / 16`.

### 5.9 `Slider`

```
+ Label     : string
+ Value     : float
+ Min / Max : float
+ Draw(float x, float y, float width)
+ HitTest(float mx, float my) : bool
+ Drag(float deltaX)
```

Rendered as two coloured quads (track + thumb) + a `BitmapFont.DrawString` label.

### 5.10 `UiPanel`

Owns one `Slider` group per `BodyNode` (Length, Width, Depth).

```
+ Draw(AppState state)
+ OnMouseDown(float mx, float my)
+ OnMouseMove(float dx, float dy)
+ OnMouseUp()
```

`Tab` cycles `AppState.SelectedNodeIndex`; the panel renders the slider group
for the currently selected node.

---

## 6. Body Hierarchy & Matrix Stack

### 6.1 The 15 nodes

| # | Name | Parent | LocalOffset (default) | Size (default) | Mandatory |
|---|---|---|---|---|---|
| 1 | Torso | — | (0, 0, 0) | (0.60, 0.90, 0.30) | ✅ |
| 2 | Neck | Torso | (0, 0.52, 0) | (0.18, 0.20, 0.18) | bonus |
| 3 | Head | Neck | (0, 0.28, 0) | (0.40, 0.40, 0.40) | ✅ |
| 4 | LeftUpperArm | Torso | (−0.50, 0.30, 0) | (0.20, 0.50, 0.20) | ✅ |
| 5 | LeftForeArm | LeftUpperArm | (0, −0.55, 0) | (0.18, 0.45, 0.18) | ✅ |
| 6 | LeftHand | LeftForeArm | (0, −0.50, 0) | (0.16, 0.16, 0.10) | bonus |
| 7 | RightUpperArm | Torso | (0.50, 0.30, 0) | (0.20, 0.50, 0.20) | ✅ |
| 8 | RightForeArm | RightUpperArm | (0, −0.55, 0) | (0.18, 0.45, 0.18) | ✅ |
| 9 | RightHand | RightForeArm | (0, −0.50, 0) | (0.16, 0.16, 0.10) | bonus |
| 10 | LeftThigh | Torso | (−0.18, −0.55, 0) | (0.22, 0.50, 0.22) | ✅ |
| 11 | LeftShin | LeftThigh | (0, −0.55, 0) | (0.20, 0.48, 0.20) | ✅ |
| 12 | LeftFoot | LeftShin | (0, −0.52, 0.08) | (0.22, 0.12, 0.36) | bonus |
| 13 | RightThigh | Torso | (0.18, −0.55, 0) | (0.22, 0.50, 0.22) | ✅ |
| 14 | RightShin | RightThigh | (0, −0.55, 0) | (0.20, 0.48, 0.20) | ✅ |
| 15 | RightFoot | RightShin | (0, −0.52, 0.08) | (0.22, 0.12, 0.36) | bonus |

> For the mandatory part, Head connects directly to Torso (skip Neck).
> Neck is added in the bonus phase — Head's parent simply changes from Torso to Neck.

### 6.2 Tree structure

```
Torso  (root)
├── Neck  [bonus]
│   └── Head
├── LeftUpperArm
│   └── LeftForeArm
│       └── LeftHand  [bonus]
├── RightUpperArm
│   └── RightForeArm
│       └── RightHand  [bonus]
├── LeftThigh
│   └── LeftShin
│       └── LeftFoot  [bonus]
└── RightThigh
    └── RightShin
        └── RightFoot  [bonus]
```

### 6.3 Why Scale comes last

The multiply order per node is:

```
Translate → RotateX → RotateY → RotateZ → Scale
```

Scale comes last so that:
1. Rotation pivots are not distorted by non-uniform scale
2. Children's `LocalOffset` values are expressed in **pre-scale** parent space,
   meaning resizing a parent automatically repositions children in world space

---

## 7. Animation System

### 7.1 AnimationState enum

```
Idle    = 0
Walk    = 1
Jump    = 2
Disco   = 3
KungFu  = 4
TPose   = 5
```

### 7.2 Transition blending

On switch from A to B, `Animator` snapshots all current angles into `_prevAngles[]`.
For the next 0.15 s it lerps each angle from snapshot toward the new B target.

### 7.3 Idle

```
TorsoOffsetY     = sin(t × 1.5) × 0.03
All joint angles = 0
```

### 7.4 Walk

```
swing(t) = sin(t × 3.0) × 0.45
knee(v)  = max(0, v) × 0.5

LeftThigh.RotX     =  swing(t)
RightThigh.RotX    = -swing(t)
LeftShin.RotX      =  knee( swing(t))
RightShin.RotX     =  knee(-swing(t))
LeftFoot.RotX      = -LeftShin.RotX  × 0.4
RightFoot.RotX     = -RightShin.RotX × 0.4
LeftUpperArm.RotX  = -swing(t)
RightUpperArm.RotX =  swing(t)
TorsoOffsetY       =  sin(t × 6.0) × 0.02
```

### 7.5 Jump (one-shot, ~0.8 s)

| Phase | Window | What happens |
|---|---|---|
| Crouch | 0.00–0.20 s | Thighs/shins bend, arms raise |
| Ascent | 0.20–0.40 s | TorsoOffsetY → +0.6, legs straighten |
| Apex | 0.40–0.60 s | Arms spread (Z rotation), legs hang |
| Landing | 0.60–0.80 s | TorsoOffsetY → 0, brief crouch |
| Return | 0.80 s+ | Blend to Idle |

### 7.6 Disco Dance

```
hipSwing(t)       = sin(t × 4π) × 0.30
armWave(t)        = sin(t × 4π) × 0.80

Torso.RotZ        = hipSwing(t)
LeftUpperArm.RotX =  armWave(t)
LeftUpperArm.RotZ =  sin(t × 4π) × 0.40
RightUpperArm.RotX= armWave(t + π)
RightUpperArm.RotZ= -sin(t × 4π) × 0.40
Head.RotZ         = sin(t × 8π) × 0.20
LeftThigh.RotZ    =  sin(t × 4π) × 0.15
RightThigh.RotZ   = -sin(t × 4π) × 0.15
TorsoOffsetY      =  sin(t × 8π) × 0.04
```

### 7.7 Kung-Fu Kick (looping, 1.2 s per side)

```
Phase 0–0.4s : chamber — thigh raises, knee bends ~90°, arms go to guard
Phase 0.4–0.8s: extend  — shin kicks forward (~−90°), foot flexes
Phase 0.8–1.2s: return  — all back to neutral
(repeat opposite leg)

Guard:
  LeftUpperArm.RotX = -0.6,  LeftForeArm.RotX  = -1.2
  RightUpperArm.RotX= -0.3,  RightForeArm.RotX = -0.6
```

### 7.8 T-Pose

```
All angles = 0
LeftUpperArm.RotZ  = -π/2
RightUpperArm.RotZ =  π/2
```

---

## 8. Texture System (Bonus)

### 8.1 What is reused from SCOP

`Texture.cs`, `NullTexture.cs`, `ITexture.cs` — copied as-is.
`Texture.FromFile` returns a `NullTexture` on missing files; the node silently
falls back to flat colour. Same contract as SCOP.

### 8.2 CubeMesh UV layout

Each face maps `[0,1]×[0,1]` to the full texture image.

```
Top-left     → (0, 1)
Top-right    → (1, 1)
Bottom-left  → (0, 0)
Bottom-right → (1, 0)
```

### 8.3 Toggle

`AppState.TexturesEnabled` → `u_useTexture` uniform (int) in fragment shader.

```glsl
if (u_useTexture == 1)
    fragColour = texture(u_texture, v_uv);
else
    fragColour = vec4(u_colour, 1.0);
```

Key `T` flips the toggle.

### 8.4 Left arm = right arm texture

Left and right symmetric parts share the same PNG (e.g. `upper_arm.png`).
Loading the same file twice is fine — `Texture.FromFile` can be called once and
the same `Texture` instance assigned to both nodes.

---

## 9. Side Panel UI (Bonus)

### 9.1 New Mat4 method required

`Mat4.Ortho(float l, float r, float b, float t, float near, float far)`

Standard orthographic projection. Not in SCOP — add to `Mat4.cs`.

### 9.2 BitmapFont atlas format

- 128×128 PNG
- 16 columns × 8 rows of glyphs
- Glyph size: 8×16 px
- White glyphs on transparent background
- ASCII codes 32–127

A free CP437-compatible atlas can be generated with a small Python script
included in the repo, or downloaded from a public domain source.

### 9.3 Panel layout (1280×720 window)

```
Panel X  : 960–1280  (320 px)
Panel Y  : 0–720

y=700  [selected part name — BitmapFont]
y=660  Length  ──────●──────  1.00
y=620  Width   ────●────────  0.60
y=580  Depth   ─────●───────  0.30
y=530  [Tab] = cycle part
y=480  ── Animations ──
y=450  [1]Idle   [2]Walk
y=420  [3]Jump   [4]Disco
y=390  [5]KungFu [6]T-Pose
y=350  [T] Toggle textures
```

### 9.4 Render pipeline for UI pass

```
1. GL.Viewport(0, 0, windowWidth, windowHeight)
2. GL.Disable(DepthTest)
3. uiShader.Use()
4. upload u_ortho = Mat4.Ortho(0, windowWidth, 0, windowHeight, -1, 1)
5. Draw panel background (semi-transparent dark quad)
6. UiPanel.Draw(state)   → sliders + BitmapFont labels
7. GL.Enable(DepthTest)
```

---

## 10. Development Phases

### Phase 1 — Bootstrap & Math port
- Copy `Mat4`, `Vec2/3/4`, `Shader`, `Texture`, `NullTexture`, `ITexture`
- Implement `CubeMesh` (24 verts with UVs, 36 indices)
- Minimal `App` + `AppState` + `Renderer`; one cube on screen
- Mouse-drag orbit + scroll zoom

**Acceptance:** white cube visible; mouse drag + scroll work; ESC closes

---

### Phase 2 — MatrixStack + single node
- Implement `MatrixStack`
- Implement `BodyNode` (angles = 0)
- `HumanModel` with Torso only
- `Renderer.DrawNode()` (one node)
- Add `Mat4.Ortho()` to Mat4

**Acceptance:** Torso cube at correct scale; Push/Pop round-trips identity

---

### Phase 3 — Full mandatory body (10 parts, T-pose)
- Add all 10 mandatory nodes; Head connects directly to Torso
- Distinct flat colour per part
- Keyboard Y-rotation of whole figure to verify hierarchy

**Acceptance:** 10 cubes visible; torso drag moves all; limb resize repositions children

---

### Phase 4 — Animator: Idle + Walk
- Implement `AnimationState` + `Animator`
- Keys `1` / `2`; 0.15 s blend

**Acceptance:** Idle bobs; Walk swings legs + arms; smooth transitions

---

### Phase 5 — Animator: Jump
- One-shot jump, auto-return to Idle
- Key `3`

**Acceptance:** 4-phase sequence plays; returns automatically; restarts on re-press

---

### Phase 6 — Mandatory polish
- Single `DrawNode` function with one `cubeMesh.Draw()` per part
- Runtime limb resize (`Tab` + `+` / `-`)
- Zero build warnings; README

**Acceptance:** evaluator checks pass; clean build

---

### Phase 7 — Bonus: Extra parts + animations
- Add Neck, Hands, Feet (5 nodes); Head reparented to Neck
- Disco, KungFu, TPose in `Animator`; keys `4` / `5` / `6`

**Acceptance:** 15 cubes; all 6 animations work

---

### Phase 8 — Bonus: Textures
- Add UVs to `CubeMesh`; `u_useTexture` uniform in fragment shader
- Load PNGs in `HumanModel`; key `T` toggle

**Acceptance:** `T` switches modes; each part has own texture; missing = silent fallback

---

### Phase 9 — Bonus: Side panel UI
- Implement `BitmapFont`, `Slider`, `UiPanel`
- `Renderer` runs UI pass after scene pass
- Mouse click+drag on sliders; `Tab` cycles nodes

**Acceptance:** panel visible; labels readable; sliders update 3D body live

---

## 11. Shader Plan

### `vertex.glsl`

```glsl
#version 410 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_uv;

uniform mat4 u_model;
uniform mat4 u_view;
uniform mat4 u_projection;

out vec2 v_uv;

void main()
{
    v_uv        = a_uv;
    gl_Position = u_projection * u_view * u_model * vec4(a_position, 1.0);
}
```

### `fragment.glsl`

```glsl
#version 410 core

in vec2 v_uv;

uniform vec3      u_colour;
uniform sampler2D u_texture;
uniform int       u_useTexture;

out vec4 fragColour;

void main()
{
    if (u_useTexture == 1)
        fragColour = texture(u_texture, v_uv);
    else
        fragColour = vec4(u_colour, 1.0);
}
```

### `ui_vertex.glsl`

```glsl
#version 410 core

layout(location = 0) in vec2 a_position;
layout(location = 1) in vec2 a_uv;

uniform mat4 u_ortho;

out vec2 v_uv;

void main()
{
    v_uv        = a_uv;
    gl_Position = u_ortho * vec4(a_position, 0.0, 1.0);
}
```

### `ui_fragment.glsl`

```glsl
#version 410 core

in vec2 v_uv;

uniform sampler2D u_atlas;
uniform vec4      u_colour;
uniform int       u_mode;   // 0 = solid quad  1 = font glyph

out vec4 fragColour;

void main()
{
    if (u_mode == 0)
        fragColour = u_colour;
    else
        fragColour = vec4(u_colour.rgb, texture(u_atlas, v_uv).r);
}
```

---

## 12. Controls Specification

| Key / Input | Action |
|---|---|
| `1` | Idle |
| `2` | Walk |
| `3` | Jump (one-shot) |
| `4` | Disco dance |
| `5` | Kung-Fu kick |
| `6` | T-Pose (debug) |
| `T` | Toggle textures |
| `Tab` | Cycle selected body part (UI panel) |
| `+` / `=` | Increase selected limb Y scale |
| `-` | Decrease selected limb Y scale |
| Scroll wheel | Zoom |
| Left drag (3D area) | Orbit camera |
| Left drag (panel) | Move slider thumb |
| `ESC` | Quit |

---

## 13. Bonus Summary

| Bonus | Description | New files |
|---|---|---|
| B1 — Extra parts | Neck, Hands ×2, Feet ×2 (5 nodes) | `HumanModel` additions only |
| B2 — Extra animations | Disco, Kung-Fu, T-Pose | `Animator` additions only |
| B3 — Textures | One PNG per part; `T` toggle | `CubeMesh` UVs + shader uniform |
| B4 — Side panel | Split-screen; bitmap font; sliders per part | `BitmapFont`, `Slider`, `UiPanel`, ui shaders, `Mat4.Ortho` |

---

## 14. Acceptance Checklist

### Mandatory

- [ ] Body parts correctly articulated — torso drag moves all limbs
- [ ] Moving upper arm only moves forearm
- [ ] Resizing a limb repositions its children automatically
- [ ] 10 parts: head, torso, 2× upper arm, 2× forearm, 2× thigh, 2× shin
- [ ] Walk animation loops
- [ ] Jump plays (one-shot, returns to Idle)
- [ ] Idle works
- [ ] Each part = exactly one `cubeMesh.Draw()` inside `DrawNode`
- [ ] Upper arm and forearm are two separate `DrawNode` calls
- [ ] No deprecated OpenGL API; own matrix stack only
- [ ] OpenGL ≥ 4.0 (using 4.1 Core)
- [ ] `dotnet build` succeeds with zero warnings

### Bonus

- [ ] Neck, Hands, Feet present and articulating
- [ ] Disco loops with hip sway and arm waves
- [ ] Kung-Fu alternates kicks with guard arms
- [ ] T-Pose arms are perfectly horizontal
- [ ] `T` toggles textures; each part has its own PNG
- [ ] Flat colour fallback when texture missing
- [ ] Side panel visible on right 25% of screen
- [ ] Bitmap font labels readable
- [ ] Sliders update 3D body in real-time
- [ ] No crash on min/max limb size

### Evaluation interview prep

- [ ] Demonstrate Walk, Jump, Idle live
- [ ] Resize a limb live; show children follow
- [ ] Point to `DrawNode`; explain one-call constraint
- [ ] Explain matrix stack: Push before child, Pop after, Top = world transform
- [ ] Explain why Scale is the last multiply in the chain

