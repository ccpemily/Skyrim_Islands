using UnityEngine;
using Verse;

namespace SkyrimIslands.MainTabs
{
    public class Window_SkyIslandControlButton : Window
    {
        private const float ButtonSize = 48f;
        private const float MarginTop = 48f;
        private const float MarginRight = 14f;

        public override Vector2 InitialSize => new Vector2(ButtonSize, ButtonSize);

        protected override float Margin => 0f;

        public Window_SkyIslandControlButton()
        {
            layer = WindowLayer.Super;
            drawShadow = false;
            doWindowBackground = false;
            draggable = false;
            resizeable = false;
            focusWhenOpened = false;
            preventCameraMotion = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            soundAppear = null;
            soundClose = null;
        }

        public override void Notify_ResolutionChanged()
        {
            SetInitialSizeAndPosition();
        }

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(
                UI.screenWidth - MarginRight - ButtonSize,
                MarginTop,
                ButtonSize,
                ButtonSize);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect buttonRect = inRect.AtZero();

            Widgets.DrawBoxSolidWithOutline(
                buttonRect,
                new Color(0.15f, 0.15f, 0.15f, 0.92f),
                SkyIslandControlWindowUtility.IsControlWindowOpen()
                    ? new Color(1f, 0.84f, 0.35f, 0.95f)
                    : (Mouse.IsOver(buttonRect)
                        ? new Color(0.75f, 0.75f, 0.75f, 0.95f)
                        : new Color(0.45f, 0.45f, 0.45f, 0.9f)),
                1);

            Rect iconRect = buttonRect.ContractedBy(6f);
            Widgets.DrawTextureFitted(iconRect, SkyrimIslandsTextureCache.WorldRoutePlannerTex, 1f, SkyrimIslandsTextureCache.SkyIslandControlButtonMat, 1f);

            TooltipHandler.TipRegion(buttonRect, "打开空岛总控界面");
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(buttonRect))
            {
                SkyIslandControlWindowUtility.ToggleControlWindow();
                Event.current.Use();
            }
        }
    }
}
