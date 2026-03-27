using System;
using System.Collections.Generic;
using HumanGL.Math;
using HumanGL.Rendering;
using HumanGL.Rendering.Interfaces;

namespace HumanGL.Scene
{
    // Builds and owns the skeletal tree.

    public class HumanModel
    {
        /* ── Public ──────────────────────────────────────────────────────── */

        public BodyNode                 Root     { get; }
        public IReadOnlyList<BodyNode>  AllNodes => _allNodes;

        /* ── State ───────────────────────────────────────────────────────── */

        private readonly List<BodyNode> _allNodes = new List<BodyNode>();

        /* ── Constructor ─────────────────────────────────────────────────── */

        public HumanModel()
        {
            ITexture noTex = new NullTexture();

            // ── Torso (root) ─────────────────────────────────────────────── //
            Root = Add("Torso",
                offset:  new Vec3( 0.00f,  0.00f, 0.00f),
                size:    new Vec3( 0.50f,  0.95f, 0.30f),
                colour:  new Vec3( 0.55f,  0.45f, 0.35f),
                texture: noTex);

            // ── Head ─────────────────────────────────────────────────────── //
            Add("Head", Root,
                offset:  new Vec3( 0.00f,  0.721f, 0.00f),
                size:    new Vec3( 0.42f,  0.42f, 0.42f),
                colour:  new Vec3( 0.85f,  0.70f, 0.55f),
                texture: noTex);

            // ── Left arm ─────────────────────────────────────────────────── //
            BodyNode lua = Add("LeftUpperArm", Root,
                offset:  new Vec3(-0.68f,  0.211f, 0.00f),
                size:    new Vec3( 0.18f,  0.55f, 0.18f),
                colour:  new Vec3( 0.25f,  0.40f, 0.75f),
                texture: noTex);

            Add("LeftForeArm", lua,
                offset:  new Vec3( 0.00f, -1.00f, 0.00f),
                size:    new Vec3( 0.18f,  0.55f, 0.18f),
                colour:  new Vec3( 0.35f,  0.55f, 0.85f),
                texture: noTex);

            // ── Right arm ────────────────────────────────────────────────── //
            BodyNode rua = Add("RightUpperArm", Root,
                offset:  new Vec3( 0.68f,  0.211f, 0.00f),
                size:    new Vec3( 0.18f,  0.55f, 0.18f),
                colour:  new Vec3( 0.75f,  0.25f, 0.25f),
                texture: noTex);

            Add("RightForeArm", rua,
                offset:  new Vec3( 0.00f, -1.00f, 0.00f),
                size:    new Vec3( 0.18f,  0.55f, 0.18f),
                colour:  new Vec3( 0.85f,  0.40f, 0.40f),
                texture: noTex);

            // ── Left leg ─────────────────────────────────────────────────── //
            BodyNode lt = Add("LeftThigh", Root,
                offset:  new Vec3(-0.28f, -0.842f, 0.00f),
                size:    new Vec3( 0.22f,  0.65f, 0.22f),
                colour:  new Vec3( 0.30f,  0.30f, 0.45f),
                texture: noTex);

            Add("LeftShin", lt,
                offset:  new Vec3( 0.00f, -1.00f, 0.00f),
                size:    new Vec3( 0.22f,  0.65f, 0.22f),
                colour:  new Vec3( 0.40f,  0.40f, 0.60f),
                texture: noTex);

            // ── Right leg ────────────────────────────────────────────────── //
            BodyNode rt = Add("RightThigh", Root,
                offset:  new Vec3( 0.28f, -0.842f, 0.00f),
                size:    new Vec3( 0.22f,  0.65f, 0.22f),
                colour:  new Vec3( 0.30f,  0.30f, 0.45f),
                texture: noTex);

            Add("RightShin", rt,
                offset:  new Vec3( 0.00f, -1.00f, 0.00f),
                size:    new Vec3( 0.22f,  0.65f, 0.22f),
                colour:  new Vec3( 0.40f,  0.40f, 0.60f),
                texture: noTex);
        }

        /* ── Lookup ──────────────────────────────────────────────────────── */

        public BodyNode GetNode(string name)
        {
            BodyNode? found = _allNodes.Find(n => n.Name == name);
            if (found == null)
            {
                throw new Exception($"HumanModel: node '{name}' not found");
            }
            return found;
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        private BodyNode Add(string name, Vec3 offset, Vec3 size, Vec3 colour, ITexture texture)
        {
            BodyNode node = new BodyNode(name, offset, size, colour, texture);
            _allNodes.Add(node);
            return node;
        }

        private BodyNode Add(string name, BodyNode parent, Vec3 offset, Vec3 size, Vec3 colour, ITexture texture)
        {
            BodyNode node = new BodyNode(name, offset, size, colour, texture);
            parent.AddChild(node);
            _allNodes.Add(node);
            return node;
        }
    }
}