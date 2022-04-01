using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TheEpicRoles;

namespace TheEpicRoles.Modules {

        
    }
    internal class CustomProgressBar {

    static List<CustomProgressBar> progressBars = new List<CustomProgressBar>();

    // Create a property to handle the slider's value
    private float currentValue = 0f;
    public float CurrentValue {
        get {
            return currentValue;
        }
        set {
            currentValue = value;
            if (fgBar != null) {
                fgRenderer.transform.localScale = new Vector2(currentValue * 50f, 5f);
                fgRenderer.transform.localPosition = new Vector3(currentValue * 1f, fgRenderer.transform.localPosition.y, fgRenderer.transform.localPosition.z);
                SetProgessText();
            }
            if (currentValue >= 1f && fgBar != null) {
                pBar.SetActive(false);
                //fgBar.SetActive(false);
                //bgBar.SetActive(false);
                
            }
        }
    }

    CustomProgressBar parent;
    GameObject pBar;
    GameObject fgBar;
    GameObject bgBar;
    SpriteRenderer fgRenderer;
    SpriteRenderer bgRenderer;
    string title;
    
    TMPro.TextMeshPro titleText;
    TMPro.TextMeshPro progressText;

    public static Sprite whitePixel;

    private static void CreateSprite() {
        Texture2D pxl = new Texture2D(1, 1);
        pxl.SetPixel(0, 0, Color.white);
        pxl.Apply();
        whitePixel = Sprite.Create(pxl, new Rect(0, 0, pxl.width, pxl.height), new Vector2(0.5f, 0.5f), 25);
    }


    public CustomProgressBar(string title) {
        this.title = title;
        if (progressBars.Count > 0) this.parent = progressBars[progressBars.Count - 1];
        CurrentValue = 0f;
        progressBars.Add(this);
    }

    public void SetProgessText() {
        progressText?.SetText((currentValue * 100).ToString("0.0") + "%");
    }

    public static void enableAll(Transform initialParent) {
        if (whitePixel == null) CreateSprite();        

        for(int i = 0; i< progressBars.Count; i++) {
            CustomProgressBar cpb = progressBars[i];

            TheEpicRolesPlugin.Logger.LogMessage("creating pBar");
            cpb.pBar = new GameObject("pBarAll");
            cpb.pBar.transform.SetParent(initialParent, false);
            cpb.pBar.transform.localPosition = new Vector2(6, 0);
            cpb.fgBar = new GameObject("pBarFG");
            cpb.bgBar = new GameObject("pBarBG");


            VersionShower template = GameObject.FindObjectOfType<VersionShower>();
            if (template != null) {
                TheEpicRolesPlugin.Logger.LogMessage("found template");
                cpb.titleText = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(template.text);
                cpb.titleText.transform.SetParent(cpb.pBar.transform, false);
                cpb.titleText.alignment = TMPro.TextAlignmentOptions.Center;
                cpb.titleText.transform.localPosition = new Vector3(-1f, -0.5f * i, -5.5f);
                cpb.titleText.SetText(cpb.title);

                cpb.progressText = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(template.text);
                cpb.progressText.transform.SetParent(cpb.pBar.transform, false);
                cpb.progressText.alignment = TMPro.TextAlignmentOptions.Center;
                cpb.progressText.transform.localPosition = new Vector3(1f, -0.5f * i, -5.5f);
                cpb.progressText.SetText((cpb.currentValue * 100).ToString("0.00"));
            }

            cpb.fgBar.transform.localPosition = new Vector3(0f, -0.5f * i, -5f);
            cpb.fgBar.transform.SetParent(cpb.pBar.transform, false);
            cpb.bgBar.transform.localPosition = new Vector3(1f, -0.5f * i, -2.5f);
            cpb.bgBar.transform.SetParent(cpb.pBar.transform, false);
            cpb.fgRenderer = cpb.fgBar.AddComponent<SpriteRenderer>();
            cpb.bgRenderer = cpb.bgBar.AddComponent<SpriteRenderer>();
            
            cpb.fgRenderer.sprite = whitePixel;
            cpb.bgRenderer.sprite = whitePixel;
            cpb.bgRenderer.color = Color.gray;

            cpb.fgBar.SetActive(true);
            cpb.bgBar.SetActive(true);

            cpb.bgRenderer.transform.localScale = new Vector2(50f, 5f);
        }
    }

}
