# Space5X – Game Design Document (GDD)

## 1. High Concept

Space5X is a **persistent, sector-based space strategy game** with:

- Direct control of one ship at a time.
- Additional ships and assets that follow orders and automation.
- A galaxy made of sectors, ports, bases, and trade routes.
- A focus on **logistics, fuel, mass, and movement**.

Two core systems define the game:

1. **Big Bang Galaxy Generation** – creates a believable galaxy with a dense core and sparse frontier.
2. **Fuel, Mass, Impulse, and Warp Travel** – moving more mass and moving faster costs more, and empty space is valuable for warp travel.

The game takes inspiration from **VGA Planets** (mass-aware fuel, movement) and **TradeWars 2002** (sector control, ports, lawful vs hostile space), but aims for **simpler and clearer rules** that are easy to understand and implement.

---

## 2. Core Design Pillars

1. **Sector-Based Strategy** – The galaxy is divided into sectors. Ownership, influence, routes, and conflict happen at the sector level.
2. **Embodied Command** – The player directly pilots one ship; other ships follow high-level orders and doctrines.
3. **Logistics Is Power** – Fuel, cargo, and mass matter. Larger fleets are powerful but slow and expensive to move.
4. **Empty Space Is Valuable** – An exotic field called **Dsek Flux** is stronger in emptier regions and powers warp travel.
5. **Persistent Social Warfare** – Players belong to species factions. Cooperation, politics, and fleet inheritance shape long-term play.

---

## 3. Galaxy Structure

### 3.1 Sector Grid

- The galaxy uses a **square grid** layout. Each sector is adjacent to up to four neighbors (up, down, left, right).
- Default map size: **500 × 500 sectors** (250,000 sectors total).
- Each sector represents a **100,000 × 100,000 unit square** in local space. The center of the sector is at local coordinates **(0, 0)**, and the sector extends **±50,000 units** in each direction.
- Inside a sector, the star is always placed at **(0, 0)**. Planets orbit the star in **8 concentric rings**, each spaced **5,000 units** apart, from **5,000 units** (orbit 1) out to **40,000 units** (orbit 8). Each planet is placed at a random angle in its orbit so systems look natural.
- Servers can use presets like:
   - Small: 200 × 200
   - Medium: 350 × 350
   - Standard: 500 × 500
   - Large: 750 × 750

### 3.2 Concentric Galaxy Bands

The galaxy is divided by **distance from the center** into three bands:

- **Core band**
  - Inner ~33% of radius.
  - Small part of the map area.
  - Very dense, politically important.

- **Mid band**
  - 33%–66% of radius.
  - Medium density.
  - Frontier civilization feel.

- **Outer band**
  - 66%–100% of radius.
  - Huge area, very sparse.
  - Dangerous but rich in Dsek Flux.

The bands mainly affect **where special features are allowed** and where homeworlds and infrastructure appear. We do **not** artificially force more stars into one band as a percentage; density differences should mostly emerge from geometry and later content rules.

- Core: politically important, more infrastructure and ports.
- Mid: main expansion layer, mix of infrastructure and frontier.
- Outer: lonely, risky, and rich in Dsek Flux.

### 3.3 Sector Contents

Every sector contains **exactly one main content type**:

- **Empty Space** – nothing special in this sector.
- **Black Hole** – very rare; at most **3 black holes** in the entire galaxy.
- **Asteroid Field** – rare; good for mining, low infrastructure.
- **Star System** – contains a star and **0–8 planets**.

The Big Bang generator rolls this content **per sector**. Bands may slightly influence the chances (for example, more asteroid fields or black holes in outer space), but there is no separate “total star count per band” weighting.

---

## 4. Big Bang Galaxy Generation

This is the **main world generation system**.

Goals:

- Configurable map size.
- Per-sector content rolls (empty, black hole, asteroid field, or star system).
- Clear but simple band rules (core / mid / outer) that affect **where** features can appear, not manual star counts per band.
- Natural-looking distributions without obvious grid lines.
- A denser-feeling inner galaxy that mostly emerges from geometry and placement rules.

### 4.1 Input Parameters (Server Config)

Core parameters:

- `mapWidth`, `mapHeight` – number of hex sectors in X and Y.
- `hexSizeUnits` – size of each hex in world units (default: 100,000).
- `seed` – random seed for reproducibility.
- `maxBlackHoles` – hard cap on black holes (default: 3).
- `ancientTraderPortCount` – number of Ancient Trader starports to spawn (default: 100).
- Per-band content chances (kept simple and tweakable):
   - `coreStarSystemChance`
   - `midStarSystemChance`
   - `outerStarSystemChance`
   - `coreAsteroidFieldChance`
   - `midAsteroidFieldChance`
   - `outerAsteroidFieldChance`
   - `coreBlackHoleChance`
   - `midBlackHoleChance`
   - `outerBlackHoleChance`

