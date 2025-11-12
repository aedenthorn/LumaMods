using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace MiniMap
{
    public class MiniMapUpdater : MonoBehaviour
    {
        public void Update()
        {
            //var selector = AccessTools.FieldRefAccess<UIQuestLogWindow, SelectableSelectorDecorator<UIQuestLogSlot>>(ui, "m_selector");
            foreach (var questMarkerDTO in BepInExPlugin.modelView.MapMarkers.SelectMany((QuestMapLayerDTO it) => it.Markers))
            {
                if (questMarkerDTO.IsDynamic && questMarkerDTO.UpdatePosition())
                {
                    BepInExPlugin.uiqm.PlaceIcon(questMarkerDTO);
                }
            }
            //if (selector.IsAnySelected)
            //{
            //    foreach (QuestMarkerDTO questMarkerDTO3 in selector.GetSelected().GetData().MapMarkers)
            //    {
            //        if (questMarkerDTO3.IsDynamic && questMarkerDTO3.UpdatePosition())
            //        {
            //            uiqm.PlaceIcon(questMarkerDTO3);
            //        }
            //    }
            //}
        }
        //public bool UpdatePosition(QuestMarkerDTO questMarkerDTO)
        //{
        //    bool flag = questMarkerDTO.UpdatePosition();
        //    flag |= questMarkerDTO.Level.Name != questMarkerDTO.PlayerController.Level.Name || !this.LookDirection.Equals(this.PlayerController.Pawn.Facing) || !this.m_levelPosition.Position.Equals(this.PlayerController.LevelPosition.Position);
        //    this.Level = this.PlayerController.Level;
        //    this.m_levelPosition = this.PlayerController.LevelPosition;
        //    this.LookDirection = this.PlayerController.Pawn.Facing;
        //    return flag;
        //}
    }
}