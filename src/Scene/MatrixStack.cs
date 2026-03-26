using System.Collections.Generic;
using HumanGL.Math;

namespace HumanGL.Scene
{
    // A stack of Mat4 transforms that accumulates a world-space matrix
    // as the DrawNode tree traversal pushes and pops frames.
    //
    // Usage pattern per node:
    //   stack.Push()          — clone current top for this node's scope
    //   stack.Multiply(...)   — accumulate translate / rotate / scale
    //   upload stack.Top      — send world transform to GPU
    //   DrawNode(children)    — recurse; children inherit this frame
    //   stack.Pop()           — discard this node's frame, restore parent

    public class MatrixStack
    {
        /* ── State ───────────────────────────────────────────────────────── */

        private readonly Stack<Mat4> _stack = new Stack<Mat4>();

        /* ── Constructor ─────────────────────────────────────────────────── */

        public MatrixStack()
        {
            _stack.Push(Mat4.Identity());
        }

        /* ── API ─────────────────────────────────────────────────────────── */

        // Current accumulated world transform — read-only.
        public Mat4 Top => _stack.Peek();

        // Clone the top entry so the current scope can modify it independently.
        public void Push()
        {
            _stack.Push(_stack.Peek());
        }

        // Discard the top entry, restoring the parent scope's transform.
        public void Pop()
        {
            if (_stack.Count > 1)
            {
                _stack.Pop();
            }
        }

        // Post-multiply: Top = Top * m
        // Because we multiply on the right, transforms applied first in code
        // are the outermost (parent) transforms — which is what we want.
        public void Multiply(Mat4 m)
        {
            Mat4 top = _stack.Pop();
            _stack.Push(top * m);
        }

        // Clear the stack and push a fresh identity.
        public void Reset()
        {
            _stack.Clear();
            _stack.Push(Mat4.Identity());
        }
    }
}
