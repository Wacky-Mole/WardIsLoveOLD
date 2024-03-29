﻿using System;
using UnityEngine;
using UnityEngine.UI;
using WardIsLove.Util.Bubble;
using Object = UnityEngine.Object;

namespace WardIsLove.Util.UI
{
    public class WardGUI
    {
        public static GameObject wardGUI;
        public static GameObject wardGUINoAdmin;
        public static WardMonoscript interactedWard;
        public static Dropdown EffectAreaDropdown;
        public static Dropdown FeedbackDropdown;
        public static Text EffectAreaDropdownText;
        public static Text FeedbackDropdownText;
        public static string FeedbackDropdownValue;

        public static bool IsPanelVisible()
        {
            return (wardGUI && wardGUI.activeSelf) || (wardGUINoAdmin && wardGUINoAdmin.activeSelf);
        }

        public static void Init()
        {
            AssetBundle wardMenuBundle = WardIsLovePlugin.GetAssetBundle("wardislove");
            GameObject go2 =
                wardMenuBundle.LoadAsset<GameObject>("Assets/CustomItems/Wards/WardIsLoveGUINoAdmin.prefab");
            GameObject go = wardMenuBundle.LoadAsset<GameObject>("Assets/CustomItems/Wards/WardIsLoveGUI.prefab");
            WardIsLovePlugin.LightningVFX =
                wardMenuBundle.LoadAsset<GameObject>("Assets/CustomItems/Wards/wardlightningAOE.prefab");
            try
            {
                WardIsLovePlugin.Thorward =
                    wardMenuBundle.LoadAsset<GameObject>("Assets/CustomItems/Wards/Thorward.prefab");
                WardIsLovePlugin.Thorward.GetComponent<WardMonoscript>().m_bubble.AddComponent<CollisionBubble>();


                wardGUI = Object.Instantiate(go);
                Object.DontDestroyOnLoad(wardGUI);
                wardGUINoAdmin = Object.Instantiate(go2);
                Object.DontDestroyOnLoad(wardGUINoAdmin);
                FeedbackDropdown = wardGUI.transform
                    .Find("Canvas/UI/panel/Tabs/FeedbackPanel/FeedbackType/FeedbackTypeDropdown")
                    .GetComponent<Dropdown>();
                FeedbackDropdownText = wardGUI.transform
                    .Find("Canvas/UI/panel/Tabs/FeedbackPanel/FeedbackType/FeedbackTypeDropdown/Label")
                    .GetComponent<Text>();
                FeedbackDropdown.onValueChanged.AddListener(DropdownSelection);

                FeedbackDropdown.ClearOptions();
            }
            catch
            {
                // ignored
            }

            for (int i = 0; i < Enum.GetNames(typeof(WardIsLovePlugin.WardGUIFeedbackEnums)).Length; i++)
            {
                string en = GetString((WardIsLovePlugin.WardGUIFeedbackEnums)i);

                FeedbackDropdown.options.Add(new Dropdown.OptionData { text = en });
            }

            /////Here initialize UI (write the data you want to text, etc.
            wardGUI.SetActive(false);
            wardGUINoAdmin.SetActive(false);
        }


        private static void DropdownSelection(int arg0)
        {
            WardIsLovePlugin.WardGUIFeedbackEnums en = (WardIsLovePlugin.WardGUIFeedbackEnums)arg0;

            FeedbackDropdownValue = en.ToString();
        }

        private static string GetString(WardIsLovePlugin.WardGUIFeedbackEnums me)
        {
            return me switch
            {
                WardIsLovePlugin.WardGUIFeedbackEnums.Feedback => Localization.instance.Localize(
                    "$wardmenu_optionfeedback"),
                WardIsLovePlugin.WardGUIFeedbackEnums.Bug => Localization.instance.Localize("$wardmenu_optionbug"),
                WardIsLovePlugin.WardGUIFeedbackEnums.Idea => Localization.instance.Localize("$wardmenu_optionidea"),
                _ => "ERROR"
            };
        }

        public static void Hide()
        {
            ShowCursor(false);
            wardGUI.SetActive(false);
            wardGUINoAdmin.SetActive(false);
        }

        public static void Show(WardMonoscript ward)
        {
            SetInteractedPa(ward);

            if (ward.m_piece.IsCreator() && !WardIsLovePlugin.Admin)
            {
                wardGUINoAdmin.SetActive(true);
            }
            else
            {
                wardGUI.SetActive(true);
            }

            ShowCursor(true);
        }

        private static void SetInteractedPa(WardMonoscript pa)
        {
            interactedWard = pa;
        }

        internal static WardMonoscript PassInWardMonoscriptToGui()
        {
            return interactedWard;
        }

        public static void ShowCursor(bool flag)
        {
            if (!Player.m_localPlayer) return;

            if (flag)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                GameCamera.instance.enabled = false;
                Player.m_localPlayer.GetComponent<PlayerController>().enabled = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                GameCamera.instance.enabled = true;
                Player.m_localPlayer.GetComponent<PlayerController>().enabled = true;
            }
        }
    }
}