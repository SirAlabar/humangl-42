using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public class TPoseState : AnimationStateBase
    {
        public override void Apply(HumanModel model, AppState state)
        {
            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            model.GetNode("LeftUpperArm").RotationZ  = -MathF.PI / 2f;
            model.GetNode("RightUpperArm").RotationZ =  MathF.PI / 2f;

            state.TorsoOffsetY = 0f;
        }
    }
}
