using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Quests.Initial
{
    public class Screen_SkyIslandMigrationCinematics : Window
    {
        private const float FadeSecs = 1.5f;
        private const float MessageDisplaySecs = 4f;
        private readonly Action nextStepAction;
        private bool fadeCleared;
        private float screenStartTime;

        public Screen_SkyIslandMigrationCinematics(Action nextStepAction)
        {
            this.nextStepAction = nextStepAction;
            doWindowBackground = false;
            doCloseButton = false;
            doCloseX = false;
            forcePause = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

        protected override float Margin => 0f;

        public override void PreOpen()
        {
            base.PreOpen();
            Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(7f, 1f);
            ScreenFader.StartFade(Color.black, FadeSecs);
            screenStartTime = Time.realtimeSinceStartup;
        }

        public override void PostClose()
        {
            base.PostClose();
            ScreenFader.SetColor(Color.black);
            nextStepAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Time.realtimeSinceStartup < screenStartTime + FadeSecs)
            {
                return;
            }

            if (!fadeCleared)
            {
                fadeCleared = true;
                ScreenFader.SetColor(Color.clear);
            }

            if (Time.realtimeSinceStartup > screenStartTime + FadeSecs + MessageDisplaySecs)
            {
                Close(false);
                return;
            }

            GUI.DrawTexture(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), BaseContent.BlackTex);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Rect textRect = new Rect(inRect.x + 80f, inRect.center.y - 60f, inRect.width - 160f, 120f);
            Widgets.Label(textRect, "穿梭机穿过云海，锚定了一片漂浮的岛屿。\n新的家园正在云层上方展开。");
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
