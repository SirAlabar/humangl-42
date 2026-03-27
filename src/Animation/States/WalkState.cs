using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public class WalkState : AnimationStateBase
    {
        public override void Apply(HumanModel model, AppState state)
        {
            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            float t     = state.Time;
            float swing = MathF.Sin(t * 3.0f) * 0.45f;

            model.GetNode("LeftThigh").RotationX     =  swing;
            model.GetNode("RightThigh").RotationX    = -swing;
            model.GetNode("LeftShin").RotationX      =  MathF.Max(0f,  swing) * 0.7f;
            model.GetNode("RightShin").RotationX     =  MathF.Max(0f, -swing) * 0.7f;
            model.GetNode("LeftUpperArm").RotationX  = -swing;
            model.GetNode("RightUpperArm").RotationX =  swing;
            model.GetNode("LeftForeArm").RotationX   = -MathF.Max(0f,  swing) * 0.6f;
            model.GetNode("RightForeArm").RotationX  = -MathF.Max(0f, -swing) * 0.6f;

            state.TorsoOffsetY = MathF.Sin(t * 6.0f) * 0.02f;
        }
    }
}
