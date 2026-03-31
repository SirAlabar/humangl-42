using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public class DiscoState : AnimationStateBase
    {
        /* ── Phase windows (seconds, looping) ────────────────────────────── */

        private const float Period = 1.60f;   // one full beat cycle

        /* ── State callbacks ─────────────────────────────────────────────── */

        public override void Apply(HumanModel model, AppState state)
        {
            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            float t    = state.Time;
            float beat = MathF.Sin(t * (2f * MathF.PI / Period));   // –1 … +1

            // ── Torso sway ────────────────────────────────────────────────── //
            model.GetNode("Torso").RotationZ  =  beat * 0.18f;

            // ── Classic disco point: one arm up, one arm down, swap on beat ── //
            // LeftUpperArm: rises to –1.20 (pointing up-back) on beat == +1
            model.GetNode("LeftUpperArm").RotationX  =  beat * -1.10f;
            model.GetNode("LeftUpperArm").RotationZ  = -0.40f + beat * -0.20f;
            model.GetNode("LeftForeArm").RotationX   =  beat *  0.30f;

            // RightUpperArm mirrors
            model.GetNode("RightUpperArm").RotationX = -beat * -1.10f;
            model.GetNode("RightUpperArm").RotationZ =  0.40f + beat *  0.20f;
            model.GetNode("RightForeArm").RotationX  = -beat *  0.30f;

            // ── Legs: slight groove bounce ────────────────────────────────── //
            float bounce = MathF.Abs(beat);           // 0 at beat zero, 1 at extremes
            float kneeFlap = MathF.Sin(t * (4f * MathF.PI / Period)) * 0.15f;

            model.GetNode("LeftThigh").RotationX   =  kneeFlap;
            model.GetNode("RightThigh").RotationX  = -kneeFlap;
            model.GetNode("LeftShin").RotationX    =  MathF.Max(0f,  kneeFlap) * 0.5f;
            model.GetNode("RightShin").RotationX   =  MathF.Max(0f, -kneeFlap) * 0.5f;

            // ── Bob ───────────────────────────────────────────────────────── //
            state.TorsoOffsetY = bounce * 0.06f;
        }
    }
}
