using System;
using System.Collections.Generic;
using HumanGL.Animation.States;
using HumanGL.Scene;

namespace HumanGL.Animation
{
    // State machine: detects AnimationMode changes, blends angles over 0.15 s.

    public static class Animator
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float BlendDuration = 0.15f;

        /* ── State registry ──────────────────────────────────────────────── */

        private static readonly Dictionary<AnimationState, AnimationStateBase> _states =
            new Dictionary<AnimationState, AnimationStateBase>
            {
                { AnimationState.Idle, new IdleState() },
                { AnimationState.Walk, new WalkState() },
            };

        /* ── Runtime ─────────────────────────────────────────────────────── */

        private static AnimationState _activeEnum  = AnimationState.Idle;
        private static float          _blendTimer  = BlendDuration;     // start fully blended
        private static float[]        _snapshot    = Array.Empty<float>();

        /* ── Public entry ────────────────────────────────────────────────── */

        public static void Update(HumanModel model, AppState state, float deltaTime)
        {
            if (state.AnimationMode != _activeEnum)
            {
                TakeSnapshot(model, state);
                _states[_activeEnum].Exit(model, state);
                _activeEnum = state.AnimationMode;
                _states[_activeEnum].Enter(model, state);
                _blendTimer = 0f;
            }

            _states[_activeEnum].Apply(model, state);

            if (_blendTimer < BlendDuration)
            {
                ApplyBlend(model, state, _blendTimer / BlendDuration);
                _blendTimer += deltaTime;
            }
        }

        /* ── Blend helpers ───────────────────────────────────────────────── */

        // Layout: [node0.rx, node0.ry, node0.rz, node1.rx, ...], then TorsoOffsetY.
        private static void TakeSnapshot(HumanModel model, AppState state)
        {
            IReadOnlyList<BodyNode> nodes = model.AllNodes;
            _snapshot = new float[nodes.Count * 3 + 1];

            for (int i = 0; i < nodes.Count; i++)
            {
                _snapshot[i * 3 + 0] = nodes[i].RotationX;
                _snapshot[i * 3 + 1] = nodes[i].RotationY;
                _snapshot[i * 3 + 2] = nodes[i].RotationZ;
            }

            _snapshot[nodes.Count * 3] = state.TorsoOffsetY;
        }

        private static void ApplyBlend(HumanModel model, AppState state, float alpha)
        {
            IReadOnlyList<BodyNode> nodes = model.AllNodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].RotationX = Lerp(_snapshot[i * 3 + 0], nodes[i].RotationX, alpha);
                nodes[i].RotationY = Lerp(_snapshot[i * 3 + 1], nodes[i].RotationY, alpha);
                nodes[i].RotationZ = Lerp(_snapshot[i * 3 + 2], nodes[i].RotationZ, alpha);
            }

            state.TorsoOffsetY = Lerp(_snapshot[nodes.Count * 3], state.TorsoOffsetY, alpha);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
