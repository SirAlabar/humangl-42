using System.Collections.Generic;
using HumanGL.Animation;

namespace HumanGL.UI
{
    // Game-style character customiser panel — drawn on the LEFT side of the screen.
    // Sections: animation buttons, HEAD, BODY, ARMS, LEGS.
    // Sliders are mouse-draggable; clicking an animation button switches the mode.

    public class UiPanel
    {
        /* ── Layout ──────────────────────────────────────────────────────── */

        public  const int PanelW = 300;
        private const int Pad    = 12;
        private const int TrackH = 8;
        private const int RowH   = 34;   // label + track height per slider
        private const int BtnH   = 30;
        private const int BtnW   = 86;

        /* ── Colour palette ──────────────────────────────────────────────── */
        // Inspired by ARK-style character UI (dark navy + cyan accents)

        private static readonly float[] BgCol      = { 0.04f, 0.07f, 0.12f };
        private static readonly float[] PanelLine  = { 0.10f, 0.35f, 0.50f };
        private static readonly float[] SecCol     = { 0.10f, 0.80f, 0.90f };  // cyan
        private static readonly float[] LabelCol   = { 0.75f, 0.85f, 0.90f };
        private static readonly float[] TrackBg    = { 0.12f, 0.18f, 0.22f };
        private static readonly float[] TrackFill  = { 0.05f, 0.65f, 0.75f };
        private static readonly float[] ThumbCol   = { 0.85f, 0.95f, 1.00f };
        private static readonly float[] BtnNorm    = { 0.08f, 0.18f, 0.25f };
        private static readonly float[] BtnActive  = { 0.05f, 0.55f, 0.65f };
        private static readonly float[] BtnBorder  = { 0.10f, 0.55f, 0.70f };
        private static readonly float[] TitleCol   = { 0.15f, 0.90f, 1.00f };

        /* ── Sliders ─────────────────────────────────────────────────────── */

        private readonly List<Slider> _sliders = new List<Slider>
        {
            // HEAD
            new Slider("Head Size",    "Head"),
            // BODY
            new Slider("Torso Height", "Torso"),
            new Slider("Neck Size",    "Neck"),
            // ARMS (mirror L→R)
            new Slider("Upper Arm",    "LeftUpperArm",  "RightUpperArm"),
            new Slider("Forearm",      "LeftForeArm",   "RightForeArm"),
            // LEGS (mirror L→R)
            new Slider("Thigh",        "LeftThigh",     "RightThigh"),
            new Slider("Shin",         "LeftShin",      "RightShin"),
        };

        // Section start indices in _sliders
        private static readonly (string name, int from, int to)[] Sections =
        {
            ("HEAD", 0, 1),
            ("BODY", 1, 3),
            ("ARMS", 3, 5),
            ("LEGS", 5, 7),
        };

        /* ── Animation buttons ───────────────────────────────────────────── */

        private static readonly (string label, AnimationState state)[] AnimButtons =
        {
            ("Idle",   AnimationState.Idle),
            ("Walk",   AnimationState.Walk),
            ("Jump",   AnimationState.Jump),
            ("Disco",  AnimationState.Disco),
            ("Karate", AnimationState.Karate),
            ("TPose",  AnimationState.TPose),
        };

        // Rects for anim buttons — set each Draw call
        private readonly float[] _btnX = new float[6];
        private readonly float[] _btnY = new float[6];

        /* ── Interaction state ───────────────────────────────────────────── */

        private int   _dragging    = -1;    // index into _sliders, -1 = none
        private int   _hovered     = -1;    // index into _sliders, -1 = none
        public  bool  IsDragging   => _dragging >= 0;

        /* ── Public API ──────────────────────────────────────────────────── */

        public bool IsInPanel(float mouseX) => mouseX < PanelW;

