using System.Collections.Generic;
using UnityEngine;

// Spawns and despawns non-networked (client-side only) visual GameObjects
// for galaxy objects such as stars, planets, black holes, and asteroid fields.
//
// This factory is intentionally NOT used for NetworkObjects (ships, stations).
// It is purely for decorative and strategic-landmark visuals that each
// client renders locally without any server replication.
//
// Assign one prefab per GalaxyObjectType in the Inspector. If a type has
// no prefab assigned, SpawnVisual will log a warning and skip that object.
//
// Place this MonoBehaviour on a client-side manager GameObject in the main scene.
public class SectorVisualFactory : MonoBehaviour
{
    // Prefab used to spawn a visual for a star object.
    // Assign a non-networked star GameObject prefab here in the Inspector.
    public GameObject starPrefab;

    // Prefab used to spawn a visual for a planet object.
    // Assign a non-networked planet GameObject prefab here in the Inspector.
    public GameObject planetPrefab;

    // Prefab used to spawn a visual for a black hole object.
    // Assign a non-networked black hole GameObject prefab here in the Inspector.
    public GameObject blackHolePrefab;

    // Prefab used to spawn a visual for an asteroid field object.
    // Assign a non-networked asteroid field GameObject prefab here in the Inspector.
    public GameObject asteroidFieldPrefab;

    // Prefab used to spawn a visual for a jump gate object.
    // Assign a non-networked jump gate GameObject prefab here in the Inspector.
    // Note: interactive jump gates use a NetworkObject instead; this prefab is
    // for client-only landmark rendering when in an adjacent sector.
    public GameObject jumpGatePrefab;

    // The size of one sector in world units. Used together with ClientSectorOrigin
    // to convert a GalaxyPosition into a rendered world position.
    // Should match the sectorSizeUnits value in BigBangConfig.
    public float sectorSizeUnits = 100000f;

    // Reference to the ClientSectorOrigin. Used to compute rendered world positions
    // relative to the player's current sector.
    // Assign this in the Inspector.
    public ClientSectorOrigin clientSectorOrigin;

    // Dictionary from object ID to the currently spawned GameObject.
    // Used to find and destroy visuals by ID when a sector is deactivated.
    private Dictionary<string, GameObject> spawnedVisuals = new Dictionary<string, GameObject>();

    // Spawns a client-side visual GameObject for the given galaxy object data.
    // If a visual with the same ID already exists it will not be spawned again.
    // data: the GalaxyObjectData describing what to spawn and where.
    public void SpawnVisual(GalaxyObjectData data)
    {
        // TacticalNetworked objects (ships, stations) must not be spawned here.
        // They are managed by FishNet NetworkObject spawning on the server.
        if (data.VisibilityCategory == SectorVisibilityCategory.TacticalNetworked)
        {
            // Skip this object; it is not a client-only visual.
            return;
        }

        // If a visual is already spawned for this ID, do not spawn a duplicate.
        if (spawnedVisuals.ContainsKey(data.Id))
        {
            // Already spawned; nothing more to do.
            return;
        }

        // Select the prefab to use based on the object type.
        GameObject prefab = GetPrefabForType(data.ObjectType);
        if (prefab == null)
        {
            // No prefab assigned for this type; log a warning and skip.
            Debug.LogWarning("SectorVisualFactory: No prefab assigned for GalaxyObjectType " + data.ObjectType);
            return;
        }

        // Compute the rendered world position using the floating origin.
        Vector3 worldPosition = ComputeRenderedPosition(data.Position);

        // Instantiate the prefab at the computed world position with no rotation.
        GameObject spawnedObject = Instantiate(prefab, worldPosition, Quaternion.identity);

        // Name the object after its ID for easy identification in the hierarchy.
        spawnedObject.name = data.Id;

        // Attach a NetworkedSectorObject component so other systems can read the sector.
        NetworkedSectorObject sectorComponent = spawnedObject.GetComponent<NetworkedSectorObject>();
        if (sectorComponent == null)
        {
            // Add the component if it is not already on the prefab.
            sectorComponent = spawnedObject.AddComponent<NetworkedSectorObject>();
        }

        // Set the sector coordinates and visibility category on the component.
        sectorComponent.SetSector(data.Position.SectorX, data.Position.SectorY, data.VisibilityCategory);

        // Store the spawned GameObject so we can destroy it later.
        spawnedVisuals[data.Id] = spawnedObject;
    }

