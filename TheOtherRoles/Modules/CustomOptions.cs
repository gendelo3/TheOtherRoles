using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Linq;
using HarmonyLib;
using Hazel;
using System.Reflection;
using System.Text;
using static TheOtherRoles.TheOtherRoles;
using UnhollowerBaseLib;

namespace TheOtherRoles {
    public class CustomOption {
        public static List<CustomOption> options = new List<CustomOption>();
        public static int preset = 0;

        public int id;
        public string name;
        public System.Object[] selections;

        public int defaultSelection;
        public ConfigEntry<int> entry;
        public int selection;
        public OptionBehaviour optionBehaviour;
        public CustomOption parent;
        public bool isHeader;

        // Option creation

        public CustomOption(int id, string name,  System.Object[] selections, System.Object defaultValue, CustomOption parent, bool isHeader) {
            this.id = id;
            this.name = parent == null ? name : "- " + name;
            this.selections = selections;
            int index = Array.IndexOf(selections, defaultValue);
            this.defaultSelection = index >= 0 ? index : 0;
            this.parent = parent;
            this.isHeader = isHeader;
            selection = 0;
            if (id != 0) {
                entry = TheOtherRolesPlugin.Instance.Config.Bind($"Preset{preset}", id.ToString(), defaultSelection);
                selection = Mathf.Clamp(entry.Value, 0, selections.Length - 1);
            }
            options.Add(this);
        }

        public static CustomOption Create(int id, string name, string[] selections, CustomOption parent = null, bool isHeader = false) {
            return new CustomOption(id, name, selections, "", parent, isHeader);
        }

        public static CustomOption Create(int id, string name, float defaultValue, float min, float max, float step, CustomOption parent = null, bool isHeader = false) {
            List<float> selections = new List<float>();
            for (float s = min; s <= max; s += step)
                selections.Add(s);
            return new CustomOption(id, name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader);
        }

        public static CustomOption Create(int id, string name, bool defaultValue, CustomOption parent = null, bool isHeader = false) {
            return new CustomOption(id, name, new string[]{"Off", "On"}, defaultValue ? "On" : "Off", parent, isHeader);
        }

        // Static behaviour

        public static void switchPreset(int newPreset) {
            CustomOption.preset = newPreset;
            foreach (CustomOption option in CustomOption.options) {
                if (option.id == 0) continue;

                option.entry = TheOtherRolesPlugin.Instance.Config.Bind($"Preset{preset}", option.id.ToString(), option.defaultSelection);
                option.selection = Mathf.Clamp(option.entry.Value, 0, option.selections.Length - 1);
                if (option.optionBehaviour != null && option.optionBehaviour is StringOption stringOption) {
                    stringOption.oldValue = stringOption.Value = option.selection;
                    stringOption.ValueText.text = option.selections[option.selection].ToString();
                }
            }
            VanillaOption.switchPreset(newPreset);
        }

        public static void ShareOptionSelections() {
            if (PlayerControl.AllPlayerControls.Count <= 1 || AmongUsClient.Instance?.AmHost == false && PlayerControl.LocalPlayer == null) return;
            
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShareOptions, Hazel.SendOption.Reliable);
            messageWriter.WritePacked((uint)CustomOption.options.Count);
            foreach (CustomOption option in CustomOption.options) {
                messageWriter.WritePacked((uint)option.id);
                messageWriter.WritePacked((uint)Convert.ToUInt32(option.selection));
            }
            messageWriter.EndMessage();
        }

        // Getter

        public int getSelection() {
            return selection;
        }

        public bool getBool() {
            return selection > 0;
        }

        public float getFloat() {
            return (float)selections[selection];
        }

        // Option changes

