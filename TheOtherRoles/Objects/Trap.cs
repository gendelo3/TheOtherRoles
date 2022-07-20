using Hazel;
using System;
using System.Collections.Generic;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Objects {
    class Trap {
        public static List<Trap> traps = new List<Trap>();

        private static int instanceCounter = 0;
        private int instanceId = 0;
        public GameObject trap;
        private bool revealed = false;
        private bool triggerable = false;

        private static Sprite trapSprite;
        public static Sprite getTrapSprite() {
            if (trapSprite) return trapSprite;
            trapSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Garlic.png", 300f);
            return trapSprite;
        }

        public Trap(Vector2 p) {
            trap = new GameObject("Trap") { layer = 11 };
            trap.AddSubmergedComponent(SubmergedCompatibility.Classes.ElevatorMover);
            Vector3 position = new Vector3(p.x, p.y, p.y / 1000 + 0.001f); // just behind player
            trap.transform.position = position;

            var trapRenderer = trap.AddComponent<SpriteRenderer>();
            trapRenderer.sprite = getTrapSprite();
            trap.SetActive(false);
            if (CachedPlayer.LocalPlayer.PlayerId == Trapper.trapper.PlayerId) trap.SetActive(true);
            this.instanceId = ++instanceCounter;
            TheOtherRolesPlugin.Logger.LogError("instanceId " + instanceId);
            TheOtherRolesPlugin.Logger.LogError("instanceCounter " + instanceCounter);
            traps.Add(this);
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(5, new Action<float>((x) => {
                if (x == 1f) {
                    this.triggerable = true;
                }
            })));
        }

        public static void clearTraps() {
            foreach (Trap t in traps) UnityEngine.Object.Destroy(t.trap);
            traps = new List<Trap>();
        }

        public static void clearRevealedTraps() {
            var trapsToClear = traps.FindAll(x => x.revealed);

            foreach (Trap t in trapsToClear) {
                traps.Remove(t);
                UnityEngine.Object.Destroy(t.trap);
            }
        }

        public static void triggerTrap(byte playerId, byte trapId) {
            Trap t = traps.FindLast(x => x.instanceId == (int)trapId);
            PlayerControl player = Helpers.playerById(playerId);
            t.trap.SetActive(true);
            t.revealed = true;
            SoundEffectsManager.play("mediumAsk");
            player.moveable = false;
            player.NetTransform.Halt();
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(3, new Action<float>((p) => { 
                if (p == 1f) {
                    player.moveable = true;
                }
            })));
            Trapper.trappedRoles.Add(RoleInfo.GetRolesString(player, false));
            if (CachedPlayer.LocalPlayer.PlayerId == Trapper.trapper.PlayerId) {
                Helpers.showFlash(Trapper.color);
            }

        }

        public static void Update() {
            if (Trapper.trapper == null) return;
            CachedPlayer player = CachedPlayer.LocalPlayer;
            Vent vent = MapUtilities.CachedShipStatus.AllVents[0];
            float closestDistance = float.MaxValue;

            if (vent == null || player == null) return;
            Trap target = null;
            foreach (Trap trap in traps) {
                if (trap.revealed || !trap.triggerable) continue;
                float distance = Vector2.Distance(trap.trap.transform.position, player.PlayerControl.GetTruePosition());
                if (distance <= vent.UsableDistance && distance < closestDistance) {
                    closestDistance = distance;
                    target = trap;
                }
            }
            if (target != null && player.PlayerId != Trapper.trapper.PlayerId) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.TriggerTrap, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.Write(target.instanceId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.triggerTrap(player.PlayerId,(byte)target.instanceId);
            }


            if (!player.Data.IsDead || player.PlayerId == Trapper.trapper.PlayerId) return;
            foreach (Trap trap in traps) {
                if (!trap.trap.active) trap.trap.SetActive(true);
            }
        }
    }
}