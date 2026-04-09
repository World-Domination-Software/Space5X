using System.Collections.Generic;
using UnityEngine;

// Manages which galaxy sectors are active at any point in time.
//
// Sectors have three activity tiers:
//   Full Activation   – the player's current sector; all objects spawn including ships and stations.
//   Partial Activation – sectors adjacent to the player's sector; only StrategicLandmark visuals spawn.
//   Inactive           – all other sectors; no GameObjects exist, data-only simulation.
//
// When the player moves to a new sector, this manager despawns objects from sectors
// that are no longer needed and spawns objects for newly relevant sectors.
//
// Place this MonoBehaviour on a server-side manager GameObject in the main scene.
public class SectorActivationManager : MonoBehaviour
{
    // Reference to the BigBangGenerator that holds all GalaxySectorData.
    // Assign this in the Inspector.
    public BigBangGenerator bigBangGenerator;

    // Reference to the SectorVisualFactory used to create client-side visuals.
    // Assign this in the Inspector.
    public SectorVisualFactory visualFactory;

    // The sector X index the player is currently in.
    // Updated by SectorTransitionManager whenever the player crosses a border.
    public int playerSectorX;

    // The sector Y index the player is currently in.
    // Updated by SectorTransitionManager whenever the player crosses a border.
    public int playerSectorY;

    // Tracks which sectors are currently fully active, by their "x,y" key.
    // Used to avoid double-spawning or missing a despawn step.
    private HashSet<string> fullyActiveSectors = new HashSet<string>();

    // Tracks which sectors are currently partially active, by their "x,y" key.
    private HashSet<string> partiallyActiveSectors = new HashSet<string>();

    // Lookup table built once at Start that maps sector keys ("x,y") to GalaxySectorData.
    // Using a dictionary avoids a slow linear search through all 250,000 sectors every
    // time a sector needs to be activated or deactivated.
    private Dictionary<string, GalaxySectorData> sectorLookup = new Dictionary<string, GalaxySectorData>();

    // Called by Unity when the game starts. Builds the fast lookup table and
    // then sets up the initial sector activations.
    private void Start()
    {
        // Build the lookup dictionary from the generator's data before any activation.
        BuildSectorLookup();

        // Activate the player's starting sector and its immediate neighbors.
        RefreshActiveSectors();
    }

    // Builds a dictionary from sector key ("x,y") to GalaxySectorData so that
    // FindSectorData runs in O(1) instead of O(n) on every sector change.
    // Called once at Start after the BigBangGenerator has finished generating.
    private void BuildSectorLookup()
    {
        // Clear any stale data from a previous generation.
        sectorLookup.Clear();

        // If there is no generator reference, there is nothing to index.
        if (bigBangGenerator == null)
        {
            return;
        }

        // Get the full list of data-driven sectors.
        List<GalaxySectorData> allSectors = bigBangGenerator.GalaxySectors;
        if (allSectors == null)
        {
            return;
        }

        // Add each sector to the dictionary keyed by its "x,y" string.
        for (int i = 0; i < allSectors.Count; i++)
        {
            GalaxySectorData sector = allSectors[i];
            string key = MakeSectorKey(sector.SectorX, sector.SectorY);
            sectorLookup[key] = sector;
        }

        // Log how many sectors were indexed.
        Debug.Log("SectorActivationManager: Built sector lookup with " + sectorLookup.Count + " entries.");
    }

    // Called by SectorTransitionManager (or any other system) when the player
    // moves to a new sector. Updates activation for the new sector layout.
    // newSectorX: X index of the sector the player just entered.
    // newSectorY: Y index of the sector the player just entered.
    public void OnPlayerChangedSector(int newSectorX, int newSectorY)
    {
        // Update the stored player sector.
        playerSectorX = newSectorX;
        playerSectorY = newSectorY;

        // Recalculate which sectors should be active with the new player position.
        RefreshActiveSectors();
    }

    // Recalculates and updates all active and partial sectors based on the
    // current player sector. Despawns sectors that are no longer needed and
    // spawns sectors that have become relevant.
    public void RefreshActiveSectors()
    {
        // Build the set of sectors that should be fully active (player's sector only).
        HashSet<string> desiredFull = new HashSet<string>();
        desiredFull.Add(MakeSectorKey(playerSectorX, playerSectorY));

        // Build the set of sectors that should be partially active (four adjacent neighbors).
        HashSet<string> desiredPartial = new HashSet<string>();
        desiredPartial.Add(MakeSectorKey(playerSectorX + 1, playerSectorY));
        desiredPartial.Add(MakeSectorKey(playerSectorX - 1, playerSectorY));
        desiredPartial.Add(MakeSectorKey(playerSectorX, playerSectorY + 1));
        desiredPartial.Add(MakeSectorKey(playerSectorX, playerSectorY - 1));

        // Deactivate any sector that was fully active but is no longer needed.
        List<string> toDeactivateFull = new List<string>();
        foreach (string key in fullyActiveSectors)
        {
            // If a previously full sector is not in the new desired sets, deactivate it.
            if (!desiredFull.Contains(key) && !desiredPartial.Contains(key))
            {
                toDeactivateFull.Add(key);
            }
        }
        for (int i = 0; i < toDeactivateFull.Count; i++)
        {
            // Despawn all objects for this sector.
            DeactivateSector(toDeactivateFull[i]);
        }

        // Deactivate any sector that was partially active but is no longer needed.
        List<string> toDeactivatePartial = new List<string>();
        foreach (string key in partiallyActiveSectors)
        {
            if (!desiredFull.Contains(key) && !desiredPartial.Contains(key))
            {
                toDeactivatePartial.Add(key);
            }
        }
        for (int i = 0; i < toDeactivatePartial.Count; i++)
        {
            // Despawn all objects for this sector.
            DeactivateSector(toDeactivatePartial[i]);
        }

        // Fully activate the player's sector if it is not already full.
        foreach (string key in desiredFull)
        {
            if (!fullyActiveSectors.Contains(key))
            {
                // If this sector was partially active, despawn its partial content first.
                if (partiallyActiveSectors.Contains(key))
                {
                    DeactivateSector(key);
                }

                // Fully activate this sector.
                FullyActivateSector(key);
            }
        }

        // Partially activate each neighbor sector if it is not already activated.
        foreach (string key in desiredPartial)
        {
            // Skip sectors that are already fully active.
            if (fullyActiveSectors.Contains(key))
            {
                continue;
            }

            if (!partiallyActiveSectors.Contains(key))
            {
                // Partially activate this neighbor sector.
                PartiallyActivateSector(key);
            }
        }
    }

