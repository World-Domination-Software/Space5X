# Space5X Galaxy Refactor – Sector-Based World + FishNet Observability

## Objective
Refactor the entire galaxy system to use:
- Sector-based coordinates (NOT large world positions)
- Data-driven galaxy storage
- Client-side floating origin (per-player sector origin)
- FishNet observer-based visibility
- Dynamic spawning/despawning of sector content

The galaxy must NOT exist as a single large Unity scene with all objects active.

---

# 1. Core Data Model

## GalaxyPosition
All objects MUST use sector-based addressing:

```csharp
public struct GalaxyPosition
{
    public int SectorX;
    public int SectorY;
    public Vector3 LocalPosition;
}
```

Rules:
- Sector identifies location in galaxy grid
- LocalPosition is position INSIDE the sector
- NEVER rely on giant world coordinates

---

## GalaxyObjectData
Represents ALL objects in the galaxy (data layer, not GameObjects):

```csharp
public class GalaxyObjectData
{
    public string Id;
    public GalaxyPosition Position;
    public SectorVisibilityCategory VisibilityCategory;
    public GalaxyObjectType ObjectType;
}
```

---

## GalaxySectorData
```csharp
public class GalaxySectorData
{
    public int SectorX;
    public int SectorY;
    public List<GalaxyObjectData> Objects;
}
```

---

# 2. Sector Object Classification System (REQUIRED)

All objects MUST define a visibility category:

```csharp
public enum SectorVisibilityCategory
{
    TacticalNetworked,     // Ships, stations, combat objects
    StrategicLandmark,     // Stars, main planets, black holes
    BackgroundVisual       // Optional visuals (nebula, debris)
}
```

Rules:
- TacticalNetworked → uses FishNet NetworkObject
- StrategicLandmark → usually client-side visual only
- BackgroundVisual → always client-only

---

# 3. Object Type System

```csharp
public enum GalaxyObjectType
{
    Star,
    Planet,
    Station,
    Ship,
    AsteroidField,
    BlackHole,
    JumpGate
}
```

---

# 4. BigBang Refactor (Galaxy Generation)

BigBang MUST:
- Generate GalaxySectorData
- Populate GalaxyObjectData
- NOT spawn all objects into Unity scene

Each sector stores:
- star system (if exists)
- planets
- stations
- NPC fleets (as data only initially)

---

# 5. Sector Activation System

Create:
SectorActivationManager

Rules:

## Full Activation (Player Sector)
Spawn:
- ships (NetworkObject)
- stations/ports (NetworkObject)
- active planets (if gameplay relevant)
- combat objects

## Partial Activation (Neighbor Sectors)
Spawn ONLY:
- StrategicLandmark objects (stars, major planets, black holes)
- NO ships
- NO combat
- NO small objects

## Inactive Sectors
- NO GameObjects
- Data-only simulation

---

# 6. Visual Factory System (REQUIRED)

Create:
SectorVisualFactory

Purpose:
- Spawn NON-networked visuals from GalaxyObjectData
- Used for:
  - stars
  - planets
  - black holes
  - asteroid fields

Rules:
- Must NOT use NetworkObject
- Must be client-side only
- Must support spawn + despawn

Example:
```csharp
SpawnVisual(GalaxyObjectData data)
DespawnVisual(string objectId)
```

---

# 7. Network Object Rules

Use FishNet NetworkObject ONLY for:
- Ships (player + NPC)
- Stations / ports
- Combat objects
- Dockable / interactable entities

Do NOT use NetworkObject for:
- stars
- distant planets
- background visuals

---

# 8. Floating Origin (Client-Side)

Create:
ClientSectorOrigin

Responsibilities:
- Track current player sector
- Rebase ALL rendered positions

Formula:
```csharp
RenderedPosition =
((ObjectSector - PlayerSector) * SectorSize)
+ ObjectLocalPosition
```

Rules:
- Player's current sector = local origin (0,0,0 area)
- Prevent large coordinate precision issues
- DO NOT modify server authoritative data

---

# 9. Border Crossing System (CRITICAL)

Create:
SectorTransitionManager

Responsibilities:
- Detect when player crosses sector boundary
- Update SectorX / SectorY
- Recalculate LocalPosition

### MUST HANDLE:
- Player ships
- NPC ships
- Orbiting ships
- Planets with orbiting entities

### Behavior:
When crossing sector:
1. Update player sector
2. Convert position into new local sector coordinates
3. Trigger sector activation update
4. Trigger observer rebuild
5. Update client floating origin

---

# 10. FishNet Observability System

Create custom observer logic:

Rules:

## Same Sector
- Full visibility
- All TacticalNetworked objects visible

## Neighbor Sectors
- ONLY StrategicLandmark visible
- NO ships or combat

## Other Sectors
- NOT visible

Must be implemented using:
- Custom observer condition OR observer manager override

---

# 11. Player Spawn Flow

When player spawns:

1. Assign starting sector
2. Set LocalPosition
3. Initialize ClientSectorOrigin
4. Activate current sector
5. Activate neighbor sector landmarks
6. Spawn required NetworkObjects
7. Spawn visuals via SectorVisualFactory
8. Apply observer rules

---

# 12. Sector Simulation Tiers

Implement or stub:

- Active sectors → full simulation
- Neighbor sectors → strategic simulation
- Distant sectors → low-frequency simulation

Examples:
- economy ticks
- fleet movement timers
- mining
- ownership

---

# 13. Prefab Rules

## Network Prefabs
- Ship
- Station
- Combat objects

## Visual Prefabs (Factory)
- Star
- Planet
- Black hole
- Asteroid field

---

# 14. DO NOT DO

- Do NOT place entire galaxy as GameObjects in scene
- Do NOT use giant world coordinates
- Do NOT make all objects NetworkObjects
- Do NOT rely on scene-based visibility

---

# 15. Deliverables

Copilot must implement or stub:

- GalaxyPosition
- GalaxyObjectData
- GalaxySectorData
- SectorVisibilityCategory
- GalaxyObjectType
- SectorActivationManager
- SectorVisualFactory
- ClientSectorOrigin
- SectorTransitionManager
- FishNet observer condition
- Player spawn sector initialization
- Sector-based spawning/despawning
- Updated documentation

---

# 16. Summary

The galaxy is:
- Data-driven
- Sector-based
- Dynamically activated
- Locally rendered per player
- Network-visible only where relevant

This replaces:
"One giant scene with everything loaded"

With:
"Only the player's local slice of the galaxy exists at runtime"
