using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.Button;

namespace TheOtherRoles.Patches {
    [HarmonyPatch(typeof(GameModeMenu))]
    class CreateOptionsPickerPatch {
        private static List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        [HarmonyPatch(typeof(GameModeMenu), "Awake")]
        [HarmonyPrefix]
        public static void Prefix(GameModeMenu __instance) {
            renderers = new List<SpriteRenderer>();

            // space for max 5 buttons
            addGamemodeButton(__instance, "Guesser", "TheOtherRoles.Resources.TabIconGuesserMode.png", CustomGamemodes.Guesser);
            addGamemodeButton(__instance, "Hide 'N Seek", "TheOtherRoles.Resources.TabIconHideNSeekMode.png", CustomGamemodes.HideNSeek);

            switch (MapOptions.gameMode) {
                case CustomGamemodes.Classic: break;
                case CustomGamemodes.Guesser: renderers.FindLast(x => x.name == "Guesser").color = Color.white; break;
                case CustomGamemodes.HideNSeek: renderers.FindLast(x => x.name == "Hide 'N Seek").color = Color.white; break;
            }
        }

        private static void addGamemodeButton(GameModeMenu __instance, string name, string spritePath, CustomGamemodes gamemode) {
            Vector3 offset = __instance.ButtonPool.transform.position;
            Vector3 dist = new Vector3(0f, -0.6f, 0f);
            Vector3 target = offset + dist * (renderers.Count + 1);

            GameObject gameObject = new("gm" + name);
            gameObject.transform.position = target;
            gameObject.transform.SetParent(__instance.ButtonPool.transform, worldPositionStays: true);

            SpriteRenderer buttonSprite = gameObject.AddComponent<SpriteRenderer>();
            buttonSprite.sprite = Helpers.loadSpriteFromResources(spritePath, 150f);
            PassiveButton passiveButton = gameObject.AddComponent<PassiveButton>();
            //buttonSprite.color *= 0;

            renderers.Add(buttonSprite);

            passiveButton.OnClick = new ButtonClickedEvent();
            passiveButton.OnClick.AddListener((System.Action)(() => setListener(buttonSprite, gamemode)));
            gameObject.SetActive(true);
        }


        private static void setListener(SpriteRenderer renderer, CustomGamemodes gameMode) {
            MapOptions.gameMode = gameMode;
            foreach (SpriteRenderer r in renderers) r.color *= 0;
            renderer.color = Color.white;
        }
    }
}
