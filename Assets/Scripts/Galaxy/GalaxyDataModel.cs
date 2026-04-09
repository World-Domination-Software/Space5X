using System;
using System.Collections.Generic;
using UnityEngine;

// --- Enums ---

// Defines which visibility category a galaxy object belongs to.
// This controls whether the object uses FishNet NetworkObject replication
// or is spawned as a client-only visual.
public enum SectorVisibilityCategory
{
    // Ships, stations, ports, and any combat or dockable object.
    // These MUST be spawned as FishNet NetworkObjects.
    TacticalNetworked,

    // Stars, major planets, and black holes.
    // These are visible from the same sector and adjacent sectors
    // as client-side visuals only (no NetworkObject needed).
    StrategicLandmark,

    // Background decorative objects such as nebula clouds or debris.
    // Always client-only and never replicated over the network.
    BackgroundVisual
}

// Lists every type of object that can exist in the galaxy.
// Used so we know what prefab or mesh to spawn for a GalaxyObjectData entry.
public enum GalaxyObjectType
{
    // A star at the center of a sector's star system.
    Star,

    // A planet in orbit around a star.
    Planet,

    // A starport or docking station.
    Station,

    // A player-controlled or NPC ship.
    Ship,

    // An asteroid field sector with mineable content.
    AsteroidField,

    // A rare, hazardous black hole.
    BlackHole,

    // A jump gate used for long-range warp travel.
    JumpGate
}

// --- Structs ---

// Describes the exact location of any object in the galaxy.
// Uses sector coordinates to avoid giant floating-point world positions.
// The star in a sector is always at LocalPosition (0, 0, 0).
[Serializable]
public struct GalaxyPosition
{
    // X index of the sector in the galaxy grid that this object lives in.
    public int SectorX;

    // Y index of the sector in the galaxy grid that this object lives in.
    public int SectorY;

    // Position of this object INSIDE its sector, in world units.
    // For a star this is always (0, 0, 0). For a planet it is the orbital position.
    public Vector3 LocalPosition;
}

// --- Classes ---

// Represents a single object in the galaxy at the pure data layer.
// There is NO GameObject attached to this; it is just data describing
// what exists and where. GameObjects are created on demand by the
// SectorActivationManager or SectorVisualFactory when a sector becomes active.
[Serializable]
public class GalaxyObjectData
{
    // A unique identifier for this object, e.g. "sector_12_34_star" or "sector_12_34_planet_0".
    // Used to look up and remove the matching GameObject when a sector is deactivated.
    public string Id;

    // Sector and local position of this object in the galaxy.
    public GalaxyPosition Position;

    // Determines whether this object uses a NetworkObject, a visual prefab, or a
    // background-only visual. Drives all spawning and observer decisions.
    public SectorVisibilityCategory VisibilityCategory;

    // What kind of object this is. Used to pick the correct prefab when spawning.
    public GalaxyObjectType ObjectType;
}

// Stores all GalaxyObjectData entries that belong to a single square sector.
// This is the main unit of data-driven galaxy storage; no GameObjects here.
[Serializable]
public class GalaxySectorData
{
    // X index of this sector in the galaxy grid.
    public int SectorX;

    // Y index of this sector in the galaxy grid.
    public int SectorY;

    // All objects that exist in this sector as pure data.
    // GameObjects are only created when the SectorActivationManager activates this sector.
    public List<GalaxyObjectData> Objects = new List<GalaxyObjectData>();
}
