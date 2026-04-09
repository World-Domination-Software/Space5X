using UnityEngine;

// Attach this component to any GameObject that represents a galaxy object
// at a specific sector location. Both networked objects (ships, stations)
// and visual-only objects (stars, planets) should carry this component so
// the SectorObserverCondition and SectorActivationManager can read their
// sector coordinates quickly.
//
// For FishNet NetworkObjects the sector fields are updated by the
// SectorTransitionManager when a ship crosses a sector boundary.
// For visual-only objects the fields are set once by SectorVisualFactory
// when the object is spawned.
public class NetworkedSectorObject : MonoBehaviour
{
    // X index of the sector this object currently occupies in the galaxy grid.
    // For a ship this can change over time. For a static object (star, station)
    // it never changes after the object is spawned.
    public int SectorX;

    // Y index of the sector this object currently occupies in the galaxy grid.
    public int SectorY;

    // The visibility category this object belongs to.
    // Set when the object is spawned; used by the SectorObserverCondition
    // to decide whether the object is visible in adjacent sectors.
    public SectorVisibilityCategory VisibilityCategory;

    // Sets the sector coordinates and visibility category in a single call.
    // sectorX: new X sector index.
    // sectorY: new Y sector index.
    // category: visibility category used by the observer condition.
    public void SetSector(int sectorX, int sectorY, SectorVisibilityCategory category)
    {
        // Store the new sector X index.
        SectorX = sectorX;

        // Store the new sector Y index.
        SectorY = sectorY;

        // Store the visibility category.
        VisibilityCategory = category;
    }

    // Returns true if the given sector coordinates are the same sector as this object.
    // otherX: X index to compare.
    // otherY: Y index to compare.
    public bool IsInSameSector(int otherX, int otherY)
    {
        // Check both axes for an exact sector match.
        return SectorX == otherX && SectorY == otherY;
    }

    // Returns true if the given sector coordinates are directly adjacent to this object's sector.
    // Adjacent means exactly one step away on either the X or Y axis (square grid, no diagonals).
    // otherX: X index to compare.
    // otherY: Y index to compare.
    public bool IsInAdjacentSector(int otherX, int otherY)
    {
        // Compute absolute difference on each axis.
        int dx = Mathf.Abs(SectorX - otherX);
        int dy = Mathf.Abs(SectorY - otherY);

        // Adjacent means exactly one step on one axis and zero on the other.
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
}
