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
            _cubeMesh   = new CubeMesh();
            _model      = new HumanModel();
            _stack      = new MatrixStack();
            state.Model = _model;
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw(AppState state, int viewportWidth, int viewportHeight)
        {
            if (state.Shader == null || viewportWidth <= 0 || viewportHeight <= 0)
            {
                return;
            }

            int mid = viewportHeight / 2;
            GL.Enable(EnableCap.ScissorTest);

            GL.Scissor(0, mid, viewportWidth, viewportHeight - mid);
            GL.ClearColor(0.25f, 0.55f, 0.90f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Scissor(0, 0, viewportWidth, mid);
            GL.ClearColor(0.22f, 0.55f, 0.20f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Disable(EnableCap.ScissorTest);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            state.Shader.Use();

            Mat4 view = BuildView(state);
            Mat4 proj = BuildProjection(viewportWidth, viewportHeight);

            state.Shader.SetMat4("u_view",       view);
            state.Shader.SetMat4("u_projection", proj);
            state.Shader.SetInt ("u_useTexture",  state.TexturesEnabled ? 1 : 0);

            _stack.Reset();
            _stack.Multiply(Mat4.RotateX(state.ManualRotX));
            _stack.Multiply(Mat4.RotateY(state.ManualRotY));
            _stack.Multiply(Mat4.Translate(new Vec3(0f, state.TorsoOffsetY, 0f)));

            DrawNode(_model.Root, Vec3.One, state);
        }

        /* ── DrawNode ────────────────────────────────────────────────────── */

        private void DrawNode(BodyNode node, Vec3 parentSize, AppState state)
        {
            _stack.Push();

            Vec3 worldOffset = new Vec3(
                node.LocalOffset.X * parentSize.X,
                node.LocalOffset.Y * parentSize.Y,
                node.LocalOffset.Z * parentSize.Z
            );
            _stack.Multiply(Mat4.Translate(worldOffset));

            _stack.Multiply(Mat4.RotateX(node.RotationX));
            _stack.Multiply(Mat4.RotateY(node.RotationY));
            _stack.Multiply(Mat4.RotateZ(node.RotationZ));

            Mat4 drawMatrix = _stack.Top * Mat4.Scale(node.Size);
            state.Shader.SetMat4("u_model",  drawMatrix);
            state.Shader.SetVec3("u_colour", node.Colour);
            node.Texture.Bind(0);
            _cubeMesh.Draw();
            node.Texture.Unbind();

            foreach (BodyNode child in node.Children)
            {
                DrawNode(child, node.Size, state);
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