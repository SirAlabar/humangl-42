using HumanGL.Scene;

namespace HumanGL.UI
{
    // A labelled horizontal slider bound to one or two BodyNodes (L+R mirror).
    // Track rect is updated each Draw call so hit-testing is always current.

    public class Slider
    {
        public string Label;
        public string NodeL;          // primary node name
        public string? NodeR;         // optional right-side mirror (null = no mirror)

        // Track rect — set every frame by UiPanel.DrawSlider()
        public float TrackX, TrackY, TrackW, TrackH;

        // Full-row hit bounds — set every frame by UiPanel.DrawSlider()
        public float HitX, HitW, HitY, HitH;

        public Slider(string label, string nodeL, string? nodeR = null)
        {
            Label = label;
            NodeL = nodeL;
            NodeR = nodeR;
        }

        // Normalised value [0..1] from the primary node's current Size.Y
        public float GetNorm(HumanModel model)
        {
            float y = model.GetNode(NodeL).Size.Y;
            return (y - NodeResizer.SizeMin) / (NodeResizer.SizeMax - NodeResizer.SizeMin);
        }

        // Apply a normalised value [0..1] to the node(s)
        public void SetNorm(float t, HumanModel model)
        {
            float sizeY = NodeResizer.SizeMin + t * (NodeResizer.SizeMax - NodeResizer.SizeMin);
            NodeResizer.SetSizeY(model.GetNode(NodeL), sizeY);
            if (NodeR != null)
                NodeResizer.SetSizeY(model.GetNode(NodeR), sizeY);
        }

        // Returns true if (px, py) is within the full slider row
        public bool HitTest(float px, float py)
        {
            return px >= HitX && px <= HitX + HitW &&
                   py >= HitY && py <= HitY + HitH;
        }

        // Converts an X pixel position to a normalised [0..1] value
        public float XToNorm(float px)
        {
            float t = (px - TrackX) / TrackW;
            return System.MathF.Max(0f, System.MathF.Min(1f, t));
        }
    }
}
