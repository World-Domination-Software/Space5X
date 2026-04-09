using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;

// A custom FishNet observer condition that controls object visibility based on
// sector proximity rather than physical distance.
//
// Rules:
//   Same Sector      → full visibility (all TacticalNetworked objects visible).
//   Adjacent Sector  → only StrategicLandmark objects are visible; ships and
//                      combat objects are NOT visible.
//   Other Sectors    → not visible at all.
//
// How to use:
//   1. Create a ScriptableObject asset from this class via the Unity editor
//      menu: FishNet / Observers / Sector Condition.
//   2. Add a NetworkObserver component to any networked prefab that should
//      follow these rules.
//   3. Assign the SectorObserverCondition asset to the NetworkObserver's
//      conditions list.
//
// This condition is "Timed" which means FishNet re-evaluates it on a regular
// interval rather than only when something changes. That ensures visibility is
// updated automatically as players move between sectors.
[CreateAssetMenu(menuName = "FishNet/Observers/Sector Condition", fileName = "New Sector Condition")]
public class SectorObserverCondition : ObserverCondition
{
    // Returns whether the NetworkObject this condition is attached to should
    // be visible to the given network connection.
    //
    // The check works in three steps:
    //   1. Find the NetworkedSectorObject component on this condition's NetworkObject
    //      to know what sector the object lives in and its visibility category.
    //   2. Loop through the connection's owned NetworkObjects to find the player
    //      ship's sector.
    //   3. Compare sectors according to the same-sector / adjacent / other rules.
    //
    // connection:     the client connection being checked.
    // currentlyAdded: true if the connection is already an observer of this object.
    // notProcessed:   set to true to skip processing (uses the previous result).
    // Returns true if the connection should observe this object.
    public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
    {
        // This condition processes every timed check, so we never skip.
        notProcessed = false;

        // Get the NetworkedSectorObject component from the object this condition guards.
        // This tells us the sector and visibility category of the guarded object.
        NetworkedSectorObject guardedSector = NetworkObject.GetComponent<NetworkedSectorObject>();
        if (guardedSector == null)
        {
            // No sector data on this object; make it visible by default to avoid
            // incorrectly hiding objects that have not been set up yet.
            return true;
        }

        // Find the player's sector by looking at the connection's owned objects.
        // We specifically look for the object flagged as the player's primary ship
        // (isPlayerShip == true) to avoid using a probe or secondary vessel in a
        // different sector as the reference point.
        int playerSectorX = 0;
        int playerSectorY = 0;
        bool foundPlayerSector = false;

        // Loop through every object owned by this connection.
        foreach (NetworkObject clientObject in connection.Objects)
        {
            // Try to get a NetworkedSectorObject from this owned object.
            NetworkedSectorObject clientSector = clientObject.GetComponent<NetworkedSectorObject>();
            if (clientSector != null && clientSector.isPlayerShip)
            {
                // Use the sector from the connection's primary player ship.
                playerSectorX = clientSector.SectorX;
                playerSectorY = clientSector.SectorY;
                foundPlayerSector = true;

                // Stop searching; we found the primary ship.
                break;
            }
        }

        // If we could not find the player's sector, default to not visible.
        // This handles the case where the player's ship has not finished spawning yet.
        if (!foundPlayerSector)
        {
            return false;
        }

        // Check if the guarded object is in the exact same sector as the player.
        bool isSameSector = guardedSector.IsInSameSector(playerSectorX, playerSectorY);
        if (isSameSector)
        {
            // Same sector means full visibility regardless of object category.
            return true;
        }

        // Check if the guarded object is in an adjacent sector (one step away).
        bool isAdjacentSector = guardedSector.IsInAdjacentSector(playerSectorX, playerSectorY);
        if (isAdjacentSector)
        {
            // In adjacent sectors only StrategicLandmark objects (stars, major planets,
            // black holes) are visible. Ships and stations are NOT visible.
            return guardedSector.VisibilityCategory == SectorVisibilityCategory.StrategicLandmark;
        }

        // The object is in a sector that is neither the same nor adjacent.
        // It is not visible to this connection.
        return false;
    }

    // Returns that this condition is Timed so FishNet checks it periodically.
    // Timed conditions are re-evaluated at regular intervals which is needed
    // for sector-based visibility because players can move between sectors.
    public override ObserverConditionType GetConditionType()
    {
        // Timed means the server checks this condition on a regular schedule,
        // which is required for spatial visibility that changes over time.
        return ObserverConditionType.Timed;
    }
}
