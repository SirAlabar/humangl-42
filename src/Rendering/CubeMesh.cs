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

        // Build a cube whose UVs sample a face-atlas texture.
        // Layout expected (4 columns × 3 rows):
        //        [ top  ]
        // [left ][front ][right ][ back ]
        //        [ btm  ]
        public static CubeMesh CreateHeadAtlas()
        {
            CubeMesh m = new CubeMesh();
            m.BuildAtlas();
            return m;
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

        // Rebuilds the existing VAO/VBO with atlas UVs for the head face texture.
        // Each face samples its own region of the 4×3 atlas.
        // UV origin is bottom-left (OpenGL convention). Image is pre-flipped on load.
        //
        // After vertical flip on load, atlas rows in UV space:
        //   original top    row → UV y = [0.667, 1.000]
        //   original middle row → UV y = [0.333, 0.667]
        //   original bottom row → UV y = [0.000, 0.333]
        // Atlas columns: left=0, front=1, right=2, back=3 → x = col/4 .. (col+1)/4
        private void BuildAtlas()
        {
            const float W = 1f / 4f;  // cell width
            const float H = 1f / 3f;  // cell height

            // col × row for each face (image layout, pre-flip)
            // After flip: UV_y_min = 1 - (row+1)*H,  UV_y_max = 1 - row*H
            float FrontX0 = 1 * W, FrontX1 = 2 * W, FrontY0 = H,   FrontY1 = 2 * H; // col1 row1
            float BackX0  = 3 * W, BackX1  = 4 * W, BackY0  = H,   BackY1  = 2 * H; // col3 row1
            float LeftX0  = 0 * W, LeftX1  = 1 * W, LeftY0  = H,   LeftY1  = 2 * H; // col0 row1
            float RightX0 = 2 * W, RightX1 = 3 * W, RightY0 = H,   RightY1 = 2 * H; // col2 row1
            float TopX0   = 1 * W, TopX1   = 2 * W, TopY0   = 2*H, TopY1   = 3 * H; // col1 row0 → top in UV
            float BotX0   = 1 * W, BotX1   = 2 * W, BotY0   = 0,   BotY1   = H;     // col1 row2 → bottom in UV

            float[] vertices =
            {
                // +Z (front face of head)
                -0.5f, -0.5f,  0.5f,   FrontX0, FrontY0,
                 0.5f, -0.5f,  0.5f,   FrontX1, FrontY0,
                 0.5f,  0.5f,  0.5f,   FrontX1, FrontY1,
                -0.5f,  0.5f,  0.5f,   FrontX0, FrontY1,

                // -Z (back of head)
                 0.5f, -0.5f, -0.5f,   BackX0, BackY0,
                -0.5f, -0.5f, -0.5f,   BackX1, BackY0,
                -0.5f,  0.5f, -0.5f,   BackX1, BackY1,
                 0.5f,  0.5f, -0.5f,   BackX0, BackY1,

                // -X (left side of head)
                -0.5f, -0.5f, -0.5f,   LeftX0, LeftY0,
                -0.5f, -0.5f,  0.5f,   LeftX1, LeftY0,
                -0.5f,  0.5f,  0.5f,   LeftX1, LeftY1,
                -0.5f,  0.5f, -0.5f,   LeftX0, LeftY1,

                // +X (right side of head)
                 0.5f, -0.5f,  0.5f,   RightX0, RightY0,
                 0.5f, -0.5f, -0.5f,   RightX1, RightY0,
                 0.5f,  0.5f, -0.5f,   RightX1, RightY1,
                 0.5f,  0.5f,  0.5f,   RightX0, RightY1,

                // +Y (top of head)
                -0.5f,  0.5f,  0.5f,   TopX0, TopY0,
                 0.5f,  0.5f,  0.5f,   TopX1, TopY0,
                 0.5f,  0.5f, -0.5f,   TopX1, TopY1,
                -0.5f,  0.5f, -0.5f,   TopX0, TopY1,

                // -Y (bottom/chin)
                -0.5f, -0.5f, -0.5f,   BotX0, BotY0,
                 0.5f, -0.5f, -0.5f,   BotX1, BotY0,
                 0.5f, -0.5f,  0.5f,   BotX1, BotY1,
                -0.5f, -0.5f,  0.5f,   BotX0, BotY1,
            };

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Length * sizeof(float),
                vertices,
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);
        }
    }
}