    // Fully activates the sector identified by the given key.
    // Spawns all objects in the sector including TacticalNetworked objects.
    // key: a "x,y" string identifying the sector.
    private void FullyActivateSector(string key)
    {
        // Find the GalaxySectorData for this key.
        GalaxySectorData sectorData = FindSectorData(key);
        if (sectorData == null)
        {
            // No data found for this key; nothing to spawn.
            return;
        }

        // Spawn all objects in the sector regardless of visibility category.
        for (int i = 0; i < sectorData.Objects.Count; i++)
        {
            GalaxyObjectData obj = sectorData.Objects[i];

            // TacticalNetworked objects (ships, stations, jump gates) must be spawned
            // through FishNet's network spawning system, not the visual factory.
            // TODO: Implement FishNet NetworkObject spawning for TacticalNetworked objects
            // once the server-side spawning infrastructure is in place. See
            // Docs/FishNet-Networking-Reference.md for the recommended spawn flow.
            if (obj.VisibilityCategory == SectorVisibilityCategory.TacticalNetworked)
            {
                // Skip networked objects for now; visual factory handles visuals only.
                continue;
            }

            // Spawn a visual for this StrategicLandmark or BackgroundVisual object
            // through the visual factory.
            visualFactory.SpawnVisual(obj);
        }

        // Mark this sector as fully active.
        fullyActiveSectors.Add(key);

        // Log that the sector is now fully active.
        Debug.Log("SectorActivationManager: Fully activated sector " + key);
    }

    // Partially activates the sector identified by the given key.
    // Only spawns StrategicLandmark objects (stars, major planets, black holes).
    // key: a "x,y" string identifying the sector.
    private void PartiallyActivateSector(string key)
    {
        // Find the GalaxySectorData for this key.
        GalaxySectorData sectorData = FindSectorData(key);
        if (sectorData == null)
        {
            // No data found for this key; nothing to spawn.
            return;
        }

        // Only spawn objects that are StrategicLandmarks.
        for (int i = 0; i < sectorData.Objects.Count; i++)
        {
            GalaxyObjectData obj = sectorData.Objects[i];

            // Skip TacticalNetworked (ships, stations) and BackgroundVisual objects.
            if (obj.VisibilityCategory != SectorVisibilityCategory.StrategicLandmark)
            {
                continue;
            }

            // Spawn this strategic landmark through the visual factory.
            visualFactory.SpawnVisual(obj);
        }

        // Mark this sector as partially active.
        partiallyActiveSectors.Add(key);

        // Log that the sector is now partially active.
        Debug.Log("SectorActivationManager: Partially activated sector " + key);
    }

    // Deactivates the sector identified by the given key.
    // Despawns all currently spawned GameObjects in that sector.
    // key: a "x,y" string identifying the sector.
    private void DeactivateSector(string key)
    {
        // Find the GalaxySectorData for this key.
        GalaxySectorData sectorData = FindSectorData(key);
        if (sectorData == null)
        {
            // No data to despawn; just remove the tracking records.
            fullyActiveSectors.Remove(key);
            partiallyActiveSectors.Remove(key);
            return;
        }

        // Despawn each object in the sector.
        for (int i = 0; i < sectorData.Objects.Count; i++)
        {
            GalaxyObjectData obj = sectorData.Objects[i];

            // Tell the visual factory to remove the GameObject for this object.
            visualFactory.DespawnVisual(obj.Id);
        }

        // Remove the sector from both active tracking sets.
        fullyActiveSectors.Remove(key);
        partiallyActiveSectors.Remove(key);

        // Log that the sector has been deactivated.
        Debug.Log("SectorActivationManager: Deactivated sector " + key);
    }

    // Searches the sector lookup dictionary for the GalaxySectorData
    // that matches the given "x,y" key string.
    // Returns the matching GalaxySectorData, or null if not found.
    // key: a "x,y" string identifying the sector.
    private GalaxySectorData FindSectorData(string key)
    {
        // Use the pre-built dictionary for an O(1) lookup.
        GalaxySectorData sector;
        if (sectorLookup.TryGetValue(key, out sector))
        {
            return sector;
        }

        // No sector found for this key.
        return null;
    }

    // Builds a simple string key for a sector from its X and Y grid indices.
    // Used to identify sectors in dictionaries and sets without boxing struct keys.
    // x: sector X index.
    // y: sector Y index.
    // Returns a key in the format "x,y".
    private string MakeSectorKey(int x, int y)
    {
        return x + "," + y;
    }
}