    // Destroys the visual GameObject that was spawned for the given object ID.
    // Does nothing if no visual with that ID is currently spawned.
    // objectId: the unique ID of the GalaxyObjectData whose visual should be removed.
    public void DespawnVisual(string objectId)
    {
        // Check whether a visual exists for this ID.
        if (!spawnedVisuals.ContainsKey(objectId))
        {
            // Nothing to despawn.
            return;
        }

        // Get the GameObject and destroy it.
        GameObject visual = spawnedVisuals[objectId];
        if (visual != null)
        {
            // Destroy the GameObject immediately.
            Destroy(visual);
        }

        // Remove the entry from the dictionary.
        spawnedVisuals.Remove(objectId);
    }

    // Returns the prefab associated with the given GalaxyObjectType.
    // Returns null if no prefab has been assigned for that type.
    // objectType: the GalaxyObjectType to look up a prefab for.
    private GameObject GetPrefabForType(GalaxyObjectType objectType)
    {
        // Return the correct prefab based on the object type.
        if (objectType == GalaxyObjectType.Star)
        {
            return starPrefab;
        }

        if (objectType == GalaxyObjectType.Planet)
        {
            return planetPrefab;
        }

        if (objectType == GalaxyObjectType.BlackHole)
        {
            return blackHolePrefab;
        }

        if (objectType == GalaxyObjectType.AsteroidField)
        {
            return asteroidFieldPrefab;
        }

        if (objectType == GalaxyObjectType.JumpGate)
        {
            return jumpGatePrefab;
        }

        // Ships (TacticalNetworked) are spawned through FishNet network spawning and
        // should never reach the visual factory. Return null to signal no prefab.
        if (objectType == GalaxyObjectType.Ship)
        {
            return null;
        }

        // Stations (TacticalNetworked) are spawned through FishNet network spawning and
        // should never reach the visual factory. Return null to signal no prefab.
        if (objectType == GalaxyObjectType.Station)
        {
            return null;
        }

        // Unknown type; return null so the caller can log a warning.
        return null;
    }

    // Converts a GalaxyPosition into a world-space Vector3 that accounts for
    // the client's floating sector origin. The player's current sector is treated
    // as the local (0, 0, 0) area so that world coordinates stay small.
    //
    // Formula:
    //   RenderedPosition = ((ObjectSector - PlayerSector) * SectorSize) + ObjectLocalPosition
    //
    // position: the GalaxyPosition of the object to compute a world position for.
    // Returns the Vector3 world position for rendering.
    private Vector3 ComputeRenderedPosition(GalaxyPosition position)
    {
        // Default player sector to (0, 0) if no origin component is assigned.
        int playerSectorX = 0;
        int playerSectorY = 0;

        // Read the player's current sector from the ClientSectorOrigin if available.
        if (clientSectorOrigin != null)
        {
            playerSectorX = clientSectorOrigin.PlayerSectorX;
            playerSectorY = clientSectorOrigin.PlayerSectorY;
        }

        // Compute the offset in sectors from the player's sector to the object's sector.
        float sectorOffsetX = (position.SectorX - playerSectorX) * sectorSizeUnits;
        float sectorOffsetY = (position.SectorY - playerSectorY) * sectorSizeUnits;

        // The rendered world position is the sector offset plus the object's local position.
        return new Vector3(
            sectorOffsetX + position.LocalPosition.x,
            position.LocalPosition.y,
            sectorOffsetY + position.LocalPosition.z
        );
    }
}
