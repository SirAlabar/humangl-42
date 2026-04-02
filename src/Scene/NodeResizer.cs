using HumanGL.Math;

namespace HumanGL.Scene
{
    // Single-node resize: sets Size.Y (uniform for Head) and recomputes
    // direct-child LocalOffsets so joints stay flush.
    // Does NOT cascade to grandchildren — gives independent per-segment control.

    public static class NodeResizer
    {
        public const float SizeMin = 0.20f;
        public const float SizeMax = 2.00f;

        public static void SetSizeY(BodyNode node, float newSizeY)
        {
            float clamped = System.MathF.Max(SizeMin, System.MathF.Min(SizeMax, newSizeY));

            if (node.Name == "Head")
            {
                float ratio = node.Size.Y > 0f ? clamped / node.Size.Y : 1f;
                node.Size = new Vec3(node.Size.X * ratio, clamped, node.Size.Z * ratio);
            }
            else
            {
                node.Size = new Vec3(node.Size.X, clamped, node.Size.Z);
            }

            // Recompute direct children's attachment offsets
            foreach (BodyNode child in node.Children)
            {
                ReattachChild(node, child);
            }

            // Also reposition this node relative to its own parent
            if (node.Parent != null)
            {
                ReattachChild(node.Parent, node);
            }
        }

        // Also recompute this node's own offset relative to its parent
        // (needed when a parent was resized independently).
        public static void ReattachChild(BodyNode parent, BodyNode child)
        {
            if (child.LateralAttach)
            {
                // Lateral child whose top is flush with parent top (UpperArms on Torso).
                // Checked first — Y can go negative when arm is taller than torso, so
                // we must not rely on the sign of LocalOffset.Y to detect this case.
                child.LocalOffset = new Vec3(
                    child.LocalOffset.X,
                    0.5f * (1f - child.Size.Y / parent.Size.Y),
                    child.LocalOffset.Z);
            }
            else if (child.LocalOffset.Y < 0f)
            {
                // Chain child hanging below parent (forearms, shins, thighs, hands, feet)
                child.LocalOffset = new Vec3(
                    child.LocalOffset.X,
                    -0.5f * (1f + child.Size.Y / parent.Size.Y),
                    child.LocalOffset.Z);
            }
            else if (child.LocalOffset.Y > 0f &&
                     child.LocalOffset.X == 0f &&
                     child.LocalOffset.Z == 0f)
            {
                // Top-attached child stacked above parent (Neck on Torso, Head on Neck)
                child.LocalOffset = new Vec3(
                    0f,
                    0.5f * (1f + child.Size.Y / parent.Size.Y),
                    0f);
            }
        }
    }
}
