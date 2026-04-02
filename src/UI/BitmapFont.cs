using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;

namespace HumanGL.UI
{
    // Baked 5×7 ASCII bitmap font. No texture files needed.
    // Each glyph is a bitmask of 7 rows × 5 columns stored as 7 bytes.
    // DrawString() batches quads into a float[] and issues one GL draw call.

    public class BitmapFont : IDisposable
    {
        /* ── Config ──────────────────────────────────────────────────────── */

        public  int  GlyphW   = 5;
        public  int  GlyphH   = 7;
        public  int  Spacing  = 1;   // pixels between glyphs
        public  int  Scale    = 2;   // pixel magnification

        /* ── GL ──────────────────────────────────────────────────────────── */

        private int  _vao, _vbo;
        private bool _disposed;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public BitmapFont()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            // 2 floats per vertex (x, y), allocated lazily
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.BindVertexArray(0);
        }

        /* ── Public API ──────────────────────────────────────────────────── */

        // Returns pixel width of a string
        public int MeasureWidth(string text) => (text.Length * (GlyphW + Spacing) - Spacing) * Scale;

        // Returns pixel height of a glyph (scaled)
        public int ScaledHeight => GlyphH * Scale;

        // Fills verts list with screen-space quads for each lit pixel.
        // x/y are top-left in screen pixels (Y down).
        public void BuildQuads(string text, float x, float y, List<float> verts)
        {
            float cx = x;

            foreach (char ch in text)
            {
                byte[] glyph = GetGlyph(ch);

                for (int row = 0; row < GlyphH; row++)
                {
                    byte bits = glyph[row];
                    for (int col = 0; col < GlyphW; col++)
                    {
                        if ((bits & (1 << (GlyphW - 1 - col))) != 0)
                        {
                            float px = cx + col * Scale;
                            float py = y  + row * Scale;
                            float s  = Scale;
                            // Two triangles per pixel (Scale×Scale quad)
                            verts.Add(px);     verts.Add(py);
                            verts.Add(px + s); verts.Add(py);
                            verts.Add(px + s); verts.Add(py + s);
                            verts.Add(px);     verts.Add(py);
                            verts.Add(px + s); verts.Add(py + s);
                            verts.Add(px);     verts.Add(py + s);
                        }
                    }
                }

                cx += (GlyphW + Spacing) * Scale;
            }
        }