The sum of these chances per band does **not** need to be 1.0; whatever probability is left after black hole, asteroid field, and star system checks simply becomes **Empty Space**.

### 4.2 Generation Phases (High-Level Algorithm)

1. **Zone Assignment**
   - Classify each sector as Core, Mid, or Outer based on distance from center.

2. **Per-Sector Content Roll**
   - For each sector:
     - Use its band (Core / Mid / Outer) to choose the correct set of chances.
     - First, roll for **Black Hole**, respecting the global `maxBlackHoles` cap.
     - If no black hole, roll for **Asteroid Field**.
     - If no asteroid field, roll for **Star System**.
     - If none of the above happen, the sector is **Empty Space**.

3. **Star System Details**
   - For each sector that is a Star System:
     - The star is placed at **(0, 0)**, the center of the sector.
     - Up to **8 planets** are randomly distributed across **8 concentric orbits**, one planet per orbit. Orbit 1 is at **5,000 units** from the star; each additional orbit adds another 5,000 units, reaching **40,000 units** at orbit 8.
     - Each planet is placed at a **random angle** in its orbit so that star systems look natural rather than all aligned.
     - Assign basic planet types (can be refined later).

4. **Void Regions (Optional)**
   - Optionally pre-generate some void areas in mid and outer bands.
   - Mark those sectors as **forced Empty Space**.
   - Use them later as high-flux corridors or anomaly regions.

5. **Homeworld Selection**
   - For each playable species:
     - Find candidate **Star System** sectors in the **Core or Mid** bands that have a planet in **orbit 2, 3, or 4** (between 10,000 and 20,000 units from the star).
     - Randomly choose one system as that species’ **Homeworld**.
     - Randomly select one of the qualifying planets (in orbit 2, 3, or 4) as the **homeworld planet**.
     - Place a starting **port** and **warp gate** in orbit around that planet, each at a randomly chosen angle.

6. **Ancient Trader Ports and Gates**
   - After homeworlds are assigned:
     - Randomly choose **100 star systems** (that have at least one planet) across the galaxy.
     - For each chosen system:
       - Create an **Ancient Trader starport** in orbit around one planet.
       - Place a **nearby jump gate** in the same sector, positioned close to the port.

7. **Feature Pass**
   - For each sector and star system, assign:
     - anomalies,
     - hazard or field types,
     - resource hints (e.g., asteroid fields rich in ore),
     - early infrastructure hooks for ports, bases, and pylons.

### 4.3 Homeworld and Infrastructure Summary

- Each playable species has **exactly one homeworld system** chosen randomly from valid core/mid candidates.
- Homeworld systems are always in the **Core or Mid** bands, never in the Outer band.
- The homeworld planet is always in **orbit 2, 3, or 4** of its star (10,000–20,000 units from the star).
- Each homeworld system starts with:
  - one homeworld planet (in orbit 2, 3, or 4),
  - one starter port in orbit around the homeworld planet at a random angle,
  - one starter warp gate in orbit around the homeworld planet at a random angle.
- The **Ancient Traders** start with **100 starports**, each with a **nearby jump gate**, all in orbits around planets in existing star systems.

This keeps setup rules simple and explicit while still allowing variety between galaxies.

---

## 5. Movement, Fuel, and Dsek Flux

### 5.1 Movement Types

1. **Impulse Travel**
   - Local and short-range travel.
   - Used inside sectors and for adjacent-sector moves.
   - Consumes conventional fuel (**Helion**).

2. **Warp Travel**
   - Long-range sector-to-sector jumps.
   - Consumes **Dsek Flux** (warp resource).
   - Strongly affected by sector flux levels and moved mass.

This separation keeps a familiar fuel economy for daily operations and a strategic warp economy for fleet projection.

### 5.2 Helion – Conventional Fuel

- **Helion** is the main fuel for impulse drives, maneuvering, and auxiliary systems.
- It can be:
  - mined as gas or liquids,
  - refined into fuel cells,
  - bought and sold at ports.

Game shorthand:

- **Fuel = Helion**
- **Warp Resource = Dsek Flux**

### 5.3 Ship Speed States

Ships can be in several movement states:

