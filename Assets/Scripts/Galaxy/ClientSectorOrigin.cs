using UnityEngine;

// Tracks the player's current sector and provides a floating origin for
// converting galaxy coordinates into small, precision-safe world positions.
//
// Because the galaxy can be 500×500 sectors each 100,000 units wide, raw
// galaxy-space coordinates would reach hundreds of millions of units and
// cause severe floating-point precision errors in Unity. The floating origin
// keeps everything relative to the player's sector so actual scene
// coordinates stay near zero.
//
// IMPORTANT: This component only affects local rendering. It must never
// modify server-authoritative data (sector indices, GalaxyObjectData, etc.).
//
// Place this MonoBehaviour on the local player GameObject or on a dedicated
// client-side manager object in the scene.
public class ClientSectorOrigin : MonoBehaviour
{
    // X index of the sector the local player is currently in.
    // This is the origin sector; all rendered positions are computed relative to it.
    public int PlayerSectorX;

    // Y index of the sector the local player is currently in.
    public int PlayerSectorY;

    // Size of one sector in world units. Must match BigBangConfig.sectorSizeUnits.
    public float SectorSizeUnits = 100000f;

    // Sets the player's current sector coordinates and fires a rebase if needed.
    // Call this when the player moves into a new sector.
    // newSectorX: X index of the new sector.
    // newSectorY: Y index of the new sector.
    public void SetPlayerSector(int newSectorX, int newSectorY)
    {
        // Check whether the sector has actually changed.
        bool sectorChanged = (newSectorX != PlayerSectorX || newSectorY != PlayerSectorY);

        // Store the new sector coordinates.
        PlayerSectorX = newSectorX;
        PlayerSectorY = newSectorY;

        // If the sector changed we log it so it is easy to trace in the editor.
        if (sectorChanged)
        {
            Debug.Log(
                "ClientSectorOrigin: Player sector updated to " +
                PlayerSectorX + ", " + PlayerSectorY
            );
        }
    }

    // Converts a GalaxyPosition into a rendered world-space Vector3 using the
    // floating origin formula.
    //
    // Formula:
    //   RenderedPosition = ((ObjectSector - PlayerSector) * SectorSize) + ObjectLocalPosition
    //
    // This keeps the player's sector at roughly (0, 0, 0) in world space
    // regardless of where in the galaxy they are.
    //
    // objectPosition: the GalaxyPosition of the object to convert.
    // Returns the world-space Vector3 the object should be rendered at.
    public Vector3 ComputeRenderedPosition(GalaxyPosition objectPosition)
    {
        // Compute how many sectors the object is offset from the player on each axis.
        float sectorOffsetX = (objectPosition.SectorX - PlayerSectorX) * SectorSizeUnits;
        float sectorOffsetY = (objectPosition.SectorY - PlayerSectorY) * SectorSizeUnits;

        // Add the object's local position inside its sector to the sector offset.
        // X and Z map to the 2D galaxy grid; Y is used for height.
        return new Vector3(
            sectorOffsetX + objectPosition.LocalPosition.x,
            objectPosition.LocalPosition.y,
            sectorOffsetY + objectPosition.LocalPosition.z
        );
    }

    // Converts a world-space rendered position back into a GalaxyPosition.
    // Useful for translating a rendered ship position into its authoritative sector data.
    // This is the inverse of ComputeRenderedPosition.
    //
    // worldPosition: the scene world-space position to convert.
    // Returns the GalaxyPosition that corresponds to the given world position.
    public GalaxyPosition WorldPositionToGalaxyPosition(Vector3 worldPosition)
    {
        // Compute the raw galaxy-space X and Z coordinates.
        float galaxyX = worldPosition.x + PlayerSectorX * SectorSizeUnits;
        float galaxyZ = worldPosition.z + PlayerSectorY * SectorSizeUnits;

        // Determine the sector indices by dividing by sector size.
        int sectorX = Mathf.FloorToInt(galaxyX / SectorSizeUnits);
        int sectorY = Mathf.FloorToInt(galaxyZ / SectorSizeUnits);

        // Compute the local position inside the sector by taking the remainder.
        float localX = galaxyX - sectorX * SectorSizeUnits;
        float localZ = galaxyZ - sectorY * SectorSizeUnits;

        // Build and return the GalaxyPosition.
        GalaxyPosition result = new GalaxyPosition();
        result.SectorX = sectorX;
        result.SectorY = sectorY;
        result.LocalPosition = new Vector3(localX, worldPosition.y, localZ);
        return result;
    }
}
