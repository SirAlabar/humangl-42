using System.Collections.Generic;
using HumanGL.Math;
using HumanGL.Rendering;
using HumanGL.Rendering.Interfaces;

namespace HumanGL.Scene
{
    // One node in the skeletal hierarchy.
    // Holds parameters only — no matrices.
    // Matrices are computed at draw time by Renderer.DrawNode().

    public class BodyNode
    {
        /* ── Identity ────────────────────────────────────────────────────── */

        public string           Name;
        public BodyNode?        Parent;
        public List<BodyNode>   Children = new List<BodyNode>();

        /* ── Spatial parameters ──────────────────────────────────────────── */

        // Position of this joint's pivot relative to its parent's pivot,
        // expressed in pre-scale parent space.
        public Vec3             LocalOffset;

        // True for nodes attached at the parent's top edge with a lateral offset
        // (upper arms). Stored explicitly so ReattachChild uses the right formula
        // even when the computed Y offset goes negative (arm taller than torso).
        public bool             LateralAttach;

        // Dimensions of the box drawn for this node (x, y, z scales).
        // Modified live by the UI panel sliders.
        public Vec3             Size;

        // Joint angles in radians — written every frame by Animator.
        public float            RotationX = 0f;
        public float            RotationY = 0f;
        public float            RotationZ = 0f;

        /* ── Rendering ───────────────────────────────────────────────────── */

        // Flat RGB colour — used when textures are disabled or missing.
        public Vec3             Colour;

        // NullTexture by default; replaced with a real Texture in the bonus phase.
        public ITexture         Texture;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public BodyNode(string name, Vec3 localOffset, Vec3 size, Vec3 colour, ITexture texture)
        {
            Name        = name;
            LocalOffset = localOffset;
            Size        = size;
            Colour      = colour;
            Texture     = texture;
        }

        /* ── Hierarchy helpers ───────────────────────────────────────────── */

        public void AddChild(BodyNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }
    }
}
