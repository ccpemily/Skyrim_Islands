using System;
using RimWorld;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.MainTabs
{
    public class Dialog_SkyIslandAltitudeSelector : Window
    {
        private readonly float defaultAltitude;
        private float selectedAltitude;
        private readonly Action<float> onConfirm;

        public override Vector2 InitialSize => new Vector2(420f, 220f);

        public Dialog_SkyIslandAltitudeSelector(float defaultAltitude, Action<float> onConfirm)
        {
            this.defaultAltitude = defaultAltitude;
            this.selectedAltitude = defaultAltitude;
            this.onConfirm = onConfirm;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, rect.width, 32f), "设置路径点高度");
            Text.Font = GameFont.Small;

            float y = 38f;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(0f, y, rect.width, 22f), $"范围: {SkyIslandAltitude.MinAltitude:F0} ~ {SkyIslandAltitude.MaxAltitude:F0}    当前: {selectedAltitude:F1}");
            GUI.color = Color.white;
            y += 28f;

            Rect sliderRect = new Rect(10f, y, rect.width - 20f, 24f);
            selectedAltitude = Widgets.HorizontalSlider(sliderRect, selectedAltitude, SkyIslandAltitude.MinAltitude, SkyIslandAltitude.MaxAltitude, false, null, null, null, 0.1f);
            y += 36f;

            float buttonWidth = 100f;
            Rect confirmRect = new Rect(rect.center.x - buttonWidth - 10f, y, buttonWidth, 32f);
            Rect cancelRect = new Rect(rect.center.x + 10f, y, buttonWidth, 32f);

            if (Widgets.ButtonText(confirmRect, "确认"))
            {
                onConfirm?.Invoke(selectedAltitude);
                Close();
            }
            if (Widgets.ButtonText(cancelRect, "取消"))
            {
                Close();
            }
        }
    }
}