        // Returns true if the event was consumed (don't forward to camera).
        public bool OnMouseDown(float mx, float my, AppState state)
        {
            if (!IsInPanel(mx)) return false;

            // Check animation buttons
            for (int i = 0; i < AnimButtons.Length; i++)
            {
                if (mx >= _btnX[i] && mx <= _btnX[i] + BtnW &&
                    my >= _btnY[i] && my <= _btnY[i] + BtnH)
                {
                    SwitchAnim(state, AnimButtons[i].state);
                    return true;
                }
            }

            // Check sliders
            if (state.Model == null) return true;

            for (int i = 0; i < _sliders.Count; i++)
            {
                if (_sliders[i].HitTest(mx, my))
                {
                    _dragging = i;
                    _sliders[i].SetNorm(_sliders[i].XToNorm(mx), state.Model);
                    return true;
                }
            }

            return true;  // still consume — don't start camera drag inside panel
        }

        public void OnMouseMove(float mx, float my, AppState state)
        {
            if (_dragging >= 0)
            {
                if (state.Model != null)
                    _sliders[_dragging].SetNorm(_sliders[_dragging].XToNorm(mx), state.Model);
                return;
            }

            // Update hover
            _hovered = -1;
            if (state.Model != null && IsInPanel(mx))
            {
                for (int i = 0; i < _sliders.Count; i++)
                {
                    if (_sliders[i].HitTest(mx, my))
                    {
                        _hovered = i;
                        break;
                    }
                }
            }
        }

        public void OnMouseUp()
        {
            _dragging = -1;
        }

