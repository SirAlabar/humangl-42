using System;
using HumanGL.Scene;

namespace HumanGL.Animation.States
{
    // Karate kata — looping sequence of 5 techniques with forward stepping.
    //
    // Sequence (seconds within Period = 9.0):
    //   0.00 – 0.80   Yoi          ready stance settle
    //   0.80 – 2.10   Oi-zuki      right lunge punch  (+Z step)
    //   2.10 – 2.70   Recovery     return to guard
    //   2.70 – 3.80   Gyaku-zuki   left reverse punch  (+Z step)
    //   3.80 – 4.40   Recovery     return to guard
    //   4.40 – 5.80   Mae Geri     right front kick    (+Z step)
    //   5.80 – 6.40   Recovery     land + guard
    //   6.40 – 7.50   Jodan Uke    left high block     (+Z step)
    //   7.50 – 9.00   Yame         return to start (Z resets)
    //
    // Biomechanical principles applied:
    //   • Kinetic chain: leg → hip → torso → shoulder → arm
    //   • Anticipation: wind-up before every strike
    //   • Easing: EaseOut on strikes, EaseInOut on movement
    //   • TorsoOffsetZ for forward progression

    public class KungFuState : AnimationStateBase
    {
        /* ── Phase boundaries (absolute seconds) ────────────────────────── */

        private const float Period     = 9.00f;
        private const float YoiEnd     = 0.80f;
        private const float Oizuki1End = 2.10f;
        private const float Guard1End  = 2.70f;
        private const float GyakuEnd   = 3.80f;
        private const float Guard2End  = 4.40f;
        private const float KickEnd    = 5.80f;
        private const float Guard3End  = 6.40f;
        private const float JodanEnd   = 7.50f;
        // 7.50 – 9.00 : Yame

        /* ── Z positions at each step ────────────────────────────────────── */

        private const float Z0 = 0.00f;
        private const float Z1 = 0.30f;
        private const float Z2 = 0.60f;
        private const float Z3 = 0.90f;
        private const float Z4 = 1.20f;

        /* ── State callbacks ─────────────────────────────────────────────── */

        public override void Apply(HumanModel model, AppState state)
        {
            foreach (BodyNode node in model.AllNodes)
            {
                node.RotationX = 0f;
                node.RotationY = 0f;
                node.RotationZ = 0f;
            }

            float t = state.Time % Period;

            if (t < YoiEnd)
            {
                DoYoi(model, state, Norm(t, 0f, YoiEnd));
            }
            else if (t < Oizuki1End)
            {
                DoOizuki(model, state, Norm(t, YoiEnd, Oizuki1End), Z0, Z1);
            }
            else if (t < Guard1End)
            {
                DoRecovery(model, state, Norm(t, Oizuki1End, Guard1End), Z1, leftForward: true);
            }
            else if (t < GyakuEnd)
            {
                DoGyakuzuki(model, state, Norm(t, Guard1End, GyakuEnd), Z1, Z2);
            }
            else if (t < Guard2End)
            {
                DoRecovery(model, state, Norm(t, GyakuEnd, Guard2End), Z2, leftForward: false);
            }
            else if (t < KickEnd)
            {
                DoMaegeri(model, state, Norm(t, Guard2End, KickEnd), Z2, Z3);
            }
            else if (t < Guard3End)
            {
                DoRecovery(model, state, Norm(t, KickEnd, Guard3End), Z3, leftForward: true);
            }
            else if (t < JodanEnd)
            {
                DoJodanUke(model, state, Norm(t, Guard3End, JodanEnd), Z3, Z4);
            }
            else
            {
                DoYame(model, state, Norm(t, JodanEnd, Period), Z4, Z0);
            }
        }

        /* ── Techniques ──────────────────────────────────────────────────── */

        // Settle from neutral into guard stance.
        private static void DoYoi(HumanModel model, AppState state, float a)
        {
            float e = EaseInOut(a);
            SetGuard(model, e);
            state.TorsoOffsetY = Lerp(0f, -0.10f, e);
            state.TorsoOffsetZ = Z0;
        }

