using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UI.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiniMap
{
    [BepInPlugin("aedenthorn.MiniMap", "Mini Map", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static bool skip;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> mapEnabled;
        public static ConfigEntry<bool> hideInDungeons;
        public static ConfigEntry<Vector2> mapOffset;
        public static ConfigEntry<float> mapScale;
        public static BepInExPlugin context;
        public static GameObject miniMapObject;
        public static QuestLogModelView modelView;
        public static UIQuestMap uiqm;
        
        public static ConfigEntry<string> toggleKey;
        public static InputAction toggleAction;

        public static void Dbgl(object str, LogLevel level = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug log");
            mapEnabled = Config.Bind<bool>("Options", "MapEnabled", true, "Enable map");
            hideInDungeons = Config.Bind<bool>("Options", "HideInDungeons", true, "Hide mini map while in dungeons");
            mapScale = Config.Bind<float>("Options", "MapScale", 0.5f, "Map scale (1.0 is full-sized map)");
            mapOffset = Config.Bind<Vector2>("Options", "MapOffset", new Vector2(-5, -35), "Map position offset from top right corner");
            toggleKey = Config.Bind<string>("General", "PlaceKey", "<Keyboard>/end", "Place key");

            toggleAction = new InputAction(binding: toggleKey.Value);
            toggleAction.Enable();
            toggleAction.performed += ToggleAction_performed;

            mapScale.SettingChanged += SettingChanged;
            mapOffset.SettingChanged += SettingChanged;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID );

        }

        private void SettingChanged(object sender, EventArgs e)
        {
            RefreshMiniMap();
        }

        public void ToggleAction_performed(InputAction.CallbackContext obj)
        {
            if(miniMapObject != null)
            {
                mapEnabled.Value = !mapEnabled.Value;
                RefreshMiniMap();
            }
            
        }

        [HarmonyPatch(typeof(UIQuestLogWindow), "Awake")]
        public static class UIQuestLogWindow_Awake_Patch
        {

            public static void Postfix(UIQuestLogWindow __instance, UIQuestMap ___m_questMap,  UIWindow ___m_mapView)
            {
                if (!modEnabled.Value)
                    return;
                ___m_questMap.gameObject.SetActive(false);
                miniMapObject = Instantiate(___m_questMap.gameObject, __instance.transform.parent);
                miniMapObject.name = "MiniMap";
                uiqm = miniMapObject.GetComponent<UIQuestMap>();
                uiqm.PlayerGameUI = ___m_questMap.PlayerGameUI;
                AccessTools.Field(typeof(UIQuestMap), "m_imageMapRect").SetValue(uiqm, uiqm.GetComponent<RectTransform>());
                Destroy(miniMapObject.GetComponent<Image>());
                Destroy(miniMapObject.transform.Find("TitlePanelText").gameObject);
                ___m_questMap.gameObject.SetActive(true);
                miniMapObject.SetActive(true);
                RefreshMiniMap();
                miniMapObject.AddComponent<MiniMapUpdater>();
                Dbgl("Created minimap");
            }

        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.CurrentLevel))]
        [HarmonyPatch(MethodType.Setter)]
        public static class PlayerController_CurrentLevel_Patch
        {

            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                RefreshMiniMap();
            }
        }

        public static void RefreshMiniMap()
        {
            if (miniMapObject == null || uiqm?.PlayerGameUI?.Player?.Level == null)
                return;
            if(hideInDungeons.Value && uiqm.PlayerGameUI.Player.Level.IsDungeon() == true)
            {
                Dbgl("hiding in dungeon");

                miniMapObject.SetActive(false);
                return;
            }
            if (miniMapObject.activeSelf != mapEnabled.Value)
            {
                miniMapObject.SetActive(mapEnabled.Value);
                Dbgl($"toggling: {mapEnabled.Value}");
            }
            if (!miniMapObject.activeSelf)
            {
                Dbgl($"not active, cancelling refresh");
               return;
            }

            Dbgl("Refreshing minimap");

            miniMapObject.GetComponent<RectTransform>().anchoredPosition = mapOffset.Value;
            var offset = miniMapObject.transform.Find("FarmMap").GetComponent<RectTransform>().rect.size * -mapScale.Value;
            foreach (Transform c in miniMapObject.transform)
            {
                var rt = c.GetComponent<RectTransform>();
                rt.pivot = Vector3.one;
                rt.anchorMax = Vector3.one;
                rt.anchorMin = Vector3.one;
                rt.localScale = Vector3.one * mapScale.Value;
                if (c.name.EndsWith("Layer"))
                {
                    rt.anchoredPosition = offset;
                }
            }
            //foreach (var name in new string[] { "m_backgroundLayer", "m_priorityLayer", "m_trailLayer" })
            //{
            //    RectTransform transform = (RectTransform)AccessTools.Field(typeof(UIQuestMap), name).GetValue(uiqm);
            //    while (transform.childCount > 0)
            //    {
            //        DestroyImmediate(transform.GetChild(0).gameObject);
            //    }
            //}
            if(modelView != null)
            {
                foreach (QuestMapLayerDTO questMapLayerDTO in modelView.MapMarkers)
                {
                    uiqm.RemoveLayer(questMapLayerDTO);
                }
                foreach (QuestLogModelView.QuestLogQuestModelView questLogQuestModelView in modelView.Quests)
                {
                    uiqm.RemovePins(questLogQuestModelView.MapMarkers);
                }
            }
            modelView = new QuestLogModelView(uiqm.PlayerGameUI.Player);
            foreach (var layer in modelView.MapMarkers)
            {
                for (int i = 0; i < layer.Markers.Count; i++)
                {
                    if (layer.Markers[i].MarkerIconType == EQuestMarkerIcon.LOCAL_PLAYER)
                    {
                        Dbgl("Got player");
                        layer.Markers[i] = new QuestMarkerPlayerDTO(uiqm.PlayerGameUI.Player, EQuestMarkerIcon.LOCAL_PLAYER);
                    }
                }
            }
            uiqm.CreateLayers(modelView.MapMarkers);
            
        }
    }
}
