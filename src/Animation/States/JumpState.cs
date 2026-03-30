using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public class JumpState : AnimationStateBase
    {
        /* ── Phase windows (seconds) ─────────────────────────────────────── */

        private const float CrouchEnd = 0.20f;
        private const float AscentEnd = 0.40f;
        private const float ApexEnd   = 0.60f;
        private const float LandEnd   = 0.80f;

        /* ── Runtime ─────────────────────────────────────────────────────── */

        private float _startTime;

        /* ── State callbacks ─────────────────────────────────────────────── */

        public override void Enter(HumanModel model, AppState state)
        {
            _startTime = state.Time;
        }

        public override void Apply(HumanModel model, AppState state)
        {
            float t = state.Time - _startTime;

            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            if (t < CrouchEnd)
            {
                float a = t / CrouchEnd;

                model.GetNode("LeftThigh").RotationX     = -a * 0.30f;
                model.GetNode("RightThigh").RotationX    = -a * 0.30f;
                model.GetNode("LeftShin").RotationX      =  a * 0.45f;
                model.GetNode("RightShin").RotationX     =  a * 0.45f;
                model.GetNode("LeftUpperArm").RotationX  =  a * 0.60f;
                model.GetNode("RightUpperArm").RotationX =  a * 0.60f;
                model.GetNode("LeftForeArm").RotationX   = -a * 0.40f;
                model.GetNode("RightForeArm").RotationX  = -a * 0.40f;
                state.TorsoOffsetY = -a * 0.15f;
            }
            else if (t < AscentEnd)
            {
                float a = (t - CrouchEnd) / (AscentEnd - CrouchEnd);

                model.GetNode("LeftThigh").RotationX     = -0.30f * (1f - a);
                model.GetNode("RightThigh").RotationX    = -0.30f * (1f - a);
                model.GetNode("LeftShin").RotationX      =  0.45f * (1f - a);
                model.GetNode("RightShin").RotationX     =  0.45f * (1f - a);
                model.GetNode("LeftUpperArm").RotationX  =  Lerp(0.60f, -1.00f, a);
                model.GetNode("RightUpperArm").RotationX =  Lerp(0.60f, -1.00f, a);
                model.GetNode("LeftForeArm").RotationX   = -0.40f * (1f - a);
                model.GetNode("RightForeArm").RotationX  = -0.40f * (1f - a);
                state.TorsoOffsetY = Lerp(-0.15f, 0.60f, a);
            }
            else if (t < ApexEnd)
            {
                float a = (t - AscentEnd) / (ApexEnd - AscentEnd);

                model.GetNode("LeftUpperArm").RotationX  = -1.00f * (1f - a);
                model.GetNode("RightUpperArm").RotationX = -1.00f * (1f - a);
                model.GetNode("LeftUpperArm").RotationZ  = -a * 1.20f;
                model.GetNode("RightUpperArm").RotationZ =  a * 1.20f;
                model.GetNode("LeftThigh").RotationX     = -a * 0.12f;
                model.GetNode("RightThigh").RotationX    = -a * 0.12f;
                state.TorsoOffsetY = 0.60f;
            }
            else if (t < LandEnd)
            {
                float a = (t - ApexEnd) / (LandEnd - ApexEnd);

                model.GetNode("LeftUpperArm").RotationZ  = -1.20f * (1f - a);
                model.GetNode("RightUpperArm").RotationZ =  1.20f * (1f - a);
                model.GetNode("LeftThigh").RotationX     =  Lerp(-0.12f, -0.30f, a);
                model.GetNode("RightThigh").RotationX    =  Lerp(-0.12f, -0.30f, a);
                model.GetNode("LeftShin").RotationX      =  a * 0.35f;
                model.GetNode("RightShin").RotationX     =  a * 0.35f;
                state.TorsoOffsetY = Lerp(0.60f, -0.10f, a);
            }
            else
            {
                state.TorsoOffsetY  = 0f;
                state.AnimationMode = AnimationState.Idle;
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
