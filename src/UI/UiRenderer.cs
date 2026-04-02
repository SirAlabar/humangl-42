using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using HumanGL.Math;

namespace HumanGL.UI
{
    // Renders 2D UI quads and text using an embedded orthographic shader.
    // Operates in screen-pixel space (origin = top-left, Y down).

    public class UiRenderer : IDisposable
    {
        /* ── Embedded shaders ────────────────────────────────────────────── */

        private const string VertSrc = @"#version 410 core
layout(location = 0) in vec2 a_pos;
uniform mat4 u_proj;
void main() { gl_Position = u_proj * vec4(a_pos, 0.0, 1.0); }";

        private const string FragSrc = @"#version 410 core
uniform vec4 u_colour;
out vec4 fragColour;
void main() { fragColour = u_colour; }";

        /* ── Fields ──────────────────────────────────────────────────────── */

        private int        _prog;
        private int        _projLoc, _colLoc;
        private int        _quadVao, _quadVbo;
        private BitmapFont _font;
        private bool       _disposed;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public UiRenderer()
        {
            _prog = BuildShader();
            _projLoc = GL.GetUniformLocation(_prog, "u_proj");
            _colLoc  = GL.GetUniformLocation(_prog, "u_colour");

            _quadVao = GL.GenVertexArray();
            _quadVbo = GL.GenBuffer();
            GL.BindVertexArray(_quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.BindVertexArray(0);

            _font = new BitmapFont();
        }

        /* ── Begin / End ─────────────────────────────────────────────────── */

        public void Begin(int screenW, int screenH)
        {
            GL.UseProgram(_prog);
            GL.Disable(EnableCap.DepthTest);

            // Orthographic: top-left = (0,0), bottom-right = (screenW, screenH)
            Mat4 proj = Mat4.Ortho(0, screenW, screenH, 0, -1, 1);
            float[] data = proj.ToArray();
            GL.UniformMatrix4(_projLoc, 1, false, data);
        }

        public void End()
        {
            GL.Enable(EnableCap.DepthTest);
        }

        /* ── Draw primitives ─────────────────────────────────────────────── */

        // Filled axis-aligned rectangle.
        public void DrawRect(float x, float y, float w, float h, float r, float g, float b, float a = 0.85f)
        {
            SetColour(r, g, b, a);

            float[] verts =
            {
                x,     y,
                x + w, y,
                x + w, y + h,
                x,     y,
                x + w, y + h,
                x,     y + h,
            };

            UploadAndDraw(verts);
        }

        // Horizontal progress bar (filled portion 0..1).
        public void DrawBar(float x, float y, float w, float h,
                            float fill,
                            float br, float bg, float bb,   // background colour
                            float fr, float fg, float fb)   // fill colour
        {
            DrawRect(x, y, w, h, br, bg, bb);
            DrawRect(x, y, w * fill, h, fr, fg, fb);
        }

        // Text at pixel position (top-left).
        public void DrawText(string text, float x, float y, float r, float g, float b)
        {
            if (string.IsNullOrEmpty(text)) return;

            SetColour(r, g, b, 1f);

            var verts = new List<float>();
            _font.BuildQuads(text, x, y, verts);
            _font.Draw(verts);
        }

        /* ── Helpers ─────────────────────────────────────────────────────── */

        public int MeasureText(string text) => _font.MeasureWidth(text);
        public int FontHeight                => _font.ScaledHeight;

        private void SetColour(float r, float g, float b, float a)
        {
            GL.Uniform4(_colLoc, r, g, b, a);
        }

        private void UploadAndDraw(float[] verts)
        {
            GL.BindVertexArray(_quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, verts.Length / 2);
            GL.BindVertexArray(0);
        }

        /* ── Shader builder ──────────────────────────────────────────────── */

        private static int BuildShader()
        {
            int vert = Compile(ShaderType.VertexShader,   VertSrc);
            int frag = Compile(ShaderType.FragmentShader, FragSrc);

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, vert);
            GL.AttachShader(prog, frag);
            GL.LinkProgram(prog);
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0)
            {
                Console.Error.WriteLine($"UiRenderer shader link error: {GL.GetProgramInfoLog(prog)}");
            }

            return prog;
        }

        private static int Compile(ShaderType type, string src)
        {
            int s = GL.CreateShader(type);
            GL.ShaderSource(s, src);
            GL.CompileShader(s);
            GL.GetShader(s, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
            {
                Console.Error.WriteLine($"UiRenderer shader compile error: {GL.GetShaderInfoLog(s)}");
            }
            return s;
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                _font.Dispose();
                GL.DeleteVertexArray(_quadVao);
                GL.DeleteBuffer(_quadVbo);
                GL.DeleteProgram(_prog);
                _disposed = true;
            }
        }
    }
}
