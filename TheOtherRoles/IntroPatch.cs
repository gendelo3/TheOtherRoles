using HarmonyLib;
using System;
using static TheOtherRoles.TheOtherRoles;
using UnityEngine;
using System.Collections.Generic;

namespace TheOtherRoles
{
    [HarmonyPatch(typeof(IntroCutscene._CoBegin_d__11), nameof(IntroCutscene._CoBegin_d__11.MoveNext))]
    class IntroPatch
    {
        // Intro special teams
        static void Prefix(IntroCutscene._CoBegin_d__11 __instance)
        {
            if (PlayerControl.LocalPlayer == Jester.jester)
            {
                var jesterTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                jesterTeam.Add(PlayerControl.LocalPlayer);
                __instance.yourTeam = jesterTeam;
            } else if (PlayerControl.LocalPlayer == Jackal.jackal) {
                var jackalTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                jackalTeam.Add(PlayerControl.LocalPlayer);
                __instance.yourTeam = jackalTeam;
            }
        }

        // Intro display role
        static void Postfix(IntroCutscene._CoBegin_d__11 __instance)
        {
            List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer);
            if (infos.Count == 0) return;
            RoleInfo roleInfo = infos[0];

            if (PlayerControl.LocalPlayer == Lovers.lover1 || PlayerControl.LocalPlayer == Lovers.lover2)
            {
                PlayerControl otherLover = PlayerControl.LocalPlayer == Lovers.lover1 ? Lovers.lover2 : Lovers.lover1;
                __instance.__this.Title.Text = PlayerControl.LocalPlayer.Data.IsImpostor ? "[FF1919FF]Imp[FC03BEFF]Lover" : "Lover";
                __instance.__this.Title.Color = PlayerControl.LocalPlayer.Data.IsImpostor ? Color.white : Lovers.color;
                __instance.__this.ImpostorText.Text = "You are in [FC03BEFF]Love [FFFFFFFF] with [FC03BEFF]" + (otherLover?.Data?.PlayerName ?? "");
                __instance.__this.ImpostorText.gameObject.SetActive(true);
                __instance.__this.BackgroundBar.material.color = Lovers.color;
            }
            else if (PlayerControl.LocalPlayer == BountyHunter.bountyHunter)
            {
                __instance.__this.Title.Text = "Bounty Hunter";
                __instance.__this.Title.Color = BountyHunter.color;
                __instance.__this.ImpostorText.Text = "Hunt [ED653BFF]" + BountyHunter.target?.Data?.PlayerName + "[FFFFFFFF] down";
                __instance.__this.BackgroundBar.material.color = BountyHunter.color;
            }
            else if (roleInfo.name == "Crewmate" || roleInfo.name == "Impostor") {}
            else {
                __instance.__this.Title.Text = roleInfo.name;
                __instance.__this.Title.Color = roleInfo.color;
                __instance.__this.ImpostorText.gameObject.SetActive(true);
                __instance.__this.ImpostorText.Text = roleInfo.introDescription;
                __instance.__this.BackgroundBar.material.color = roleInfo.color;
            }
        }
    }
}
