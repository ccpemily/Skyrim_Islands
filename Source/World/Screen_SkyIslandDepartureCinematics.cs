using System;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class Screen_SkyIslandDepartureCinematics : Window
    {
        private const float HoldSecs = 3.5f;

        private readonly Action nextStepAction;
        private float screenStartTime;

        public Screen_SkyIslandDepartureCinematics(Map sourceMap, IntVec3 departureCell, Action nextStepAction)
        {
            this.nextStepAction = nextStepAction;
            forcePause = false;
            absorbInputAroundWindow = true;
            doWindowBackground = false;
            doCloseButton = false;
            doCloseX = false;
            onlyOneOfTypeAllowed = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

        protected override float Margin => 0f;

        public override void PreOpen()
        {
            base.PreOpen();
            screenStartTime = Time.realtimeSinceStartup;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Time.realtimeSinceStartup > screenStartTime + HoldSecs)
            {
                Close(false);
                return;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, 24f, inRect.width, 32f), "穿梭机正在升空……");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void PostClose()
        {
            base.PostClose();
            nextStepAction();
        }
    }
}