- **Docked / Stationkeeping** – minimal or no fuel use.
- **Maneuver** – precise local motion; low fuel cost.
- **Cruise** – standard intra-sector travel; moderate fuel cost.
- **Impulse N (1–20)** – faster local/sector travel; higher fuel cost.
- **Warp** – long jumps using Dsek Flux.

### 5.4 Mass Model (Simplified)

Instead of raw tonnage, use **Mass Units (MU)** for clarity.

Examples:

- Light tug: 4 MU
- Fighter: 2 MU
- Frigate: 12 MU
- Cruiser: 30 MU
- Container pod: 8 MU
- Heavy ore load: +10 MU

A ship’s **EffectiveMass** is the sum of:

- Hull + armor + modules.
- Cargo and containers.
- Fuel and special loads.
- Any towed or linked ships.

### 5.5 Impulse Fuel Rule (Simple and Linear)

At impulse speed `N`, fuel use grows linearly with speed.

**Formula (conceptual):**

```text
HelionUsed = ceil(
    DistanceUnits × EffectiveMass × ImpulseSpeed × EngineEfficiency
)
```

Where:

- `DistanceUnits` – traveled distance in local units or sector hops.
- `EffectiveMass` – ship mass in MU.
- `ImpulseSpeed` – chosen speed (1–20).
- `EngineEfficiency` – per-ship modifier (e.g., 0.1 for basic engines).

This is **simple to understand**: going faster always costs proportionally more fuel.

### 5.6 Warp Travel Rule (Dsek Flux)

Warp jumps spend **Dsek Flux** instead of Helion for the main jump.

**Conceptual formula:**

```text
FluxUsed = ceil(
    JumpDistanceSectors × EffectiveMass × WarpDifficulty
)
```

Where `WarpDifficulty` combines:

- local sector flux penalty,
- route instability (hazards, anomalies),
- ship hull modifiers.

A jump is only allowed if:

```text
WarpCoreCharge >= FluxUsed
```

### 5.7 Sector Flux Levels

Each sector has a **FluxLevel** that affects warp:

- Deep outer void: low penalty (easier, cheaper warp).
- Sparse frontier: moderate penalty.
- Mid space: baseline.
- Dense core and busy ports: high penalty (harder, more expensive warp).

Result:

- Empty space is strategically valuable for staging and warp corridors.
- Dense civilized space is safe and rich in trade, but warp is less efficient.

### 5.8 Warp Core Reserve

Each player (or faction) has a shared **Warp Core Reserve**:

- Collector ships, bases, or pylons can harvest Dsek Flux from sectors over time.
- More moved mass and longer jumps consume more of the reserve.
- Multiple simultaneous jumps increase drain.

This keeps large fleets powerful but **slow to reposition**.

### 5.9 Gate-Assisted Warp

Static **warp gates** reduce warp cost and risk:

- **Benefits:** cheaper jumps, reliable routes, higher throughput.
- **Costs:** expensive to build, obvious strategic targets.

Gates create important trade lanes and military choke points.

---

## 6. Sector Navigation and Layers

- Each sector has up to **4 neighbors** (square grid adjacency: up, down, left, right).
- Movement happens on three layers:
  1. **Intra-sector** – free movement inside a loaded sector.
  2. **Adjacent-sector travel** – short moves using Helion.
  3. **Warp jumps** – long-range sector travel using Dsek Flux.

---

## 7. Species Factions

Playable species should feel different but stay mechanically manageable.

Initial list (can be trimmed during implementation):

- **Terrans** – balanced, good at trade and diplomacy.
- **Pirates** – raiders and smugglers, strong in ambush and black markets.
- **Lizards** – prefer green worlds; strong colonization and biomass extraction.
- **Triffids** – plant-like; biomass and living hull specializations.
- **Aquatics** – oceanic world specialists; water and gas industry.
- **Rock People** – mining and armor experts.
- **Insectoids** – swarm tactics, drones, and mass production.
- **Greys** – scanning, precision tech, and small specialist hulls.
- **Energy Beings** – flux harvesting, warp efficiency, electronic warfare.

Non-playable reference factions:

- **AI Faction** – server-controlled presence (events, pressure, or neutral activity).
- **Ancient Traders** – neutral trade and policing power.

To keep implementation simple, it is fine to **start with 2–3 playable species** plus the Ancient Traders, then expand later.

---

## 8. Ancient Traders and Lawful Space

The Ancient Traders act as a **lawful neutral** force:

