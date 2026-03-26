using System;
using OpenTK.Graphics.OpenGL4;

namespace HumanGL.Rendering
{
    // A unit cube (1×1×1) centred at the origin.
    // 24 vertices — 4 per face — so each face gets its own UV island.
    // All 15 body parts share ONE instance of this mesh.
    //
    // Vertex layout (matches vertex.glsl):
    //   location 0 — position  vec3   (12 bytes)
    //   location 1 — uv        vec2   ( 8 bytes)
    //   stride = 20 bytes

    public class CubeMesh : IDisposable
    {
        /* ── GPU handles ─────────────────────────────────────────────────── */

        private int  _vao;
        private int  _vbo;
        private int  _ebo;
        private bool _disposed;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public CubeMesh()
        {
            Build();
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteVertexArray(_vao);
                GL.DeleteBuffer(_vbo);
                GL.DeleteBuffer(_ebo);
                _disposed = true;
            }
        }

        /* ── Build ───────────────────────────────────────────────────────── */

        private void Build()
        {
            // 24 vertices: px py pz  ux uy
            // Each face has 4 unique vertices so UVs can tile [0,1]×[0,1] per face.
            float[] vertices =
            {
                // +Z (front)
                -0.5f, -0.5f,  0.5f,   0f, 0f,
                 0.5f, -0.5f,  0.5f,   1f, 0f,
                 0.5f,  0.5f,  0.5f,   1f, 1f,
                -0.5f,  0.5f,  0.5f,   0f, 1f,

                // -Z (back)
                 0.5f, -0.5f, -0.5f,   0f, 0f,
                -0.5f, -0.5f, -0.5f,   1f, 0f,
                -0.5f,  0.5f, -0.5f,   1f, 1f,
                 0.5f,  0.5f, -0.5f,   0f, 1f,

                // -X (left)
                -0.5f, -0.5f, -0.5f,   0f, 0f,
                -0.5f, -0.5f,  0.5f,   1f, 0f,
                -0.5f,  0.5f,  0.5f,   1f, 1f,
                -0.5f,  0.5f, -0.5f,   0f, 1f,

                // +X (right)
                 0.5f, -0.5f,  0.5f,   0f, 0f,
                 0.5f, -0.5f, -0.5f,   1f, 0f,
                 0.5f,  0.5f, -0.5f,   1f, 1f,
                 0.5f,  0.5f,  0.5f,   0f, 1f,

                // +Y (top)
                -0.5f,  0.5f,  0.5f,   0f, 0f,
                 0.5f,  0.5f,  0.5f,   1f, 0f,
                 0.5f,  0.5f, -0.5f,   1f, 1f,
                -0.5f,  0.5f, -0.5f,   0f, 1f,

                // -Y (bottom)
                -0.5f, -0.5f, -0.5f,   0f, 0f,
                 0.5f, -0.5f, -0.5f,   1f, 0f,
                 0.5f, -0.5f,  0.5f,   1f, 1f,
                -0.5f, -0.5f,  0.5f,   0f, 1f,
            };

            // 36 indices: 6 faces × 2 triangles × 3 vertices
            uint[] indices =
            {
                 0,  1,  2,   2,  3,  0,   // front
                 4,  5,  6,   6,  7,  4,   // back
                 8,  9, 10,  10, 11,  8,   // left
                12, 13, 14,  14, 15, 12,   // right
                16, 17, 18,  18, 19, 16,   // top
                20, 21, 22,  22, 23, 20,   // bottom
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Length * sizeof(float),
                vertices,
                BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indices.Length * sizeof(uint),
                indices,
                BufferUsageHint.StaticDraw);

            // Attribute 0 — position: 3 floats at byte offset 0
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(
                index:      0,
                size:       3,
                type:       VertexAttribPointerType.Float,
                normalized: false,
                stride:     5 * sizeof(float),
                offset:     0);

            // Attribute 1 — uv: 2 floats at byte offset 12 (= 3 floats)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(
                index:      1,
                size:       2,
                type:       VertexAttribPointerType.Float,
                normalized: false,
                stride:     5 * sizeof(float),
                offset:     3 * sizeof(float));

            GL.BindVertexArray(0);
        }
    }
}