        public void OnMouseLeave()
        {
            _hovered = -1;
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw(UiRenderer ui, AppState state, int screenW, int screenH)
        {
            ui.Begin(screenW, screenH);

            float x = 0, y = 0;

            // ── Panel background ───────────────────────────────────────────── //
            ui.DrawRect(x, y, PanelW, screenH, BgCol[0], BgCol[1], BgCol[2], 0.60f);

            ui.DrawRect(PanelW - 2, y, 2, screenH, PanelLine[0], PanelLine[1], PanelLine[2], 1f);

            y += Pad;

            // ── Title ──────────────────────────────────────────────────────── //
            DrawCentred(ui, "HumanGL  42", x, y, PanelW, TitleCol[0], TitleCol[1], TitleCol[2]);
            y += 24;
            ui.DrawRect(x + Pad, y, PanelW - Pad * 2, 1, PanelLine[0], PanelLine[1], PanelLine[2], 1f);
            y += 8;

            // ── Animation buttons (2 rows × 3) ────────────────────────────── //
            DrawSectionLabel(ui, "ANIMATION", x, y);
            y += 22;

            for (int i = 0; i < AnimButtons.Length; i++)
            {
                int  col  = i % 3;
                int  row  = i / 3;
                float bx  = x + Pad + col * (BtnW + 4);
                float by  = y + row * (BtnH + 4);

                _btnX[i] = bx;
                _btnY[i] = by;

                bool active = state.AnimationMode == AnimButtons[i].state;
                float[] bg  = active ? BtnActive : BtnNorm;
                ui.DrawRect(bx, by, BtnW, BtnH, bg[0], bg[1], bg[2], 1f);
                DrawOutline(ui, bx, by, BtnW, BtnH, BtnBorder[0], BtnBorder[1], BtnBorder[2]);
                float tr = active ? 1.00f : LabelCol[0];
                float tg = active ? 1.00f : LabelCol[1];
                float tb = active ? 1.00f : LabelCol[2];
                DrawCentred(ui, AnimButtons[i].label, bx, by + (BtnH - ui.FontHeight) / 2, BtnW, tr, tg, tb);
            }

            y += 2 * (BtnH + 4) + 8;
            ui.DrawRect(x + Pad, y, PanelW - Pad * 2, 1, PanelLine[0], PanelLine[1], PanelLine[2], 1f);
            y += 8;

            // ── Sliders per section ────────────────────────────────────────── //
            if (state.Model != null)
            {
                foreach (var (sName, from, to) in Sections)
                {
                    float sectionLabelY = y;
                    DrawSectionLabel(ui, sName, x, y);
                    y += 22;

                    for (int i = from; i < to; i++)
                    {
                        bool highlighted = (i == _dragging) || (i == _hovered && _dragging < 0);
                        // First slider in section absorbs the section label gap
                        float topExtend = (i == from) ? (y - sectionLabelY) : 0f;
                        y = DrawSlider(ui, _sliders[i], x, y, state, highlighted, topExtend);
                    }

                    y += 4;
                }
            }

            y += 4;
            ui.DrawRect(x + Pad, y, PanelW - Pad * 2, 1, PanelLine[0], PanelLine[1], PanelLine[2], 1f);
            y += 8;

            // ── Camera hints ──────────────────────────────────────────────── //
            DrawSectionLabel(ui, "CAMERA", x, y);
            y += 22;
            ui.DrawText("[drag] orbit",   x + Pad, y, LabelCol[0], LabelCol[1], LabelCol[2]); y += 18;
            ui.DrawText("[scroll] zoom",  x + Pad, y, LabelCol[0], LabelCol[1], LabelCol[2]); y += 18;

            // Texture toggle
            bool  texOn = state.TexturesEnabled;
            float tr2 = texOn ? 0.10f : LabelCol[0];
            float tg2 = texOn ? 0.90f : LabelCol[1];
            float tb2 = texOn ? 0.60f : LabelCol[2];
            ui.DrawText($"[T] Textures: {(texOn ? "ON" : "OFF")}", x + Pad, y, tr2, tg2, tb2);

            ui.End();
        }

        /* ── Drawing helpers ─────────────────────────────────────────────── */

        private float DrawSlider(UiRenderer ui, Slider slider, float panelX, float y, AppState state, bool highlighted = false, float topExtend = 0f)
        {
            float rowStartY = y;
            float trackX   = panelX + Pad;
            float trackW   = PanelW - Pad * 2;

            // Highlight background for hovered / active row (includes section label gap if first in section)
            if (highlighted)
                ui.DrawRect(panelX, rowStartY - topExtend, PanelW, 44 + topExtend, 0.10f, 0.40f, 0.55f, 0.30f);

            // Label + value %
            float norm = state.Model != null ? slider.GetNorm(state.Model) : 0.5f;
            norm = System.MathF.Max(0f, System.MathF.Min(1f, norm));
            string label = $"{slider.Label}  {(int)(norm * 100)}%";
            ui.DrawText(label, trackX, y, LabelCol[0], LabelCol[1], LabelCol[2]);
            y += 16;

            // Track background
            float ty = y + 1;
            ui.DrawRect(trackX, ty, trackW, TrackH, TrackBg[0], TrackBg[1], TrackBg[2], 1f);

            // Fill
            ui.DrawRect(trackX, ty, trackW * norm, TrackH, TrackFill[0], TrackFill[1], TrackFill[2], 1f);

            // Thumb
            float thumbX = trackX + trackW * norm - 4;
            ui.DrawRect(thumbX, ty - 2, 8, TrackH + 4, ThumbCol[0], ThumbCol[1], ThumbCol[2], 1f);

            // Store track rect for visual reference
            slider.TrackX = trackX;
            slider.TrackY = ty;
            slider.TrackW = trackW;
            slider.TrackH = TrackH;

            float rowEndY = y + TrackH + 20;

            // Full-row hit bounds — full panel width, covers section label gap if first in section
            slider.HitX = panelX;
            slider.HitW = PanelW;
            slider.HitY = rowStartY - topExtend;
            slider.HitH = rowEndY - rowStartY + topExtend;

            return rowEndY;
        }

        private static void DrawSectionLabel(UiRenderer ui, string text, float panelX, float y)
        {
            ui.DrawText(text, panelX + Pad, y, SecCol[0], SecCol[1], SecCol[2]);
        }

        private static void DrawCentred(UiRenderer ui, string text, float panelX, float y, float panelW, float r, float g, float b)
        {
            float tw = ui.MeasureText(text);
            ui.DrawText(text, panelX + (panelW - tw) / 2f, y, r, g, b);
        }

        private static void DrawOutline(UiRenderer ui, float x, float y, float w, float h, float r, float g, float b)
        {
            float a = 1f;
            ui.DrawRect(x,         y,         w, 1, r, g, b, a);
            ui.DrawRect(x,         y + h - 1, w, 1, r, g, b, a);
            ui.DrawRect(x,         y,         1, h, r, g, b, a);
            ui.DrawRect(x + w - 1, y,         1, h, r, g, b, a);
        }

        private static void SwitchAnim(AppState state, AnimationState next)
        {
            if (state.AnimationMode != next)
            {
                state.PreviousAnim  = state.AnimationMode;
                state.AnimationMode = next;
            }
        }
    }
}
