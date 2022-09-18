using System.Collections.Generic;
using TheOtherRoles.Objects;
using TheOtherRoles.Players;
using UnityEngine;

namespace TheOtherRoles.CustomGameModes {
    public static class HideNSeek { // HideNSeek Gamemode
        public static bool isHideNSeekGM = false;
        public static TMPro.TMP_Text timerText = null;
        public static Vent polusVent = null;
        public static bool isWaitingTimer = false;

        public static float timer = 300f;
        public static float hunterVision = 0.5f;
        public static float huntedVision = 2f;
        public static bool taskWinPossible = false;
        public static float taskPunish = 10f;
        public static int impNumber = 2;
        public static bool canSabotage = false;
        public static float killCooldown = 10f;
        public static float hunterWaitingTime = 15f;
        public static bool isHunter() {
            return isHideNSeekGM && CachedPlayer.LocalPlayer != null && CachedPlayer.LocalPlayer.Data.Role.IsImpostor;
        }

        public static List<CachedPlayer> getHunters() {
            List<CachedPlayer> hunters = new List<CachedPlayer>(CachedPlayer.AllPlayers);
            hunters.RemoveAll(x => !x.Data.Role.IsImpostor);
            return hunters;
        }

        public static bool isHunted() {
            return isHideNSeekGM && CachedPlayer.LocalPlayer != null && !CachedPlayer.LocalPlayer.Data.Role.IsImpostor;
        }

        public static void clearAndReload() {
            isHideNSeekGM = MapOptions.gameMode == CustomGamemodes.HideNSeek;
            if (timerText != null) UnityEngine.Object.Destroy(timerText);
            timerText = null;
            if (polusVent != null) UnityEngine.Object.Destroy(polusVent);
            polusVent = null;
            isWaitingTimer = false;

            timer = CustomOptionHolder.hideNSeekTimer.getFloat() * 60;
            hunterVision = CustomOptionHolder.hideNSeekHunterVision.getFloat();
            huntedVision = CustomOptionHolder.hideNSeekHuntedVision.getFloat();
            taskWinPossible = CustomOptionHolder.hideNSeekTaskWin.getBool();
            taskPunish = CustomOptionHolder.hideNSeekTaskPunish.getFloat();
            impNumber = Mathf.RoundToInt(CustomOptionHolder.hideNSeekHunterCount.getFloat());
            canSabotage = CustomOptionHolder.hideNSeekCanSabotage.getBool();
            killCooldown = CustomOptionHolder.hideNSeekKillCooldown.getFloat();
            hunterWaitingTime = CustomOptionHolder.hideNSeekHunterWaiting.getFloat();

            Hunter.clearAndReload();
            Hunted.clearAndReload();

            PlayerControl.GameOptions.NumImpostors = impNumber;
            PlayerControl.GameOptions.ImpostorLightMod = hunterVision;
            PlayerControl.GameOptions.CrewLightMod = huntedVision;
            PlayerControl.GameOptions.KillCooldown = killCooldown;
        }
    }

    public static class Hunter {
        public static List<Arrow> localArrows = new List<Arrow>();
        public static List<byte> lightActive = new List<byte>();
        public static bool arrowActive = false;

        public static float lightCooldown = 30f;
        public static float lightDuration = 5f;
        public static float lightVision = 2f;
        public static float lightPunish = 5f;
        public static float AdminCooldown = 30f;
        public static float AdminDuration = 5f;
        public static float AdminPunish = 5f;
        public static float ArrowCooldown = 30f;
        public static float ArrowDuration = 5f;
        public static float ArrowPunish = 5f;

        public static bool isLightActive (byte playerId) {
            return lightActive.Contains(playerId);
        }

        public static void clearAndReload() {
            if (localArrows != null) {
                foreach (Arrow arrow in localArrows)
                    if (arrow?.arrow != null)
                        UnityEngine.Object.Destroy(arrow.arrow);
            }
            localArrows = new List<Arrow>();
            lightActive = new List<byte>();
            arrowActive = false;

            lightCooldown = CustomOptionHolder.hunterLightCooldown.getFloat();
            lightDuration = CustomOptionHolder.hunterLightDuration.getFloat();
            lightVision = CustomOptionHolder.hunterLightVision.getFloat();
            lightPunish = CustomOptionHolder.hunterLightPunish.getFloat();
            AdminCooldown = CustomOptionHolder.hunterAdminCooldown.getFloat();
            AdminDuration = CustomOptionHolder.hunterAdminDuration.getFloat();
            AdminPunish = CustomOptionHolder.hunterAdminPunish.getFloat();
            ArrowCooldown = CustomOptionHolder.hunterArrowCooldown.getFloat();
            ArrowDuration = CustomOptionHolder.hunterArrowDuration.getFloat();
            ArrowPunish = CustomOptionHolder.hunterArrowPunish.getFloat();
        }
    }

    public static class Hunted {
        public static List<byte> timeshieldActive = new List<byte>();
        public static int shieldCount = 3;

        public static float shieldCooldown = 30f;
        public static float shieldDuration = 5f;
        public static float shieldRewindTime = 3f;
        public static bool taskPunish = false;
        public static void clearAndReload() {
            timeshieldActive = new List<byte>();
            taskPunish = false;

            shieldCount = Mathf.RoundToInt(CustomOptionHolder.huntedShieldNumber.getFloat());
            shieldCooldown = CustomOptionHolder.huntedShieldCooldown.getFloat();
            shieldDuration = CustomOptionHolder.huntedShieldDuration.getFloat();
            shieldRewindTime = CustomOptionHolder.huntedShieldRewindTime.getFloat();
        }
    }
}