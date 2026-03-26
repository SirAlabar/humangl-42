using System;
using System.Collections.Generic;
using HumanGL.Math;
using HumanGL.Rendering;
using HumanGL.Rendering.Interfaces;

namespace HumanGL.Scene
{
    // Builds and owns the skeletal tree.
    // Phase 2: Torso only.
    // Phase 3: all 10 mandatory nodes wired up.

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

            // ── Torso (root) ────────────────────────────────────────────── //
            Root = Add("Torso",
                offset:  new Vec3( 0.00f,  0.00f, 0f),
                size:    new Vec3( 0.60f,  0.90f, 0.30f),
                colour:  new Vec3( 0.55f,  0.45f, 0.35f),
                texture: noTex);

            // Phase 3 will add the remaining 9 mandatory nodes here.
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

        // Create a node with no parent (used for root).
        private BodyNode Add(string name, Vec3 offset, Vec3 size, Vec3 colour, ITexture texture)
        {
            BodyNode node = new BodyNode(name, offset, size, colour, texture);
            _allNodes.Add(node);
            return node;
        }

        // Create a node and attach it as a child of parent.
        private BodyNode Add(string name, BodyNode parent, Vec3 offset, Vec3 size, Vec3 colour, ITexture texture)
        {
            BodyNode node = new BodyNode(name, offset, size, colour, texture);
            parent.AddChild(node);
            _allNodes.Add(node);
            return node;
        }
    }
}
