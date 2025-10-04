using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;

namespace BBTimes.CompatibilityModule.EditorCompat;

public class SuperFanMarker : PositionMarker
{
    public override void Compile(EditorLevelData data, BaldiLevel compiled)
    {
        ushort num = data.RoomIdFromPos(position.ToCellVector(), forEditor: true);
        if (num == 0)
        {
            throw new System.Exception("Uh oh, no room found for super fan.");
        }

        compiled.rooms[num - 1].basicObjects.Add(new BasicObjectInfo
        {
            position = position.ToData(),
            prefab = type,
            rotation = default
        });
    }

    public override bool ValidatePosition(EditorLevelData data)
    {
        var vec = position.ToCellVector();
        EditorRoom editorRoom = data.RoomFromPos(vec, forEditor: true);

        return
        editorRoom != null &&
        editorRoom.roomType == "hall" &&
        data.cells[vec.x, vec.z].walls != 0; // It can't be an open area
    }
}