using UnityEngine;

// Detects when the player's ship crosses a sector boundary and handles all
// the work needed to update sector tracking correctly.
//
// Responsibilities:
//   1. Continuously check if the player's local position has moved outside
//      the current sector's bounds (±sectorSizeUnits/2 on X and Z).
//   2. When a boundary is crossed:
//      a. Update SectorX / SectorY on the player's NetworkedSectorObject.
//      b. Recalculate LocalPosition inside the new sector.
//      c. Notify ClientSectorOrigin so the floating origin is updated.
//      d. Notify SectorActivationManager so sectors are spawned/despawned.
//      e. Trigger a FishNet observer rebuild for the player's NetworkObject
//         so the server re-evaluates which objects the client should see.
//
// Place this MonoBehaviour on the player's ship GameObject alongside its
// NetworkedSectorObject and (on the client) its ClientSectorOrigin reference.
public class SectorTransitionManager : MonoBehaviour
{
    // Reference to the NetworkedSectorObject on the player's ship.
    // This tracks the current sector index and visibility category.
    // Assign this in the Inspector or call Initialize from a spawn script.
    public NetworkedSectorObject playerSectorObject;

    // Reference to the client-side floating origin component.
    // Assign this in the Inspector or set it from the player spawn flow.
    public ClientSectorOrigin clientSectorOrigin;

    // Reference to the SectorActivationManager that manages active sector content.
    // Assign this in the Inspector.
    public SectorActivationManager activationManager;

    // Size of one sector in world units. Must match BigBangConfig.sectorSizeUnits.
    public float sectorSizeUnits = 100000f;

    // Half the sector size. The sector extends from -halfSize to +halfSize on each axis.
    private float halfSectorSize;

    // Called by Unity when the component is first enabled.
    private void Awake()
    {
        // Precompute half sector size so it does not have to be recalculated every frame.
        halfSectorSize = sectorSizeUnits * 0.5f;
    }

    // Called by Unity once per frame. Checks whether the player has crossed a boundary.
    private void Update()
    {
        // Do not check if references are not set up yet.
        if (playerSectorObject == null)
        {
            return;
        }

        // Read the player ship's current world position.
        Vector3 worldPos = transform.position;

        // Check whether the player has moved outside the current sector's bounds.
        CheckForSectorCrossing(worldPos);
    }

    // Initializes this manager's references when the player ship is spawned.
    // Call this from the player spawn flow after all components are set up.
    // sectorObject: the NetworkedSectorObject on the player ship.
    // origin: the ClientSectorOrigin for this client.
    // manager: the SectorActivationManager in the scene.
    public void Initialize(
        NetworkedSectorObject sectorObject,
        ClientSectorOrigin origin,
        SectorActivationManager manager)
    {
        // Store the references.
        playerSectorObject = sectorObject;
        clientSectorOrigin = origin;
        activationManager = manager;

        // Set the initial sector on the ClientSectorOrigin.
        if (clientSectorOrigin != null)
        {
            clientSectorOrigin.SetPlayerSector(
                playerSectorObject.SectorX,
                playerSectorObject.SectorY
            );
        }
    }

    // Checks whether the given world position has left the current sector and,
    // if so, triggers a sector transition.
    // worldPos: the player's current world-space position.
    private void CheckForSectorCrossing(Vector3 worldPos)
    {
        // Compute the current sector's local origin in world space.
        // The player's sector maps to (0, 0, 0) area in the floating origin.
        // The bounds extend halfSectorSize in each direction.

        // Determine if the player has crossed the right boundary (positive X).
        if (worldPos.x > halfSectorSize)
        {
            // Move one sector to the right.
            HandleSectorCrossing(1, 0, worldPos);
            return;
        }

        // Determine if the player has crossed the left boundary (negative X).
        if (worldPos.x < -halfSectorSize)
        {
            // Move one sector to the left.
            HandleSectorCrossing(-1, 0, worldPos);
            return;
        }

        // Determine if the player has crossed the upper boundary (positive Z).
        if (worldPos.z > halfSectorSize)
        {
            // Move one sector upward.
            HandleSectorCrossing(0, 1, worldPos);
            return;
        }

        // Determine if the player has crossed the lower boundary (negative Z).
        if (worldPos.z < -halfSectorSize)
        {
            // Move one sector downward.
            HandleSectorCrossing(0, -1, worldPos);
        }
    }

    // Handles the full sector transition when the player crosses a boundary.
    // Updates sector indices, recalculates local position, and notifies all
    // relevant systems.
    // deltaX: how many sectors to move on the X axis (-1, 0, or +1).
    // deltaY: how many sectors to move on the Y axis (-1, 0, or +1).
    // worldPos: the player's world position at the moment of crossing.
    private void HandleSectorCrossing(int deltaX, int deltaY, Vector3 worldPos)
    {
        // Calculate the new sector indices.
        int newSectorX = playerSectorObject.SectorX + deltaX;
        int newSectorY = playerSectorObject.SectorY + deltaY;

        // Recalculate the player's local position inside the new sector.
        // Subtract one full sector worth of units in the direction moved.
        float newLocalX = worldPos.x - deltaX * sectorSizeUnits;
        float newLocalZ = worldPos.z - deltaY * sectorSizeUnits;

        // Move the player's GameObject to the new local position so the
        // floating origin remains valid.
        transform.position = new Vector3(newLocalX, worldPos.y, newLocalZ);

        // Log the transition so developers can trace border crossings easily.
        Debug.Log(
            "SectorTransitionManager: Player crossed sector boundary. " +
            "Old sector: (" + playerSectorObject.SectorX + ", " + playerSectorObject.SectorY + ") " +
            "New sector: (" + newSectorX + ", " + newSectorY + ")"
        );

        // Update the NetworkedSectorObject with the new sector indices.
        playerSectorObject.SetSector(newSectorX, newSectorY, playerSectorObject.VisibilityCategory);

        // Update the ClientSectorOrigin so all visuals are rebased to the new sector.
        if (clientSectorOrigin != null)
        {
            clientSectorOrigin.SetPlayerSector(newSectorX, newSectorY);
        }

        // Notify the SectorActivationManager to spawn/despawn sectors as needed.
        if (activationManager != null)
        {
            activationManager.OnPlayerChangedSector(newSectorX, newSectorY);
        }

        // TODO: Trigger a FishNet observer rebuild so the server re-evaluates
        // which NetworkObjects are visible to this client.
        // This requires access to the FishNet NetworkObject on the player ship.
        // Example (once FishNet NetworkObject reference is available):
        //   playerNetworkObject.NetworkManager.ServerManager.Objects.RebuildObservers(playerNetworkObject);
        // See Docs/FishNet-Networking-Reference.md for observer rebuild patterns.
    }
}
