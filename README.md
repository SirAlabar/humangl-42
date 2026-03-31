# HumanGL — 42

A 3D articulated human figure rendered with OpenGL 4.1 (Core Profile) using a custom matrix stack. No GLM, no external math library.

## Build & Run

```bash
dotnet build
dotnet run --project HumanGL.csproj
```

Requires .NET 8+ and a GPU that supports OpenGL 4.1 Core Profile.

## Controls

| Key | Action |
|-----|--------|
| `1` | Idle animation |
| `2` | Walk animation |
| `3` | Jump animation (one-shot) |
| `4` | Disco animation |
| `5` | Kung-Fu animation |
| `6` | T-Pose |
| `Tab` | Cycle selected limb (for resize) |
| `+` / `=` | Grow selected limb |
| `-` | Shrink selected limb |
| `T` | Toggle textures |
| Left drag | Orbit camera |
| Scroll | Zoom in/out |
| `Esc` | Quit |

## Architecture

### Matrix Stack

All transforms are computed by hand in `src/Scene/MatrixStack.cs`. The stack mirrors OpenGL's old `glPushMatrix` / `glPopMatrix` API:

```
Push → apply parent transform → draw children → Pop
```

Each `BodyNode` is drawn relative to its parent's coordinate frame. A node's world position is:

```
World = Parent.World × LocalTranslation × LocalRotation × LocalScale
```

### Body Hierarchy

```
Torso
├── Head
├── LeftUpperArm
│   └── LeftForeArm
├── RightUpperArm
│   └── RightForeArm
├── LeftThigh
│   └── LeftShin
└── RightThigh
    └── RightShin
```

Child attachment points are expressed as `LocalOffset` in the parent's local space (e.g. `(0, -0.5, 0)` for a limb hanging below its parent).

### Animation System

Animations use a Unity-style state machine defined in `src/Animation/`:

- `AnimationStateBase` — abstract base with `Enter`, `Apply`, `Exit` hooks
- `Animator` — static state machine; detects `AppState.AnimationMode` changes, calls `Enter`/`Exit`, then `Apply` every frame
- Transitions blend smoothly over **0.15 s** by snapshotting all joint angles at the moment of switch and lerping to the new state's output

Current states:

| State | Description |
|-------|-------------|
| `IdleState` | Gentle torso bob |
| `WalkState` | Sinusoidal leg/arm swing with knee and elbow bend |
| `JumpState` | One-shot 4-phase jump (crouch → ascent → apex → land), auto-returns to Idle |

### Limb Resize

`Tab` cycles through limb roots (Torso, Head, UpperArms, Thighs). `+`/`-` held continuously scales the selected limb and cascades the resize down its chain (UpperArm→ForeArm, Thigh→Shin), recomputing `LocalOffset` to keep joints flush.

## Project Structure

```
src/
  Core/         App.cs, AppState.cs, InputHandler.cs, Program.cs
  Scene/        HumanModel.cs, BodyNode.cs, MatrixStack.cs
  Animation/    Animator.cs, AnimationState.cs
    States/     AnimationStateBase.cs, IdleState.cs, WalkState.cs, JumpState.cs
  Rendering/    Renderer.cs, Shader.cs, CubeMesh.cs, Texture.cs, ...
  Math/         Mat4.cs, Vec2.cs, Vec3.cs, Vec4.cs
  UI/           BitmapFont.cs, Slider.cs, UiPanel.cs
shaders/
  vertex.glsl
  fragment.glsl
```