        public void updateSelection(int newSelection) {
            selection = Mathf.Clamp((newSelection + selections.Length) % selections.Length, 0, selections.Length - 1);
            if (optionBehaviour != null && optionBehaviour is StringOption stringOption) {
                stringOption.oldValue = stringOption.Value = selection;
                stringOption.ValueText.text = selections[selection].ToString();

                if (AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer) {
                    if (id == 0) switchPreset(selection); // Switch presets
                    else if (entry != null) entry.Value = selection; // Save selection to config

                    ShareOptionSelections();// Share all selections
                }
           } else if (id == 0 && AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer) {  // Share the preset switch for random maps, even if the menu isnt open!
                switchPreset(selection);
                ShareOptionSelections();// Share all selections
           }
        }
    }


    public class VanillaOption {
        public static List<VanillaOption> options = new List<VanillaOption>();
        public static int preset = 0;

        public static int curId = 900;

        public int id;
        public string name;
        public System.Object[] selections;

        public int defaultSelection;
        public ConfigEntry<int> entry;
        public int selection;
        public OptionBehaviour optionBehaviour;
        public OptionType optionType;
        StringOption stringOption;
        KeyValueOption keyValueOption;
        NumberOption numberOption;
        ToggleOption toggleOption;

        public enum OptionType {
            NumberOption,
            StringOption,
            ToggleOption,
            KeyValueOption
        }

        public VanillaOption(int id, string name, System.Object[] selections, System.Object defaultValue, OptionType optionType, OptionBehaviour behaviour) {
            this.id = id;
            this.name = name;
            this.selections = selections;
            int index = Array.IndexOf(selections, defaultValue);
            this.defaultSelection = index >= 0 ? index : 0;
            this.optionType = optionType;
            selection = 0;
            if (id != 0) {
                entry = TheOtherRolesPlugin.Instance.Config.Bind($"Preset{preset}", id.ToString(), defaultSelection, $"Vanilla Option: {name}");
            }
            optionBehaviour = behaviour;

            switch (optionType) {
                case OptionType.StringOption:
                    stringOption = behaviour.Cast<StringOption>();
                    break;
                case OptionType.NumberOption:
                    numberOption = behaviour.Cast<NumberOption>();
                    break;
                case OptionType.KeyValueOption:
                    keyValueOption = behaviour.Cast<KeyValueOption>();
                    break;
                case OptionType.ToggleOption:
                    toggleOption = behaviour.Cast<ToggleOption>();
                    break;
            }
            options.Add(this);
            curId++;
        }

        public static void switchPreset(int newPreset) {
            VanillaOption.preset = newPreset;
            foreach (VanillaOption option in options) {
                option.entry = TheOtherRolesPlugin.Instance.Config.Bind($"Preset{preset}", option.id.ToString(), option.defaultSelection);
                var newSelection = Mathf.Clamp(option.entry.Value, 0, option.selections.Length - 1);
                option.updateSelection(newSelection);
            }
        }

        public void updateSelection(int newSelection) {
            selection = Mathf.Clamp((newSelection + selections.Length) % selections.Length, 0, selections.Length - 1);
            if (optionBehaviour == null) return;
            if (AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer) {
                if (entry != null) entry.Value = selection; // Save selection to config
            }

            switch (this.optionType) {
                case OptionType.StringOption:
                    stringOption.oldValue = stringOption.Value = selection;
                    stringOption.ValueText.text = DestroyableSingleton<TranslationController>.Instance.GetString((StringNames)selections[selection], new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                    stringOption.OnValueChanged.Invoke(stringOption);
                    break;
                case OptionType.NumberOption:
                    numberOption.Value = (float)selections[selection];
                    numberOption.OnValueChanged.Invoke(numberOption);
                    break;
                case OptionType.KeyValueOption:
                    var kvp = (Il2CppSystem.Collections.Generic.KeyValuePair<string, int>)selections[selection];
                    keyValueOption.ValueText.text = kvp.Key;
                    keyValueOption.oldValue = kvp.Value - 1;
                    keyValueOption.Selected = kvp.Value;
                    keyValueOption.OnValueChanged.Invoke(keyValueOption);
                    break;
                case OptionType.ToggleOption:
                    toggleOption.oldValue = selection==0;
                    toggleOption.CheckMark.enabled = selection == 1;
                    toggleOption.OnValueChanged.Invoke(toggleOption);
                    break;                
            }
        }

        public static void forceLoad() {
            // Update vanilla settings for the game by opening and closing the settings....
            PlayerControl.LocalPlayer.transform.position = new Vector3(-1.3f, 2.4f, 0.0f);
            HudManager.Instance.StartCoroutine(Effects.Lerp(0.25f, new Action<float>((p) => {
                if (p == 1.0f) HudManager.Instance.UseButton.DoClick();
            })));
            bool closed = false;
            HudManager.Instance.StartCoroutine(Effects.Lerp(5f, new Action<float>((p) => {
                if (GameSettingMenu.Instance != null && !closed) {
                    GameSettingMenu.Instance.Close();
                    closed = true;
                }
            })));
        }

        public static void Load(OptionBehaviour[] optionBehaviours) {
            options = new List<VanillaOption>();
            curId = 900;
            foreach (var behaviour in optionBehaviours) {
                string name = "";
                System.Object[] selections = null;
                System.Object defaultValue = null;
                OptionType currentOptionType = (OptionType)0;

                StringOption stringOption;
                KeyValueOption keyValueOption;
                NumberOption numberOption;
                ToggleOption toggleOption;

                try {
                    stringOption = behaviour.Cast<StringOption>();
                    currentOptionType = OptionType.StringOption;
                    name = stringOption.TitleText.text;
                    selections = stringOption.Values.Cast<object>().ToArray();
                    defaultValue = stringOption.Value;
                } catch {
                    try {
                        keyValueOption = behaviour.Cast<KeyValueOption>();
                        currentOptionType = OptionType.KeyValueOption;
                        name = keyValueOption.TitleText.text;
                        selections = keyValueOption.Values.ToArray(); // TODO
                        defaultValue = keyValueOption.oldValue;
                    } catch {
                        try {
                            numberOption = behaviour.Cast<NumberOption>();
                            name = numberOption.TitleText.text;
                            currentOptionType = OptionType.NumberOption;

                            float start = numberOption.ValidRange.min;
                            float end = numberOption.ValidRange.max;
                            float increment = numberOption.Increment;
                            var sel = new List<float>();
                            while (start <= end) {
                                sel.Add(start);
                                start += increment;
                            }
                            selections = sel.Cast<object>().ToArray();
                            defaultValue = numberOption.Value;

                        } catch {
                            try {
                                toggleOption = behaviour.Cast<ToggleOption>();
                                currentOptionType = OptionType.ToggleOption;
                                name = toggleOption.TitleText.text;
                                selections = (new bool[] { false, true }).Cast<object>().ToArray();
                                defaultValue = toggleOption.GetBool();
                            }
                            catch {
                            }
                        }
                    }

                }

                // create option object to sync settings with.
                new VanillaOption(curId, name, selections, defaultValue, currentOptionType, behaviour);
            }

            switchPreset(preset);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
    class GameOptionsMenuStartPatch {
        public static void Postfix(GameOptionsMenu __instance) {
            if (GameObject.Find("TORSettings") != null) { // Settings setup has already been performed, fixing the title of the tab and returning
                GameObject.Find("TORSettings").transform.FindChild("GameGroup").FindChild("Text").GetComponent<TMPro.TextMeshPro>().SetText("The Other Roles Settings");
                return;
            }

            // Setup TOR tab
            var template = UnityEngine.Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;
            var gameSettings = GameObject.Find("Game Settings");
            var gameSettingMenu = UnityEngine.Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            var torSettings = UnityEngine.Object.Instantiate(gameSettings, gameSettings.transform.parent);
            var torMenu = torSettings.transform.FindChild("GameGroup").FindChild("SliderInner").GetComponent<GameOptionsMenu>();
            torSettings.name = "TORSettings";

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");

            var torTab = UnityEngine.Object.Instantiate(roleTab, roleTab.transform.parent);
            var torTabHighlight = torTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
            torTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TabIcon.png", 100f);

            gameTab.transform.position += Vector3.left * 0.5f;
            torTab.transform.position += Vector3.right * 0.5f;
            roleTab.transform.position += Vector3.left * 0.5f;

            var tabs = new GameObject[]{gameTab, roleTab, torTab};
            for (int i = 0; i < tabs.Length; i++) {
                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                int copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                    gameSettingMenu.RegularGameSettings.SetActive(false);
                    gameSettingMenu.RolesSettings.gameObject.SetActive(false);
                    torSettings.gameObject.SetActive(false);
                    gameSettingMenu.GameSettingsHightlight.enabled = false;
                    gameSettingMenu.RolesSettingsHightlight.enabled = false;
                    torTabHighlight.enabled = false;
                    if (copiedIndex == 0) {
                        if (CustomOption.options[0].optionBehaviour != null) CustomOption.options[0].optionBehaviour.transform.SetParent(__instance.transform, false);
                        gameSettingMenu.RegularGameSettings.SetActive(true);
                        gameSettingMenu.GameSettingsHightlight.enabled = true;
                    } else if (copiedIndex == 1) {
                        gameSettingMenu.RolesSettings.gameObject.SetActive(true);
                        gameSettingMenu.RolesSettingsHightlight.enabled = true;
                    } else if (copiedIndex == 2) {
                        if (CustomOption.options[0].optionBehaviour != null) CustomOption.options[0].optionBehaviour.transform.SetParent(torMenu.transform, false);
                        torSettings.gameObject.SetActive(true);
                        torTabHighlight.enabled = true;
                    }
                }));
            }

            foreach (OptionBehaviour option in torMenu.GetComponentsInChildren<OptionBehaviour>())
                UnityEngine.Object.Destroy(option.gameObject);
            List<OptionBehaviour> torOptions = new List<OptionBehaviour>();

            for (int i = 0; i < CustomOption.options.Count; i++) {
                CustomOption option = CustomOption.options[i];
                if (option.optionBehaviour == null) {
                    StringOption stringOption = UnityEngine.Object.Instantiate(template, torMenu.transform);
                    torOptions.Add(stringOption);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => {});
                    stringOption.TitleText.text = option.name;
                    stringOption.Value = stringOption.oldValue = option.selection;
                    stringOption.ValueText.text = option.selections[option.selection].ToString();

                    option.optionBehaviour = stringOption;
                }
                option.optionBehaviour.gameObject.SetActive(true);
            }

            torMenu.Children = torOptions.ToArray();
            torSettings.gameObject.SetActive(false);

            // Adapt task count for main options

            var commonTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumCommonTasks").TryCast<NumberOption>();
            if(commonTasksOption != null) commonTasksOption.ValidRange = new FloatRange(0f, 4f);

            var shortTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumShortTasks").TryCast<NumberOption>();
            if(shortTasksOption != null) shortTasksOption.ValidRange = new FloatRange(0f, 23f);

            var longTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumLongTasks").TryCast<NumberOption>();
            if(longTasksOption != null) longTasksOption.ValidRange = new FloatRange(0f, 15f);
            
            // Start Menu on TOR tab!
            var torButton = tabs[2].GetComponentInChildren<PassiveButton>();
            torButton.OnClick.Invoke();
            // Extra space in game settings, for preset switch button!
            gameSettingMenu.Scroller.ContentYBounds.max += 0.8f;
            foreach (var option in __instance.Children) {
                option.transform.localPosition += new Vector3(0, -0.6f, 0);
            } 
            VanillaOption.Load(__instance.Children);
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
    public class StringOptionEnablePatch {
        public static bool Prefix(StringOption __instance) {
            CustomOption option = CustomOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => {});
            __instance.TitleText.text = option.name;
            __instance.Value = __instance.oldValue = option.selection;
            __instance.ValueText.text = option.selections[option.selection].ToString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            CustomOption option = CustomOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) {
                VanillaOption vOption = VanillaOption.options.FirstOrDefault(opt => opt.optionBehaviour == __instance);
                if (vOption == null) return true;
                vOption.updateSelection(vOption.selection + 1);
                return false;
            }
            option.updateSelection(option.selection + 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            CustomOption option = CustomOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) {
                VanillaOption vOption = VanillaOption.options.FirstOrDefault(opt => opt.optionBehaviour == __instance);
                if (vOption == null) return true;
                vOption.updateSelection(vOption.selection - 1);
                return false;
            }
            option.updateSelection(option.selection - 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
    public class NumberOptionIncreasePatch {
        public static bool Prefix(NumberOption __instance) {
            VanillaOption option = VanillaOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection + 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
    public class NumberOptionDecreasePatch {
        public static bool Prefix(NumberOption __instance) {
            VanillaOption option = VanillaOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection - 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.Decrease))]
    public class KeyValueOptionDecreasePatch {
        public static bool Prefix(KeyValueOption __instance) {
            VanillaOption option = VanillaOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection - 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.Increase))]
    public class KeyValueOptionIncreasePatch {
        public static bool Prefix(KeyValueOption __instance) {
            VanillaOption option = VanillaOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection + 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(ToggleOption), nameof(ToggleOption.Toggle))]
    public class ToggleOptionTogglePatch {
        public static bool Prefix(ToggleOption __instance) {
            VanillaOption option = VanillaOption.options.FirstOrDefault(option => option.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection + 1);
            return false;
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            CustomOption.ShareOptionSelections();
        }
    }


    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    class GameOptionsMenuUpdatePatch
    {
        private static float timer = 1f;
        public static void Postfix(GameOptionsMenu __instance) {
            if (__instance.Children.Length < 100) return; // TODO: Introduce a cleaner way to seperate the TOR settings from the game settings

            __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -0.5F + __instance.Children.Length * 0.55F +10F; 
            timer += Time.deltaTime;
            if (timer < 0.1f) return;
            timer = 0f;

            float offset = 2.75f;
            foreach (CustomOption option in CustomOption.options) {
                if (option?.optionBehaviour != null && option.optionBehaviour.gameObject != null) {
                    bool enabled = true;
                    var parent = option.parent;
                    while (parent != null && enabled) {
                        enabled = parent.selection != 0;
                        parent = parent.parent;
                    }
                    option.optionBehaviour.gameObject.SetActive(enabled);
                    if (enabled) {
                        offset -= option.isHeader ? 0.75f : 0.5f;
                        option.optionBehaviour.transform.localPosition = new Vector3(option.optionBehaviour.transform.localPosition.x, offset, option.optionBehaviour.transform.localPosition.z);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch {
        public static void Prefix(GameSettingMenu __instance) {
            __instance.HideForOnline = new Transform[]{};
        }

        public static void Postfix(GameSettingMenu __instance) {
            // Setup mapNameTransform
            var mapNameTransform = __instance.AllItems.FirstOrDefault(x => x.gameObject.activeSelf && x.name.Equals("MapName", StringComparison.OrdinalIgnoreCase));
            if (mapNameTransform == null) return;

            var options = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<string, int>>();
            for (int i = 0; i < Constants.MapNames.Length; i++) {
                var kvp = new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>();
                kvp.key = Constants.MapNames[i];
                kvp.value = i;
                options.Add(kvp);
            }
            mapNameTransform.GetComponent<KeyValueOption>().Values = options;
        }
    }

    [HarmonyPatch(typeof(Constants), nameof(Constants.ShouldFlipSkeld))]
    class ConstantsShouldFlipSkeldPatch {
        public static bool Prefix(ref bool __result) {
            if (PlayerControl.GameOptions == null) return true;
            __result = PlayerControl.GameOptions.MapId == 3;
            return false;
        }
    }

    [HarmonyPatch] 
    class GameOptionsDataPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() {
            return typeof(GameOptionsData).GetMethods().Where(x => x.ReturnType == typeof(string) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(int));
        }

        private static void Postfix(ref string __result)
        {
            StringBuilder sb = new StringBuilder(__result);
            foreach (CustomOption option in CustomOption.options) {
                if (option.parent == null) {
                    if (option == CustomOptionHolder.crewmateRolesCountMin) {
                        var optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), "Crewmate Roles");
                        var min = CustomOptionHolder.crewmateRolesCountMin.getSelection();
                        var max = CustomOptionHolder.crewmateRolesCountMax.getSelection();
                        if (min > max) min = max;
                        var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                        sb.AppendLine($"{optionName}: {optionValue}");
                    } else if (option == CustomOptionHolder.neutralRolesCountMin) {
                        var optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), "Neutral Roles");
                        var min = CustomOptionHolder.neutralRolesCountMin.getSelection();
                        var max = CustomOptionHolder.neutralRolesCountMax.getSelection();
                        if (min > max) min = max;
                        var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                        sb.AppendLine($"{optionName}: {optionValue}");
                    } else if (option == CustomOptionHolder.impostorRolesCountMin) {
                        var optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), "Impostor Roles");
                        var min = CustomOptionHolder.impostorRolesCountMin.getSelection();
                        var max = CustomOptionHolder.impostorRolesCountMax.getSelection();
                        if (min > max) min = max;
                        var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
                        sb.AppendLine($"{optionName}: {optionValue}");
                    } else if ((option == CustomOptionHolder.crewmateRolesCountMax) || (option == CustomOptionHolder.neutralRolesCountMax) || (option == CustomOptionHolder.impostorRolesCountMax)) {
                        continue;
                    } else {
                        sb.AppendLine($"{option.name}: {option.selections[option.selection].ToString()}");
                    }
                    
                }
            }
            CustomOption parent = null;
            foreach (CustomOption option in CustomOption.options)
                if (option.parent != null) {
                    if (option.parent != parent) {
                        sb.AppendLine();
                        parent = option.parent;
                    }
                    sb.AppendLine($"{option.name}: {option.selections[option.selection].ToString()}");
                }

            var hudString = sb.ToString();

            int defaultSettingsLines = 23;
            int roleSettingsLines = defaultSettingsLines + 40;
            int detailedSettingsP1 = roleSettingsLines + 40;
            int detailedSettingsP2 = detailedSettingsP1 + 42;
            int detailedSettingsP3 = detailedSettingsP2 + 42;
            int end1 = hudString.TakeWhile(c => (defaultSettingsLines -= (c == '\n' ? 1 : 0)) > 0).Count();
            int end2 = hudString.TakeWhile(c => (roleSettingsLines -= (c == '\n' ? 1 : 0)) > 0).Count();
            int end3 = hudString.TakeWhile(c => (detailedSettingsP1 -= (c == '\n' ? 1 : 0)) > 0).Count();
            int end4 = hudString.TakeWhile(c => (detailedSettingsP2 -= (c == '\n' ? 1 : 0)) > 0).Count();
            int end5 = hudString.TakeWhile(c => (detailedSettingsP3 -= (c == '\n' ? 1 : 0)) > 0).Count();
            int counter = TheOtherRolesPlugin.optionsPage;
            if (counter == 0) {
                hudString = hudString.Substring(0, end1) + "\n";   
            } else if (counter == 1) {
                hudString = hudString.Substring(end1 + 1, end2 - end1);
                // Temporary fix, should add a new CustomOption for spaces
                int gap = 2;
                int index = hudString.TakeWhile(c => (gap -= (c == '\n' ? 1 : 0)) > 0).Count();
                hudString = hudString.Insert(index, "\n");
                gap = 6;
                index = hudString.TakeWhile(c => (gap -= (c == '\n' ? 1 : 0)) > 0).Count();
                hudString = hudString.Insert(index, "\n");
                gap = 20;
                index = hudString.TakeWhile(c => (gap -= (c == '\n' ? 1 : 0)) > 0).Count();
                hudString = hudString.Insert(index + 1, "\n");
                gap = 26;
                index = hudString.TakeWhile(c => (gap -= (c == '\n' ? 1 : 0)) > 0).Count();
                hudString = hudString.Insert(index + 1, "\n");
            } else if (counter == 2) {
                hudString = hudString.Substring(end2 + 1, end3 - end2);
            } else if (counter == 3) {
                hudString = hudString.Substring(end3 + 1, end4 - end3);
            } else if (counter == 4) {
                hudString = hudString.Substring(end4 + 1, end5 - end4);
            } else if (counter == 5) {
                hudString = hudString.Substring(end5 + 1);
            }

            hudString += $"\n Press tab for more... ({counter+1}/6)";
            __result = hudString;
        }
    }

    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class GameOptionsNextPagePatch
    {
        public static void Postfix(KeyboardJoystick __instance)
        {
            if(Input.GetKeyDown(KeyCode.Tab)) {
                TheOtherRolesPlugin.optionsPage = (TheOtherRolesPlugin.optionsPage + 1) % 6;
            }
        }
    }

    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class GameSettingsScalePatch {
        public static void Prefix(HudManager __instance) {
            if (__instance.GameSettings != null) __instance.GameSettings.fontSize = 1.2f; 
        }
    }
}