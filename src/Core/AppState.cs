using HumanGL.Math;
using HumanGL.Rendering;
using HumanGL.Animation;
using HumanGL.Scene;

namespace HumanGL
{
    // All runtime state in one place.
    // InputHandler and Renderer read/write this — no globals, no circular deps.

    public class AppState
    {
        /* ── Shader ──────────────────────────────────────────────────────── */

        public Shader   Shader  = null!;

        /* ── Animation ───────────────────────────────────────────────────── */

        public AnimationState   AnimationMode    = AnimationState.Idle;
        public AnimationState   PreviousAnim     = AnimationState.Idle;
        public float            Time             = 0f;
        public float            TransitionTimer  = 0f;
        public float            TorsoOffsetY     = 0f;

        /* ── Camera ──────────────────────────────────────────────────────── */

        public float    CameraZ     = 4.0f;
        public float    ManualRotX  = 0f;
        public float    ManualRotY  = 0f;

        /* ── Mouse drag ──────────────────────────────────────────────────── */

        public bool     IsDragging   = false;
        public Vec2     MouseLastPos = new Vec2(0f, 0f);

        /* ── Textures (bonus) ────────────────────────────────────────────── */

        public bool     TexturesEnabled  = false;

        /* ── Scene model (set by Renderer.Init) ─────────────────────────── */

        public HumanModel? Model = null;

        /* ── UI panel (bonus) ────────────────────────────────────────────── */

        public int      SelectedNodeIndex = 0;
    }
}