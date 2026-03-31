using System.IO;
using HumanGL.Rendering.Interfaces;
using HumanGL.Scene;

namespace HumanGL.Rendering
{
    // Loads textures from disk and assigns them to model nodes by name.
    // Keeps texture concerns out of HumanModel (SRP).
    //
    // Drop any supported image (jpg, jpeg, png, bmp, tga, ppm, gif, psd)
    // into assets/textures/ using these names:
    //
    //   face     — head (shown on all 6 faces of the head cube)
    //   skin     — neck, hands
    //   torso    — torso
    //   leftarm  — left upper arm + forearm
    //   rightarm — right upper arm + forearm
    //   pants    — thighs + shins
    //   shoes    — feet
    //
    // Extension is auto-detected — face.png, face.jpg, face.jpeg all work.
    // Missing files silently fall back to flat colour (NullTexture).

    public static class ModelTextureLoader
    {
        private static readonly string[] Extensions =
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".tga", ".ppm", ".gif", ".psd"
        };

        public static void Load(HumanModel model, string textureDir)
        {
            ITexture face    = Find(textureDir, "face");
            ITexture skin    = Find(textureDir, "skin");
            ITexture torso   = Find(textureDir, "torso");
            ITexture larm    = Find(textureDir, "leftarm");
            ITexture rarm    = Find(textureDir, "rightarm");
            ITexture pants   = Find(textureDir, "pants");
            ITexture shoes   = Find(textureDir, "shoes");

            // Head gets its own face texture (wraps all 6 sides of the cube)
            model.GetNode("Head").Texture         = face;

            // Skin tone for neck and hands
            model.GetNode("Neck").Texture         = skin;
            model.GetNode("LeftHand").Texture     = skin;
            model.GetNode("RightHand").Texture    = skin;

            model.GetNode("Torso").Texture        = torso;

            model.GetNode("LeftUpperArm").Texture  = larm;
            model.GetNode("LeftForeArm").Texture   = larm;

            model.GetNode("RightUpperArm").Texture = rarm;
            model.GetNode("RightForeArm").Texture  = rarm;

            model.GetNode("LeftThigh").Texture    = pants;
            model.GetNode("LeftShin").Texture     = pants;
            model.GetNode("RightThigh").Texture   = pants;
            model.GetNode("RightShin").Texture    = pants;

            model.GetNode("LeftFoot").Texture     = shoes;
            model.GetNode("RightFoot").Texture    = shoes;
        }

        // Tries each extension in order, returns first match or NullTexture.
        private static ITexture Find(string dir, string name)
        {
            foreach (string ext in Extensions)
            {
                string path = Path.Combine(dir, name + ext);
                if (File.Exists(path))
                {
                    return Texture.FromFile(path);
                }
            }

            return new NullTexture();
        }
    }
}