- Maintain neutral trade ports and protected sectors.
- Offer bounties and contracts.
- Punish repeated piracy near protected space.
- Stabilize early-game trade routes and new-player zones.

Their systems include:

- Neutral trade ports with tariffs.
- Lawful vs hostile reputation.
- Patrols that respond to high crime.
- Simple rules that are easy to code (e.g., reputation thresholds for response).

---

## 9. Economy and Resources

Keep the economy **understandable and limited** at first.

### 9.1 Core Raw Resources (Suggested Starting Set)

Start with a small set, then expand if needed:

- Iron Ore
- Rare Metals
- Crystal
- Water / Ice
- Volatile Gas
- Radioactives
- Biomass
- Dsek Residue (warp-related material)

### 9.2 Core Refined Goods (Suggested Starting Set)

- Structural Alloys
- Electronics
- Helion Fuel Cells
- Flux Capacitors
- Weapon Components
- Hull Plating
- Industrial Parts

More exotic goods can be added later when basic systems are stable.

### 9.3 Race Market Biases (Simplified)

Each species can have simple buy/sell modifiers:

- Mining races pay less for ore, more for tech.
- Tech races pay less for electronics, more for raw materials.
- Trade-oriented races get small margin bonuses.

Keep the first implementation small and consistent.

---

## 10. Ports, Bases, and Pylons

### 10.1 Port Types (Simplified Set)

- **Civilian Port** – basic trade, docking, refuel, small repairs.
- **Industrial Port** – refining and production.
- **Military Port** – weapons, defense, basic doctrines.
- **Ancient Trade Port** – neutral, policed hub.

### 10.2 Base Types

- **Outpost** – cheap foothold, light defenses.
- **Forward Base** – resupply and staging.
- **Shipyard Base** – hull assembly and fitting.
- **Flux Harvester Base** – converts local Dsek Flux into warp charge.

### 10.3 Pylons (Small Structures)

- **Scan Pylon** – sensor coverage.
- **Navigation Beacon** – easier routing.
- **Flux Relay** – small boost to local flux harvesting or warp.

Start with a **small subset** and add more types only when needed.

---

## 11. Ships and Combat (Overview)

### 11.1 Ship Categories (Starter List)

- Shuttle / Tug
- Miner
- Surveyor / Explorer
- Hauler
- Frigate / Destroyer
- Cruiser
- Carrier (optional early on)
- Harvester

### 11.2 Starter Experience

New players start with:

- One small, flexible ship (e.g., light tug or equivalent).
- Basic cargo capability.
- Access to their home port and base.

Progression should feel like **growing from one ship to a small fleet**.

### 11.3 Weapons and Defense (Simplified)

Starter set:

- **Weapons:** Pulse Laser, Missile Rack, Railgun.
- **Defense:** Light Shield, Armor, Point Defense.
- **Utility:** Tractor Beam, Scan Jammer (later).

More systems (EMP, siege weapons, advanced ECM) can be added after basic combat works.

---

## 12. Player Roles (Optional Specialization)

Within a species, players can specialize. This is mainly a **social and organizational layer**, not a hard requirement.

Example roles:

- **Fleet Commander** – focuses on combat and fleets.
- **Trader** – runs ports and routes.
- **Industrialist** – extraction and production.
- **Explorer** – scouting and anomaly hunting.
- **Engineer** – ship and base optimization.

These roles help guide play but should not require complex systems to be fun.

---

## 13. Death, Fleet Inheritance, and Cannibalism

This is a **key social mechanic**.

Core idea:

- When a player is defeated, their ships can be **absorbed** by another same-species player in the region.

Benefits:

- Wars can snowball in an interesting way.
- Strong players can rise by conquering others.
- Faction momentum is preserved.

Safeguards (to keep it fair and simple):

- A **command capacity cap** limits how many ships one player can effectively control.
- Newly absorbed ships have **temporary penalties** (reduced effectiveness) to avoid instant power spikes.
- Optional “heir” setting lets players choose preferred inheritors.

---

## 14. Sector Runtime Architecture

The galaxy is **data-driven and dynamically activated**. No sector exists as a fully loaded Unity scene all at once.

### 14.1 Core Data Model

All galaxy objects use sector-based addressing (never giant world coordinates):

- **`GalaxyPosition`** – contains `SectorX`, `SectorY`, and `LocalPosition` (position inside the sector).
- **`GalaxyObjectData`** – a single object in the galaxy: `Id`, `GalaxyPosition`, `SectorVisibilityCategory`, and `GalaxyObjectType`.
- **`GalaxySectorData`** – all `GalaxyObjectData` entries that belong to one sector.

