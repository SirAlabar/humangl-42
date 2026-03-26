using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using HumanGL.Math;
using HumanGL.Animation;

namespace HumanGL
{
    // Reads keyboard and mouse state, mutates AppState.
    // No OpenGL calls. No rendering logic.

    public static class InputHandler
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float ZoomSpeed  = 0.3f;
        private const float ZoomMin    = 1.5f;
        private const float ZoomMax    = 20f;
        private const float DragSens   = 0.005f;   // rad/pixel

        /* ── Per-frame update ────────────────────────────────────────────── */

        public static void Update(AppState state, KeyboardState keyboard, float dt)
        {
            HandleAnimationKeys(state, keyboard);
            HandleTextureToggle(state, keyboard);
            HandleLimbResize(state, keyboard);
        }

        /* ── Mouse events ────────────────────────────────────────────────── */

        public static void OnMouseDown(AppState state, float mouseX, float mouseY)
        {
            state.IsDragging   = true;
            state.MouseLastPos = new Vec2(mouseX, mouseY);
        }

        public static void OnMouseMove(AppState state, float mouseX, float mouseY)
        {
            if (!state.IsDragging)
            {
                return;
            }

            Vec2  current = new Vec2(mouseX, mouseY);
            float dx      = current.X - state.MouseLastPos.X;
            float dy      = current.Y - state.MouseLastPos.Y;

            state.ManualRotY   += dx * DragSens;
            state.ManualRotX   += dy * DragSens;
            state.MouseLastPos  = current;
        }

        public static void OnMouseUp(AppState state)
        {
            state.IsDragging = false;
        }

        public static void OnMouseWheel(AppState state, float offsetY)
        {
            state.CameraZ -= offsetY * ZoomSpeed;
            state.CameraZ  = MathF.Max(ZoomMin, MathF.Min(ZoomMax, state.CameraZ));
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        private static void HandleAnimationKeys(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.D1)) { SwitchAnimation(state, AnimationState.Idle);   }
            if (keyboard.IsKeyPressed(Keys.D2)) { SwitchAnimation(state, AnimationState.Walk);   }
            if (keyboard.IsKeyPressed(Keys.D3)) { SwitchAnimation(state, AnimationState.Jump);   }
            if (keyboard.IsKeyPressed(Keys.D4)) { SwitchAnimation(state, AnimationState.Disco);  }
            if (keyboard.IsKeyPressed(Keys.D5)) { SwitchAnimation(state, AnimationState.KungFu); }
            if (keyboard.IsKeyPressed(Keys.D6)) { SwitchAnimation(state, AnimationState.TPose);  }
        }

        private static void SwitchAnimation(AppState state, AnimationState next)
        {
            if (state.AnimationMode == next)
            {
                return;
            }

            state.PreviousAnim     = state.AnimationMode;
            state.AnimationMode    = next;
            state.TransitionTimer  = 0f;
        }

        private static void HandleTextureToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.T))
            {
                state.TexturesEnabled = !state.TexturesEnabled;
            }
        }

        private static void HandleLimbResize(AppState state, KeyboardState keyboard)
        {
            // Tab cycles selected node; +/- adjusts its Y scale
            // Implemented once HumanModel and BodyNode exist in Phase 3

            if (keyboard.IsKeyPressed(Keys.Tab))
            {
                state.SelectedNodeIndex++;
            }
        }
    }
}