        // Right lunge punch (Oi-zuki).
        // Sub-phases:  [0, 0.20) anticipation   [0.20, 0.62) step   [0.62, 0.88) strike   [0.88, 1] hold
        private static void DoOizuki(HumanModel model, AppState state, float a, float zFrom, float zTo)
        {
            if (a < 0.20f)
            {
                // Anticipation — slight opposite-side torso turn, rear arm winds up
                float b = EaseInOut(a / 0.20f);
                SetGuard(model, 1f);
                model.GetNode("RightUpperArm").RotationX += b * 0.18f; // slight pullback
                model.GetNode("Torso").RotationY          = b * 0.14f; // opposite turn
                state.TorsoOffsetY = -0.10f;
                state.TorsoOffsetZ = zFrom;
            }
            else if (a < 0.62f)
            {
                // Step forward into left-forward zenkutsu-dachi
                float b   = EaseInOut((a - 0.20f) / 0.42f);
                float bob = MathF.Sin(b * MathF.PI) * 0.07f; // natural vertical dip in mid-step

                // Legs transition: guard → zenkutsu-dachi (left foot forward)
                model.GetNode("LeftThigh").RotationX   = Lerp(-0.18f, -0.38f, b);
                model.GetNode("LeftShin").RotationX    = Lerp( 0.28f,  0.44f, EaseOut(b));
                model.GetNode("RightThigh").RotationX  = Lerp(-0.18f,  0.08f, b);
                model.GetNode("RightShin").RotationX   = Lerp( 0.28f,  0.05f, b);

                // Arms maintain guard during step
                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationY = Lerp(0.14f, 0f, b);

                state.TorsoOffsetY = -0.10f - bob;
                state.TorsoOffsetZ = Lerp(zFrom, zTo, b);
            }
            else if (a < 0.88f)
            {
                // Strike — kinetic chain: stance → hip rotates → arm follows with slight delay
                float b    = EaseOut((a - 0.62f) / 0.26f);
                float armB = EaseOut(MathF.Max(0f, (a - 0.68f) / 0.20f)); // arm lags body

                // Stance locked
                SetZenkutsuLeft(model);

                // Hip/torso drives left into right punch
                model.GetNode("Torso").RotationY = -b * 0.24f;

                // Punching arm (right) extends
                model.GetNode("RightUpperArm").RotationX = Lerp(-0.50f, -1.30f, armB);
                model.GetNode("RightUpperArm").RotationZ = Lerp( 0.25f,  0.05f, armB);
                model.GetNode("RightForeArm").RotationX  = Lerp(-0.70f,  0.00f, armB);

                // Lead arm chambers to hip (hikite)
                model.GetNode("LeftUpperArm").RotationX  = Lerp(-0.80f,  0.25f, b);
                model.GetNode("LeftUpperArm").RotationZ  = Lerp(-0.25f,  0.55f, b);
                model.GetNode("LeftForeArm").RotationX   = Lerp(-1.00f, -0.50f, b);

                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
            else
            {
                // Hold extended punch
                SetZenkutsuLeft(model);
                model.GetNode("Torso").RotationY          = -0.24f;
                model.GetNode("RightUpperArm").RotationX  = -1.30f;
                model.GetNode("RightUpperArm").RotationZ  =  0.05f;
                model.GetNode("RightForeArm").RotationX   =  0.00f;
                model.GetNode("LeftUpperArm").RotationX   =  0.25f;
                model.GetNode("LeftUpperArm").RotationZ   =  0.55f;
                model.GetNode("LeftForeArm").RotationX    = -0.50f;
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
        }

        // Left reverse punch (Gyaku-zuki) — starts in left-forward stance.
        // Sub-phases: [0, 0.20) wind-up   [0.20, 0.55) step   [0.55, 0.85) strike   [0.85, 1] hold
        private static void DoGyakuzuki(HumanModel model, AppState state, float a, float zFrom, float zTo)
        {
            if (a < 0.20f)
            {
                // Wind-up: counter-rotate hips slightly before driving
                float b = EaseInOut(a / 0.20f);
                SetZenkutsuLeft(model);
                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationY = Lerp(-0.24f, 0.12f, b);
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zFrom;
            }
            else if (a < 0.55f)
            {
                // Step forward with right foot into right-forward stance
                float b   = EaseInOut((a - 0.20f) / 0.35f);
                float bob = MathF.Sin(b * MathF.PI) * 0.06f;

                model.GetNode("LeftThigh").RotationX   = Lerp(-0.38f,  0.08f, b);
                model.GetNode("LeftShin").RotationX    = Lerp( 0.44f,  0.05f, b);
                model.GetNode("RightThigh").RotationX  = Lerp( 0.08f, -0.38f, b);
                model.GetNode("RightShin").RotationX   = Lerp( 0.05f,  0.44f, EaseOut(b));

                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationY = Lerp(0.12f, 0f, b);

                state.TorsoOffsetY = -0.10f - bob;
                state.TorsoOffsetZ = Lerp(zFrom, zTo, b);
            }
            else if (a < 0.85f)
            {
                // Strike — hip counter-rotates, left arm drives
                float b    = EaseOut((a - 0.55f) / 0.30f);
                float armB = EaseOut(MathF.Max(0f, (a - 0.62f) / 0.23f));

                SetZenkutsuRight(model);
                model.GetNode("Torso").RotationY = b * 0.24f; // hip drives right for left punch

                // Left punch
                model.GetNode("LeftUpperArm").RotationX  = Lerp( 0.25f, -1.30f, armB);
                model.GetNode("LeftUpperArm").RotationZ  = Lerp( 0.55f,  0.05f, armB);
                model.GetNode("LeftForeArm").RotationX   = Lerp(-0.50f,  0.00f, armB);

                // Right chambers
                model.GetNode("RightUpperArm").RotationX = Lerp(-1.30f,  0.25f, b);
                model.GetNode("RightUpperArm").RotationZ = Lerp( 0.05f,  0.55f, b);
                model.GetNode("RightForeArm").RotationX  = Lerp( 0.00f, -0.50f, b);

                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
            else
            {
                SetZenkutsuRight(model);
                model.GetNode("Torso").RotationY          =  0.24f;
                model.GetNode("LeftUpperArm").RotationX   = -1.30f;
                model.GetNode("LeftUpperArm").RotationZ   =  0.05f;
                model.GetNode("LeftForeArm").RotationX    =  0.00f;
                model.GetNode("RightUpperArm").RotationX  =  0.25f;
                model.GetNode("RightUpperArm").RotationZ  =  0.55f;
                model.GetNode("RightForeArm").RotationX   = -0.50f;
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
        }

        // Right front kick (Mae Geri).
        // Sub-phases: [0, 0.22) weight shift   [0.22, 0.50) knee lift   [0.50, 0.68) snap kick
        //             [0.68, 0.85) retract      [0.85, 1] land
        private static void DoMaegeri(HumanModel model, AppState state, float a, float zFrom, float zTo)
        {
            if (a < 0.22f)
            {
                // Shift weight onto left leg before lifting
                float b = EaseInOut(a / 0.22f);
                SetZenkutsuRight(model, 1f - b * 0.5f);
                SetGuardArms(model, 1f);
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zFrom;
            }
            else if (a < 0.50f)
            {
                // Knee rises — support leg (left) bends more for balance
                float b = EaseOut((a - 0.22f) / 0.28f);

                model.GetNode("LeftThigh").RotationX   = -0.30f;
                model.GetNode("LeftShin").RotationX    =  0.40f;
                model.GetNode("RightThigh").RotationX  =  Lerp(-0.20f,  0.72f, b);
                model.GetNode("RightShin").RotationX   =  Lerp( 0.22f,  0.72f, b); // shin folds up

                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationX = -b * 0.12f; // slight lean back for balance

                state.TorsoOffsetY = Lerp(-0.12f, -0.20f, b);
                state.TorsoOffsetZ = zFrom;
            }
            else if (a < 0.68f)
            {
                // Kick extension — rapid snap
                float b = Snap((a - 0.50f) / 0.18f);

                model.GetNode("LeftThigh").RotationX   = -0.30f;
                model.GetNode("LeftShin").RotationX    =  0.40f;
                model.GetNode("RightThigh").RotationX  =  Lerp( 0.72f, 1.10f, b);
                model.GetNode("RightShin").RotationX   =  Lerp( 0.72f,-0.15f, b); // leg straightens

                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationX = Lerp(-0.12f, -0.18f, b);

                state.TorsoOffsetY = -0.20f;
                state.TorsoOffsetZ = zFrom;
            }
            else if (a < 0.85f)
            {
                // Retract and land into left-forward stance (+Z step)
                float b = EaseInOut((a - 0.68f) / 0.17f);

                model.GetNode("LeftThigh").RotationX   = Lerp(-0.30f, -0.38f, b);
                model.GetNode("LeftShin").RotationX    = Lerp( 0.40f,  0.44f, b);
                model.GetNode("RightThigh").RotationX  = Lerp( 1.10f,  0.08f, b);
                model.GetNode("RightShin").RotationX   = Lerp(-0.15f,  0.05f, EaseOut(b));

                SetGuardArms(model, 1f);
                model.GetNode("Torso").RotationX = Lerp(-0.18f, 0f, b);

                state.TorsoOffsetY = Lerp(-0.20f, -0.12f, b);
                state.TorsoOffsetZ = Lerp(zFrom, zTo, b);
            }
            else
            {
                SetZenkutsuLeft(model);
                SetGuardArms(model, 1f);
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
        }

        // Left high block (Jodan Uke).
        // Sub-phases: [0, 0.28) step   [0.28, 0.70) block sweep   [0.70, 1] hold
        private static void DoJodanUke(HumanModel model, AppState state, float a, float zFrom, float zTo)
        {
            if (a < 0.28f)
            {
                // Step forward with right foot
                float b   = EaseInOut(a / 0.28f);
                float bob = MathF.Sin(b * MathF.PI) * 0.06f;

                model.GetNode("LeftThigh").RotationX   = Lerp(-0.38f,  0.08f, b);
                model.GetNode("LeftShin").RotationX    = Lerp( 0.44f,  0.05f, b);
                model.GetNode("RightThigh").RotationX  = Lerp( 0.08f, -0.38f, b);
                model.GetNode("RightShin").RotationX   = Lerp( 0.05f,  0.44f, EaseOut(b));

                SetGuardArms(model, 1f);

                state.TorsoOffsetY = -0.10f - bob;
                state.TorsoOffsetZ = Lerp(zFrom, zTo, b);
            }
            else if (a < 0.70f)
            {
                // Left arm sweeps up in high block arc
                float b    = EaseOut((a - 0.28f) / 0.42f);
                float snap = Snap((a - 0.28f) / 0.42f);

                SetZenkutsuRight(model);
                model.GetNode("Torso").RotationY = -b * 0.16f; // body drives into block

                model.GetNode("LeftUpperArm").RotationX  = Lerp(-0.80f, -1.60f, snap);
                model.GetNode("LeftUpperArm").RotationZ  = Lerp(-0.25f, -0.30f, snap);
                model.GetNode("LeftForeArm").RotationX   = Lerp(-1.00f, -0.45f, snap);

                // Right hand chambers (hikite)
                model.GetNode("RightUpperArm").RotationX = Lerp(-0.50f,  0.25f, b);
                model.GetNode("RightUpperArm").RotationZ = Lerp( 0.25f,  0.55f, b);
                model.GetNode("RightForeArm").RotationX  = Lerp(-0.70f, -0.50f, b);

                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
            else
            {
                SetZenkutsuRight(model);
                model.GetNode("Torso").RotationY          = -0.16f;
                model.GetNode("LeftUpperArm").RotationX   = -1.60f;
                model.GetNode("LeftUpperArm").RotationZ   = -0.30f;
                model.GetNode("LeftForeArm").RotationX    = -0.45f;
                model.GetNode("RightUpperArm").RotationX  =  0.25f;
                model.GetNode("RightUpperArm").RotationZ  =  0.55f;
                model.GetNode("RightForeArm").RotationX   = -0.50f;
                state.TorsoOffsetY = -0.12f;
                state.TorsoOffsetZ = zTo;
            }
        }

        // Recovery: relax stance back to guard between techniques.
        private static void DoRecovery(HumanModel model, AppState state, float a, float z, bool leftForward)
        {
            float b = EaseInOut(a);
            // Ease legs from current stance toward guard
            float tFront = leftForward ? -0.38f : 0.08f;
            float sFront = leftForward ?  0.44f : 0.05f;
            float tBack  = leftForward ?  0.08f : -0.38f;
            float sBack  = leftForward ?  0.05f :  0.44f;
            model.GetNode("LeftThigh").RotationX   = Lerp(tFront, -0.18f, b);
            model.GetNode("LeftShin").RotationX    = Lerp(sFront,  0.28f, b);
            model.GetNode("RightThigh").RotationX  = Lerp(tBack,  -0.18f, b);
            model.GetNode("RightShin").RotationX   = Lerp(sBack,   0.28f, b);
            SetGuardArms(model, 1f);
            state.TorsoOffsetY = -0.10f;
            state.TorsoOffsetZ = z;
        }

        // Yame: return to origin.
        private static void DoYame(HumanModel model, AppState state, float a, float zFrom, float zTo)
        {
            float b = EaseInOut(a);
            SetGuard(model, 1f - b * 0.40f); // gradually relax to near-neutral
            state.TorsoOffsetY = Lerp(-0.10f,  0f,    b);
            state.TorsoOffsetZ = Lerp(zFrom,   zTo,   EaseInOut(MathF.Min(1f, a * 1.3f)));
        }

        /* ── Pose helpers ────────────────────────────────────────────────── */

        // Full guard stance (alpha 0 = neutral, 1 = full guard).
        private static void SetGuard(HumanModel model, float alpha)
        {
            model.GetNode("LeftThigh").RotationX   = Lerp(0f, -0.18f, alpha);
            model.GetNode("RightThigh").RotationX  = Lerp(0f, -0.18f, alpha);
            model.GetNode("LeftShin").RotationX    = Lerp(0f,  0.28f, alpha);
            model.GetNode("RightShin").RotationX   = Lerp(0f,  0.28f, alpha);
            SetGuardArms(model, alpha);
        }

        // Guard arms only (lead left high, rear right mid).
        private static void SetGuardArms(HumanModel model, float alpha)
        {
            model.GetNode("LeftUpperArm").RotationX  = Lerp(0f, -0.80f, alpha);
            model.GetNode("LeftForeArm").RotationX   = Lerp(0f, -1.00f, alpha);
            model.GetNode("LeftUpperArm").RotationZ  = Lerp(0f, -0.25f, alpha);
            model.GetNode("RightUpperArm").RotationX = Lerp(0f, -0.50f, alpha);
            model.GetNode("RightForeArm").RotationX  = Lerp(0f, -0.70f, alpha);
            model.GetNode("RightUpperArm").RotationZ = Lerp(0f,  0.25f, alpha);
        }

        // Zenkutsu-dachi, left foot forward.
        private static void SetZenkutsuLeft(HumanModel model, float alpha = 1f)
        {
            model.GetNode("LeftThigh").RotationX   = Lerp(0f, -0.38f, alpha);
            model.GetNode("LeftShin").RotationX    = Lerp(0f,  0.44f, alpha);
            model.GetNode("RightThigh").RotationX  = Lerp(0f,  0.08f, alpha);
            model.GetNode("RightShin").RotationX   = Lerp(0f,  0.05f, alpha);
        }

        // Zenkutsu-dachi, right foot forward.
        private static void SetZenkutsuRight(HumanModel model, float alpha = 1f)
        {
            model.GetNode("RightThigh").RotationX  = Lerp(0f, -0.38f, alpha);
            model.GetNode("RightShin").RotationX   = Lerp(0f,  0.44f, alpha);
            model.GetNode("LeftThigh").RotationX   = Lerp(0f,  0.08f, alpha);
            model.GetNode("LeftShin").RotationX    = Lerp(0f,  0.05f, alpha);
        }

        /* ── Easing functions ────────────────────────────────────────────── */

        private static float EaseIn(float x)    => x * x;
        private static float EaseOut(float x)   => 1f - (1f - x) * (1f - x);
        private static float EaseInOut(float x) => x * x * (3f - 2f * x);

        // Impulse curve — fast rise and fall, peak at t=0.5. Used for strike snaps.
        private static float Snap(float x) => MathF.Pow(MathF.Sin(x * MathF.PI), 2f);

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Norm(float t, float start, float end) => (t - start) / (end - start);
    }
}
