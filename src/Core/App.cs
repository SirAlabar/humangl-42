using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using HumanGL.Animation;
using HumanGL.Rendering;
using HumanGL.UI;

namespace HumanGL
{
    // Thin orchestrator — owns AppState, extends GameWindow.
    // Delegates input to InputHandler, rendering to Renderer.

    public class App : GameWindow
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private AppState    _state  = null!;
        private Renderer    _renderer = null!;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public App()
            : base(
                new GameWindowSettings
                {
                    UpdateFrequency = 60.0
                },
                new NativeWindowSettings
                {
                    Title         = "HumanGL - 42",
                    ClientSize    = new OpenTK.Mathematics.Vector2i(1280, 720),
                    API           = ContextAPI.OpenGL,
                    APIVersion    = new Version(4, 1),
                    Profile       = ContextProfile.Core,
                    IsEventDriven = false
                }
            )
        {
        }

        /* ── OnLoad ──────────────────────────────────────────────────────── */

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.08f, 0.08f, 0.10f, 1.0f);

            Console.WriteLine(
                $"App: OpenGL {GL.GetString(StringName.Version)}" +
                $" / GLSL {GL.GetString(StringName.ShadingLanguageVersion)}"
            );

            _state    = new AppState();
            _renderer = new Renderer();

            LoadShader();
            _renderer.Init(_state);
        }

        /* ── OnUpdateFrame ───────────────────────────────────────────────── */

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            float dt = (float)args.Time;

            _state.Time += dt;

            InputHandler.Update(_state, KeyboardState, dt);

            if (_state.Model != null)
            {
                Animator.Update(_state.Model, _state, dt);
            }
        }

        /* ── OnRenderFrame ───────────────────────────────────────────────── */

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            _renderer.Draw(_state, Size.X, Size.Y);

            SwapBuffers();
        }

        /* ── OnResize ────────────────────────────────────────────────────── */

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        /* ── Mouse events ────────────────────────────────────────────────── */

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButton.Left)
            {
                float mx = MousePosition.X;
                float my = MousePosition.Y;
                if (!_renderer.Panel.OnMouseDown(mx, my, _state))
                {
                    InputHandler.OnMouseDown(_state, mx, my);
                }
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            _renderer.Panel.OnMouseMove(e.X, e.Y, _state);

            if (!_renderer.Panel.IsDragging)
            {
                InputHandler.OnMouseMove(_state, e.X, e.Y);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButton.Left)
            {
                _renderer.Panel.OnMouseUp();
                InputHandler.OnMouseUp(_state);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            InputHandler.OnMouseWheel(_state, e.OffsetY);
        }

        /* ── OnUnload ────────────────────────────────────────────────────── */

        protected override void OnUnload()
        {
            base.OnUnload();

            _state.Shader?.Dispose();
            _renderer.Dispose();
        }

        /* ── Load helpers ────────────────────────────────────────────────── */

        private void LoadShader()
        {
            _state.Shader = new Shader();

            if (!_state.Shader.Load("shaders/vertex.glsl", "shaders/fragment.glsl"))
            {
                throw new Exception("App: shader load failed");
            }
        }
    }
}