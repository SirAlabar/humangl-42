using System;
using OpenTK.Graphics.OpenGL4;
using HumanGL.Math;

namespace HumanGL.Rendering
{
    // Owns the CubeMesh and orchestrates all draw passes.
    // Phase 1: clears the screen and builds MVP matrices.
    // Phase 2+: DrawNode tree traversal replaces the placeholder below.

    public class Renderer : IDisposable
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float FovY      = MathF.PI / 4f;
        private const float NearPlane = 0.1f;
        private const float FarPlane  = 100f;

        /* ── Fields ──────────────────────────────────────────────────────── */

        private bool _disposed;

        /* ── Init ────────────────────────────────────────────────────────── */

        public void Init(AppState state)
        {
            // Phase 2: create CubeMesh here
            // Phase 3: create HumanModel here
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw(AppState state, int viewportWidth, int viewportHeight)
        {
            if (state.Shader == null)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            state.Shader.Use();

            Mat4 view = BuildView(state);
            Mat4 proj = BuildProjection(viewportWidth, viewportHeight);

            state.Shader.SetMat4("u_view",       view);
            state.Shader.SetMat4("u_projection", proj);

            // Phase 2: DrawNode(model.Root, matrixStack) goes here
        }

        /* ── Matrix builders ─────────────────────────────────────────────── */

        private static Mat4 BuildView(AppState state)
        {
            return Mat4.LookAt(
                new Vec3(0f, 0f, state.CameraZ),
                Vec3.Zero,
                Vec3.UnitY
            );
        }

        private static Mat4 BuildProjection(int width, int height)
        {
            float aspect = (float)width / (float)height;
            return Mat4.Perspective(FovY, aspect, NearPlane, FarPlane);
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                // Phase 2: cubeMesh.Dispose() here
                _disposed = true;
            }
        }
    }
}