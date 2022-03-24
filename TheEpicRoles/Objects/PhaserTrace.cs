using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TheEpicRoles;

namespace TheEpicRoles.Objects {
    class PhaserTrace {
        public static List<PhaserTrace> traces = new List<PhaserTrace>();

        private GameObject trace;
        private float timeRemaining;

        private static Sprite TraceSprite;
        public static Sprite getTraceSprite() {
            if (TraceSprite) return TraceSprite;
            TraceSprite = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.PhaserTraceM.png", 225f);
            return TraceSprite;
        }

        public PhaserTrace(Vector2 p, float duration = 1f) {
            trace = new GameObject("PhaserTrace");
            Vector3 position = new Vector3(p.x, p.y, PlayerControl.LocalPlayer.transform.localPosition.z + 0.001f); // just behind player
            trace.transform.position = position;
            trace.transform.localPosition = position;

            var traceRenderer = trace.AddComponent<SpriteRenderer>();
            traceRenderer.sprite = getTraceSprite();

            timeRemaining = duration;

            // display the Phasers color in the trace
            HudManager.Instance.StartCoroutine(Effects.Lerp(CustomOptionHolder.phaserTraceColorTime.getFloat(), new Action<float>((p) => {
                Color c = Palette.PlayerColors[(int)Phaser.phaser.Data.DefaultOutfit.ColorId];
                if (Camouflager.camouflageTimer > 0) {
                    c = Palette.PlayerColors[6];
                }

                Color g = new Color(0, 0, 0);  // Usual display color. could also be Palette.PlayerColors[6] for default grey like camo
                // if this stays black (0,0,0), it can ofc be removed.
                float p2 = p * p * p; // slower first, then quicker! https://youtu.be/sIlNIVXpIns

                Color combinedColor = Mathf.Clamp01(p2) * g + Mathf.Clamp01(1 - p2) * c;

                if (traceRenderer) traceRenderer.color = combinedColor;
            })));
            HudManager.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>((p) => {
                if (traceRenderer) traceRenderer.color = new Color(traceRenderer.color.r, traceRenderer.color.g, traceRenderer.color.b, Mathf.Clamp01(1-p));
            })));


            trace.SetActive(true);
            traces.Add(this);
        }

        public static void clearTraces() {
            traces = new List<PhaserTrace>();
        }

        public static void UpdateAll() {
            foreach (PhaserTrace traceCurrent in traces) {
                traceCurrent.timeRemaining -= Time.fixedDeltaTime;
                if (traceCurrent.timeRemaining < 0) {
                    traceCurrent.trace.SetActive(false);
                    UnityEngine.Object.Destroy(traceCurrent.trace);
                }
            }
        }
    }
}