using System;
using OpenTK.Graphics.OpenGL4;
using HumanGL.Math;
using HumanGL.Scene;

namespace HumanGL.Rendering
{
    // Owns the shared CubeMesh and the HumanModel.
    // Each frame: clear → scene pass (DrawNode tree) → [bonus: UI pass].

    public class Renderer : IDisposable
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float FovY      = MathF.PI / 4f;
        private const float NearPlane = 0.1f;
        private const float FarPlane  = 100f;

        /* ── Fields ──────────────────────────────────────────────────────── */

        private CubeMesh    _cubeMesh = null!;
        private HumanModel  _model    = null!;
        private MatrixStack _stack    = null!;
        private bool        _disposed;

        /* ── Init ────────────────────────────────────────────────────────── */

        public void Init(AppState state)
        {
            _cubeMesh = new CubeMesh();
            _model    = new HumanModel();
            _stack    = new MatrixStack();
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw(AppState state, int viewportWidth, int viewportHeight)
        {
            if (state.Shader == null)
            {
                return;
            }

            // Sky / grass background via scissor — no geometry needed.
            int mid = viewportHeight / 2;
            GL.Enable(EnableCap.ScissorTest);

            GL.Scissor(0, mid, viewportWidth, viewportHeight - mid); // top half
            GL.ClearColor(0.25f, 0.55f, 0.90f, 1f);                 // sky blue
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Scissor(0, 0, viewportWidth, mid);                    // bottom half
            GL.ClearColor(0.22f, 0.55f, 0.20f, 1f);                 // grass green
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Disable(EnableCap.ScissorTest);
            GL.Clear(ClearBufferMask.DepthBufferBit);                // full-screen depth reset

            state.Shader.Use();

            Mat4 view = BuildView(state);
            Mat4 proj = BuildProjection(viewportWidth, viewportHeight);

            state.Shader.SetMat4("u_view",       view);
            state.Shader.SetMat4("u_projection", proj);
            state.Shader.SetInt ("u_useTexture",  state.TexturesEnabled ? 1 : 0);

            // Reset the stack and apply the camera orbit before any node.
            _stack.Reset();
            _stack.Multiply(Mat4.RotateX(state.ManualRotX));
            _stack.Multiply(Mat4.RotateY(state.ManualRotY));

            // Vertical bob offset set by Animator (0 until Phase 4).
            _stack.Multiply(Mat4.Translate(new Vec3(0f, state.TorsoOffsetY, 0f)));

            DrawNode(_model.Root, state);
        }

        /* ── DrawNode ────────────────────────────────────────────────────── */

        // Recursive tree traversal.
        // One CubeMesh.Draw() call per node — the constraint the subject enforces.
        private void DrawNode(BodyNode node, AppState state)
        {
            _stack.Push();

            // Order: Translate → RotateX → RotateY → RotateZ → Scale
            // Scale is last so rotation pivots are not distorted and children's
            // LocalOffset values remain in pre-scale parent space.
            _stack.Multiply(Mat4.Translate(node.LocalOffset));
            _stack.Multiply(Mat4.RotateX(node.RotationX));
            _stack.Multiply(Mat4.RotateY(node.RotationY));
            _stack.Multiply(Mat4.RotateZ(node.RotationZ));
            _stack.Multiply(Mat4.Scale(node.Size));

            state.Shader.SetMat4("u_model",  _stack.Top);
            state.Shader.SetVec3("u_colour", node.Colour);

            node.Texture.Bind(0);
            _cubeMesh.Draw();
            node.Texture.Unbind();

            foreach (BodyNode child in node.Children)
            {
                DrawNode(child, state);
            }

            _stack.Pop();
        }

        /* ── Matrix builders ─────────────────────────────────────────────── */

        private static Mat4 BuildView(AppState state)
        {
            return Mat4.LookAt(
                new Vec3(0f, 0f, state.CameraZ),
                Vec3.Zero,
                Vec3.UnitY);
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
                _cubeMesh?.Dispose();
                _disposed = true;
            }
        }
    }
}
