using HumanGL.Rendering.Interfaces;

namespace HumanGL.Rendering
{
    // Null Object — returned by Texture.FromFile() when loading fails.
    // Every method is a silent no-op.
    // App.cs never needs to check if the texture loaded — it just calls Bind/Unbind.

    public class NullTexture : ITexture
    {
        /* ── ITexture ────────────────────────────────────────────────────── */

        public bool IsLoaded => false;

        public void Bind(int unit = 0) { }

        public void Unbind() { }
    }
}