        public void Draw(List<float> verts)
        {
            if (verts.Count == 0) return;

            float[] data = verts.ToArray();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / 2);
            GL.BindVertexArray(0);
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteVertexArray(_vao);
                GL.DeleteBuffer(_vbo);
                _disposed = true;
            }
        }

        /* ── Glyph data ──────────────────────────────────────────────────── */

        private static byte[] GetGlyph(char c)
        {
            if (c >= 32 && c < 32 + Glyphs.Length / 7)
            {
                int base_ = (c - 32) * 7;
                return new byte[]
                {
                    Glyphs[base_+0], Glyphs[base_+1], Glyphs[base_+2],
                    Glyphs[base_+3], Glyphs[base_+4], Glyphs[base_+5],
                    Glyphs[base_+6]
                };
            }
            return new byte[7];
        }

        // 5×7 bitmaps for ASCII 32–126.
        // Each group of 7 bytes = one character, MSB = leftmost pixel.
        private static readonly byte[] Glyphs = new byte[]
        {
            // ' '
            0b00000,0b00000,0b00000,0b00000,0b00000,0b00000,0b00000,
            // '!'
            0b00100,0b00100,0b00100,0b00100,0b00000,0b00100,0b00000,
            // '"'
            0b01010,0b01010,0b00000,0b00000,0b00000,0b00000,0b00000,
            // '#'
            0b01010,0b11111,0b01010,0b01010,0b11111,0b01010,0b00000,
            // '$'
            0b00100,0b01111,0b10100,0b01110,0b00101,0b11110,0b00100,
            // '%'
            0b11000,0b11001,0b00010,0b00100,0b01000,0b10011,0b00011,
            // '&'
            0b01100,0b10010,0b10100,0b01000,0b10101,0b10010,0b01101,
            // '''
            0b00100,0b00100,0b00000,0b00000,0b00000,0b00000,0b00000,
            // '('
            0b00010,0b00100,0b01000,0b01000,0b01000,0b00100,0b00010,
            // ')'
            0b01000,0b00100,0b00010,0b00010,0b00010,0b00100,0b01000,
            // '*'
            0b00000,0b00100,0b10101,0b01110,0b10101,0b00100,0b00000,
            // '+'
            0b00000,0b00100,0b00100,0b11111,0b00100,0b00100,0b00000,
            // ','
            0b00000,0b00000,0b00000,0b00000,0b01100,0b00100,0b01000,
            // '-'
            0b00000,0b00000,0b00000,0b11111,0b00000,0b00000,0b00000,
            // '.'
            0b00000,0b00000,0b00000,0b00000,0b00000,0b01100,0b01100,
            // '/'
            0b00001,0b00010,0b00100,0b01000,0b10000,0b00000,0b00000,
            // '0'
            0b01110,0b10001,0b10011,0b10101,0b11001,0b10001,0b01110,
            // '1'
            0b00100,0b01100,0b00100,0b00100,0b00100,0b00100,0b01110,
            // '2'
            0b01110,0b10001,0b00001,0b00110,0b01000,0b10000,0b11111,
            // '3'
            0b11111,0b00010,0b00100,0b00010,0b00001,0b10001,0b01110,
            // '4'
            0b00010,0b00110,0b01010,0b10010,0b11111,0b00010,0b00010,
            // '5'
            0b11111,0b10000,0b11110,0b00001,0b00001,0b10001,0b01110,
            // '6'
            0b00110,0b01000,0b10000,0b11110,0b10001,0b10001,0b01110,
            // '7'
            0b11111,0b00001,0b00010,0b00100,0b01000,0b01000,0b01000,
            // '8'
            0b01110,0b10001,0b10001,0b01110,0b10001,0b10001,0b01110,
            // '9'
            0b01110,0b10001,0b10001,0b01111,0b00001,0b00010,0b01100,
            // ':'
            0b00000,0b01100,0b01100,0b00000,0b01100,0b01100,0b00000,
            // ';'
            0b00000,0b01100,0b01100,0b00000,0b01100,0b00100,0b01000,
            // '<'
            0b00010,0b00100,0b01000,0b10000,0b01000,0b00100,0b00010,
            // '='
            0b00000,0b00000,0b11111,0b00000,0b11111,0b00000,0b00000,
            // '>'
            0b01000,0b00100,0b00010,0b00001,0b00010,0b00100,0b01000,
            // '?'
            0b01110,0b10001,0b00001,0b00110,0b00100,0b00000,0b00100,
            // '@'
            0b01110,0b10001,0b00001,0b01101,0b10101,0b10101,0b01110,
            // 'A'
            0b01110,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001,
            // 'B'
            0b11110,0b10001,0b10001,0b11110,0b10001,0b10001,0b11110,
            // 'C'
            0b01110,0b10001,0b10000,0b10000,0b10000,0b10001,0b01110,
            // 'D'
            0b11110,0b10001,0b10001,0b10001,0b10001,0b10001,0b11110,
            // 'E'
            0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b11111,
            // 'F'
            0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b10000,
            // 'G'
            0b01110,0b10001,0b10000,0b10111,0b10001,0b10001,0b01111,
            // 'H'
            0b10001,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001,
            // 'I'
            0b01110,0b00100,0b00100,0b00100,0b00100,0b00100,0b01110,
            // 'J'
            0b00111,0b00010,0b00010,0b00010,0b10010,0b10010,0b01100,
            // 'K'
            0b10001,0b10010,0b10100,0b11000,0b10100,0b10010,0b10001,
            // 'L'
            0b10000,0b10000,0b10000,0b10000,0b10000,0b10000,0b11111,
            // 'M'
            0b10001,0b11011,0b10101,0b10101,0b10001,0b10001,0b10001,
            // 'N'
            0b10001,0b11001,0b10101,0b10011,0b10001,0b10001,0b10001,
            // 'O'
            0b01110,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110,
            // 'P'
            0b11110,0b10001,0b10001,0b11110,0b10000,0b10000,0b10000,
            // 'Q'
            0b01110,0b10001,0b10001,0b10001,0b10101,0b10010,0b01101,
            // 'R'
            0b11110,0b10001,0b10001,0b11110,0b10100,0b10010,0b10001,
            // 'S'
            0b01111,0b10000,0b10000,0b01110,0b00001,0b00001,0b11110,
            // 'T'
            0b11111,0b00100,0b00100,0b00100,0b00100,0b00100,0b00100,
            // 'U'
            0b10001,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110,
            // 'V'
            0b10001,0b10001,0b10001,0b10001,0b10001,0b01010,0b00100,
            // 'W'
            0b10001,0b10001,0b10001,0b10101,0b10101,0b11011,0b10001,
            // 'X'
            0b10001,0b10001,0b01010,0b00100,0b01010,0b10001,0b10001,
            // 'Y'
            0b10001,0b10001,0b01010,0b00100,0b00100,0b00100,0b00100,
            // 'Z'
            0b11111,0b00001,0b00010,0b00100,0b01000,0b10000,0b11111,
            // '['
            0b01110,0b01000,0b01000,0b01000,0b01000,0b01000,0b01110,
            // '\'
            0b10000,0b01000,0b00100,0b00010,0b00001,0b00000,0b00000,
            // ']'
            0b01110,0b00010,0b00010,0b00010,0b00010,0b00010,0b01110,
            // '^'
            0b00100,0b01010,0b10001,0b00000,0b00000,0b00000,0b00000,
            // '_'
            0b00000,0b00000,0b00000,0b00000,0b00000,0b00000,0b11111,
            // '`'
            0b01000,0b00100,0b00000,0b00000,0b00000,0b00000,0b00000,
            // 'a'
            0b00000,0b00000,0b01110,0b00001,0b01111,0b10001,0b01111,
            // 'b'
            0b10000,0b10000,0b11110,0b10001,0b10001,0b10001,0b11110,
            // 'c'
            0b00000,0b00000,0b01110,0b10000,0b10000,0b10001,0b01110,
            // 'd'
            0b00001,0b00001,0b01111,0b10001,0b10001,0b10001,0b01111,
            // 'e'
            0b00000,0b00000,0b01110,0b10001,0b11111,0b10000,0b01110,
            // 'f'
            0b00110,0b01001,0b01000,0b11100,0b01000,0b01000,0b01000,
            // 'g'
            0b00000,0b01111,0b10001,0b10001,0b01111,0b00001,0b01110,
            // 'h'
            0b10000,0b10000,0b11110,0b10001,0b10001,0b10001,0b10001,
            // 'i'
            0b00100,0b00000,0b01100,0b00100,0b00100,0b00100,0b01110,
            // 'j'
            0b00010,0b00000,0b00110,0b00010,0b00010,0b10010,0b01100,
            // 'k'
            0b10000,0b10000,0b10010,0b10100,0b11000,0b10100,0b10010,
            // 'l'
            0b01100,0b00100,0b00100,0b00100,0b00100,0b00100,0b01110,
            // 'm'
            0b00000,0b00000,0b11010,0b10101,0b10101,0b10001,0b10001,
            // 'n'
            0b00000,0b00000,0b11110,0b10001,0b10001,0b10001,0b10001,
            // 'o'
            0b00000,0b00000,0b01110,0b10001,0b10001,0b10001,0b01110,
            // 'p'
            0b00000,0b11110,0b10001,0b10001,0b11110,0b10000,0b10000,
            // 'q'
            0b00000,0b01111,0b10001,0b10001,0b01111,0b00001,0b00001,
            // 'r'
            0b00000,0b00000,0b10110,0b11001,0b10000,0b10000,0b10000,
            // 's'
            0b00000,0b00000,0b01111,0b10000,0b01110,0b00001,0b11110,
            // 't'
            0b01000,0b01000,0b11100,0b01000,0b01000,0b01001,0b00110,
            // 'u'
            0b00000,0b00000,0b10001,0b10001,0b10001,0b10011,0b01101,
            // 'v'
            0b00000,0b00000,0b10001,0b10001,0b10001,0b01010,0b00100,
            // 'w'
            0b00000,0b00000,0b10001,0b10101,0b10101,0b10101,0b01010,
            // 'x'
            0b00000,0b00000,0b10001,0b01010,0b00100,0b01010,0b10001,
            // 'y'
            0b00000,0b10001,0b10001,0b01111,0b00001,0b10001,0b01110,
            // 'z'
            0b00000,0b00000,0b11111,0b00010,0b00100,0b01000,0b11111,
            // '{'
            0b00110,0b00100,0b00100,0b01000,0b00100,0b00100,0b00110,
            // '|'
            0b00100,0b00100,0b00100,0b00000,0b00100,0b00100,0b00100,
            // '}'
            0b01100,0b00100,0b00100,0b00010,0b00100,0b00100,0b01100,
            // '~'
            0b00000,0b00000,0b01000,0b10101,0b00010,0b00000,0b00000,
        };
    }
}
