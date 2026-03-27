using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public class IdleState : AnimationStateBase
    {
        public override void Apply(HumanModel model, AppState state)
        {
            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            state.TorsoOffsetY = MathF.Sin(state.Time * 1.5f) * 0.03f;
        }
    }
}
