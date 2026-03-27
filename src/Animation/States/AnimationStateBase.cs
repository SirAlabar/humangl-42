using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    public abstract class AnimationStateBase
    {
        public virtual void Enter(HumanModel model, AppState state) { }
        public virtual void Exit (HumanModel model, AppState state) { }

        // Writes target angles directly to model nodes and state.TorsoOffsetY.
        public abstract void Apply(HumanModel model, AppState state);
    }
}
