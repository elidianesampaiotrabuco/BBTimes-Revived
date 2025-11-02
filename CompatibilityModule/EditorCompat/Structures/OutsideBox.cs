using System.IO;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.GlobalSettingsMenus;
using PlusStudioLevelFormat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBTimes.CompatibilityModule.EditorCompat.Structures;

public class OutsideBoxUIHandler : GlobalStructureUIHandler
{
    // Input
    public TextMeshProUGUI outsideLightColor_R, outsideLightColor_G, outsideLightColor_B, outsideStrength;
    public MenuToggle shouldIncludeGrass_tick, isOnLastFloor_tick;
    // Display
    public Image hexDisplay;

    // Structure
    public OutsideBoxLocation OutsideStructure => structure == null ? null : (OutsideBoxLocation)structure;

    public override bool GetStateBoolean(string key) => false;

    public override void OnElementsCreated()
    {
        outsideLightColor_R = transform.Find("OutsideColor_R").GetComponent<TextMeshProUGUI>();
        outsideLightColor_G = transform.Find("OutsideColor_G").GetComponent<TextMeshProUGUI>();
        outsideLightColor_B = transform.Find("OutsideColor_B").GetComponent<TextMeshProUGUI>();
        outsideStrength = transform.Find("OutsideStrength").GetComponent<TextMeshProUGUI>();
        shouldIncludeGrass_tick = transform.Find("includeGrassTick").GetComponent<MenuToggle>();
        isOnLastFloor_tick = transform.Find("lastFloorTick").GetComponent<MenuToggle>();
        hexDisplay = transform.Find("OutsideColorDisplay").GetComponent<Image>();
    }

    public override void PageLoaded(StructureLocation structure)
    {
        base.PageLoaded(structure);
        outsideLightColor_R.text = OutsideStructure.outsideColor.r.ToString();
        outsideLightColor_G.text = OutsideStructure.outsideColor.g.ToString();
        outsideLightColor_B.text = OutsideStructure.outsideColor.b.ToString();

        shouldIncludeGrass_tick.Set(OutsideStructure.hasFloorOutside);
        isOnLastFloor_tick.Set(OutsideStructure.isAtLastFloor);

        outsideStrength.text = OutsideStructure.outsideStrength.ToString();

        hexDisplay.color = OutsideStructure.outsideColor;
    }

    public override void SendInteractionMessage(string message, object data = null)
    {
        byte value;
        switch (message)
        {
            case "outsideColorEnter_R":
                if (byte.TryParse((string)data, out value))
                {
                    if (value < 0) value = 0;
                    OutsideStructure.outsideColor.r = value;
                }
                PageLoaded(structure);
                break;
            case "outsideColorEnter_G":
                if (byte.TryParse((string)data, out value))
                {
                    if (value < 0) value = 0;
                    OutsideStructure.outsideColor.g = value;
                }
                PageLoaded(structure);
                break;
            case "outsideColorEnter_B":
                if (byte.TryParse((string)data, out value))
                {
                    if (value < 0) value = 0;
                    OutsideStructure.outsideColor.b = value;
                }
                PageLoaded(structure);
                break;
            case "outsideColorStrength":
                if (byte.TryParse((string)data, out value))
                {
                    if (value < 0) value = 0;
                    OutsideStructure.outsideStrength = value;
                }
                PageLoaded(structure);
                break;
            case "includeGrassTick":
                if (data is bool toggle1)
                {
                    OutsideStructure.hasFloorOutside = toggle1;
                }
                PageLoaded(structure);
                break;
            case "includeLastFloor":
                if (data is bool toggle2)
                {
                    OutsideStructure.isAtLastFloor = toggle2;
                }
                PageLoaded(structure);
                break;
        }
    }
}

public class OutsideBoxLocation : RandomStructureLocation
{
    // Unimplemented stuff
    public override void AddStringsToCompressor(StringCompressor compressor) { }
    public override void CleanupVisual(GameObject visualObject) { }
    public override GameObject GetVisualPrefab() => null;
    public override void InitializeVisual(GameObject visualObject) { }
    public override void ShiftBy(Vector3 worldOffset, IntVector2 cellOffset, IntVector2 sizeDifference) { }
    public override bool ValidatePosition(EditorLevelData data) => true;
    public override void UpdateVisual(GameObject visualObject) { }

    // Actually used
    // *Fields*
    public Color32 outsideColor = Color.white;
    public bool hasFloorOutside = false, isAtLastFloor = false;
    public byte outsideStrength = 0;
    // *Methods*
    public override RandomStructureInfo CompileIntoRandom(EditorLevelData data, BaldiLevel level) =>
        new()
        {
            type = type,
            info = new StructureParameterInfo()
            {
                chance = [
                    hasFloorOutside ? 1f : 0f,
                    outsideColor.r,
                    outsideColor.g,
                    outsideColor.b,
                    isAtLastFloor ? 1f : 0f,
                    outsideStrength
                ]
            }
        };

    public override void ReadInto(EditorLevelData data, BinaryReader reader, StringCompressor compressor)
    {
        _ = reader.ReadByte(); // version
        hasFloorOutside = reader.ReadBoolean();
        outsideColor.r = reader.ReadByte();
        outsideColor.g = reader.ReadByte();
        outsideColor.b = reader.ReadByte();
        outsideStrength = reader.ReadByte();
        isAtLastFloor = reader.ReadBoolean();
    }


    public override void Write(EditorLevelData data, BinaryWriter writer, StringCompressor compressor)
    {
        writer.Write((byte)0);
        writer.Write(hasFloorOutside);
        writer.Write(outsideColor.r);
        writer.Write(outsideColor.g);
        writer.Write(outsideColor.b);
        writer.Write(outsideStrength);
        writer.Write(isAtLastFloor);
    }
}