### 14.2 Visibility Categories

Every object must declare a visibility category:

| Category | Examples | Networking |
|---|---|---|
| `TacticalNetworked` | Ships, stations, jump gates | FishNet `NetworkObject` |
| `StrategicLandmark` | Stars, planets, black holes | Client-side visual only |
| `BackgroundVisual` | Nebula, debris | Client-only, never networked |

### 14.3 Sector Activation Tiers

The **`SectorActivationManager`** maintains three tiers:

- **Full Activation** (player's current sector) – all objects spawned including `TacticalNetworked` entities.
- **Partial Activation** (adjacent sectors) – only `StrategicLandmark` objects spawned as client visuals.
- **Inactive** (all other sectors) – no `GameObjects`; data-only simulation.

When the player moves to a new sector, the manager despawns outgoing sectors and activates incoming ones.

### 14.4 Visual Factory

**`SectorVisualFactory`** spawns and despawns non-networked `GameObjects` for `StrategicLandmark` and `BackgroundVisual` objects. It requires one prefab per `GalaxyObjectType` assigned in the Inspector.

### 14.5 Floating Origin

**`ClientSectorOrigin`** keeps the player's current sector at roughly world position `(0, 0, 0)` by rebasing all rendered positions:

```
RenderedPosition = ((ObjectSector - PlayerSector) * SectorSize) + ObjectLocalPosition
```

This prevents floating-point precision errors from accumulating across a 500 × 500 sector galaxy.

### 14.6 Sector Border Crossing

**`SectorTransitionManager`** detects when the player's ship leaves the current sector boundary (±50,000 units on X and Z), updates `SectorX`/`SectorY`, recalculates local position, and notifies `ClientSectorOrigin` and `SectorActivationManager`.

### 14.7 FishNet Observer Condition

**`SectorObserverCondition`** is a custom FishNet `ObserverCondition` ScriptableObject. The server uses it to decide which clients can see each `NetworkObject`:

- Same sector → fully visible.
- Adjacent sector → `StrategicLandmark` only (no ships or combat objects).
- Any other sector → not visible.

---

## 15. Networking and Persistence

### 15.1 Networking

- Use **FishNet** as the networking solution.
- Server is **authoritative**.
- Players load and simulate one main sector at a time.
- Other sectors run at lower fidelity using abstract or event-based simulation.

### 15.2 Persistence

- **SQLite** for single-player or small local games.
- **MySQL or similar** for larger, multi-user server deployments.

Key saved data includes:

- Players and species.
- Sectors, flux levels, and ownership.
- Star systems and planets.
- Ships, fleets, cargo, fuel levels.
- Ports, bases, pylons.
- Markets, transactions, and warp gates.
- Big Bang generator config and seed.

---

## 16. Phased Development Plan (Simplified)

1. **Phase 1 – Galaxy Skeleton**
   - Big Bang generator.
   - Sector bands (core, mid, outer).
   - Simple map visualization.
   - SQLite persistence for sectors and systems.

2. **Phase 2 – Local Space and Ships**
   - Load a sector scene.
   - Basic ship entity and simple movement.

3. **Phase 3 – Fuel and Mass**
   - Helion fuel tracking.
   - Mass units (MU).
   - Linear impulse fuel use.

4. **Phase 4 – Dsek Flux and Warp**
   - Sector flux levels.
   - Warp core reserve.
   - Basic warp jumps.

5. **Phase 5 – Economy and Ports**
   - Simple resources and refined goods.
   - Ports with buying/selling and basic pricing.

6. **Phase 6 – Bases and Infrastructure**
   - Bases, pylons, and simple influence rules.

7. **Phase 7 – Combat and Death**
   - Simple combat model.
   - Death and fleet inheritance.

8. **Phase 8 – Multiplayer**
   - FishNet-based server.
   - Sector replication and authority.

9. **Phase 9 – Ancient Traders and Law**
   - Neutral ports, patrols, and reputation.

10. **Phase 10 – Polishing and Expansion**
    - More factions, ships, modules, and economic depth.
    - Balancing fuel and flux numbers.

---

## 17. Guiding Principles

- **Clarity over complexity** – rules should be easy to explain and code.
- **Mass and distance matter** – big fleets are powerful but slow and expensive to move.
- **Empty space is valuable** – frontier and void regions are key for warp strategy.
- **Lawful vs frontier tension** – safe, profitable core vs dangerous, empowering outer space.
- **Teach through the code** – implementation should be readable for new programmers.
