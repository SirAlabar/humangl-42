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
            HandleLimbResize(state, keyboard, dt);
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
            if (keyboard.IsKeyPressed(Keys.D5)) { SwitchAnimation(state, AnimationState.Karate); }
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

        private const float ResizeSpeed = 0.5f;
        private const float SizeMin    = 0.20f;

        private static void HandleLimbResize(AppState state, KeyboardState keyboard, float dt)
        {
            if (state.Model == null)
            {
                return;
            }

            System.Collections.Generic.List<Scene.BodyNode> limbs =
                new System.Collections.Generic.List<Scene.BodyNode>();

            foreach (Scene.BodyNode n in state.Model.AllNodes)
            {
                if (n.Parent == null || n.Parent.Parent == null)
                {
                    limbs.Add(n);
                }
            }

            if (keyboard.IsKeyPressed(Keys.Tab))
            {
                state.SelectedNodeIndex = (state.SelectedNodeIndex + 1) % limbs.Count;
            }

            Scene.BodyNode node = limbs[state.SelectedNodeIndex];

            if (keyboard.IsKeyDown(Keys.Equal) || keyboard.IsKeyDown(Keys.KeyPadAdd))
            {
                ResizeChain(node, +ResizeSpeed * dt);
            }

            if (keyboard.IsKeyDown(Keys.Minus) || keyboard.IsKeyDown(Keys.KeyPadSubtract))
            {
                ResizeChain(node, -ResizeSpeed * dt);
            }
        }

        private static void ResizeChain(Scene.BodyNode node, float delta)
        {
            ApplyResize(node, delta);

            foreach (Scene.BodyNode child in node.Children)
            {
                ResizeAndReattach(node, child, delta);
            }
        }

        // Scale a single node's Size. Head scales uniformly to preserve proportions.
        private static void ApplyResize(Scene.BodyNode node, float delta)
        {
            float ny = MathF.Max(SizeMin, node.Size.Y + delta);
            if (node.Name == "Head")
            {
                float ratio = node.Size.Y > 0f ? ny / node.Size.Y : 1f;
                node.Size = new Vec3(node.Size.X * ratio, ny, node.Size.Z * ratio);
            }
            else
            {
                node.Size = new Vec3(node.Size.X, ny, node.Size.Z);
            }
        }

        private static void ResizeAndReattach(Scene.BodyNode parent, Scene.BodyNode child, float delta)
        {
            ApplyResize(child, delta);

            if (child.LocalOffset.Y < 0f)
            {
                // Chain child below parent (arms, legs, hands, feet).
                // Recompute Y, preserve X and Z offsets.
                child.LocalOffset = new Vec3(
                    child.LocalOffset.X,
                    -0.5f * (1f + child.Size.Y / parent.Size.Y),
                    child.LocalOffset.Z);
            }
            else if (child.LocalOffset.Y > 0f && child.LocalOffset.X == 0f && child.LocalOffset.Z == 0f)
            {
                // Top-attached child (Neck on Torso, Head on Neck).
                // Keep bottom flush with parent top.
                child.LocalOffset = new Vec3(
                    0f,
                    0.5f * (1f + child.Size.Y / parent.Size.Y),
                    0f);
            }

            foreach (Scene.BodyNode grandchild in child.Children)
            {
                ResizeAndReattach(child, grandchild, delta);
            }
        }
    }
}