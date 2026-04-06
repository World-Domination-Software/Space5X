# Space5X – Game Rules Reference

## Key Command Reference

| Command | Action |
|---------|--------|
| `W` / `S` | Throttle up / down (increase or decrease impulse speed) |
| `A` / `D` | Rotate ship left / right |
| `Q` / `E` | Strafe left / right (maneuver mode only) |
| `Shift` + direction | Boost (brief acceleration burst, consumes extra Helion) |
| `Space` | All-stop / emergency brake |
| `Tab` | Cycle selection between your fleet ships |
| `F` | Toggle tactical overlay for selected ship |
| `M` | Open galaxy / sector map |
| `J` | Open jump (warp) interface |
| `T` | Open trade / docking interface (at port or base) |
| `O` | Open orders panel for selected fleet ship |
| `I` | Open ship info / cargo screen |
| `Esc` | Close current panel / cancel current action |
| `1`–`5` | Quick-select fleet ships by slot |
| `P` | Open port / base management panel |
| `R` | Set waypoint or route for selected ship |
| `C` | Open communications / diplomacy panel |

---

## Ship Speeds and Fuel Usage

### Speed States

Ships can operate at the following speed levels:

| State | Description | Helion Cost |
|-------|-------------|-------------|
| Docked / Stationkeeping | Anchored at port or base; minimal fuel drain | None / minimal |
| Maneuver | Precise local motion (fine positioning, docking approach) | Low |
| Cruise | Standard intra-sector travel | Moderate |
| Impulse 1–20 | Faster local or short-range sector travel | Linear; higher speed = proportionally more fuel |
| Warp | Long-range sector-to-sector jump | Uses Dsek Flux, not Helion |

### Impulse Fuel Formula

```
HelionUsed = ceil(
    DistanceUnits × EffectiveMass × ImpulseSpeed × EngineEfficiency
)
```

- `DistanceUnits` – traveled distance in local units or sector hops.
- `EffectiveMass` – ship mass in Mass Units (MU); see Mass Model below.
- `ImpulseSpeed` – chosen speed level (1–20).
- `EngineEfficiency` – per-ship modifier (e.g., 0.1 for basic engines).

Going faster **always costs proportionally more fuel** (linear relationship).

### Tactical View and Snapshots

- **Tactical view is available for all ships in your own fleet** without any delay.
- Ships belonging to other players or factions must send a **snapshot** of their sensor view for it to be accessible locally.  
  - Snapshot transmission takes **1 second per sector** of distance.
  - This means remote intel is always slightly out of date — the farther away the ship, the older the snapshot.

---

## Jump Fuel Details (Dsek Flux and Warp)

### Warp Travel Formula

Long-range warp jumps consume **Dsek Flux** (not Helion).

```
FluxUsed = ceil(
    JumpDistanceSectors × EffectiveMass × WarpDifficulty
)
```

A jump is only allowed if:

```
WarpCoreCharge >= FluxUsed
```

### WarpDifficulty Factors

`WarpDifficulty` is a combined multiplier based on:

| Factor | Effect |
|--------|--------|
| **Sector FluxLevel** | Each sector has a flux availability level; dense/busy sectors have a higher penalty |
| **EffectiveMass** | More mass costs proportionally more flux per sector jumped |
| **Route instability** | Hazards, anomalies, or black holes along the route increase difficulty |
| **Ship hull modifiers** | Some hull types or engine upgrades reduce warp difficulty |

### Sector Flux Levels

Each sector carries a `FluxLevel` that directly affects warp cost and availability:

| Sector Type | Flux Level | Warp Cost |
|-------------|-----------|-----------|
| Deep outer void | Very high flux availability | Low penalty — easy and cheap to warp |
| Sparse frontier | High flux availability | Moderate penalty |
| Mid space | Baseline flux | Baseline cost |
| Dense core / busy ports | Low flux availability | High penalty — expensive and harder to warp |

> **Strategic note:** Empty space is valuable. Frontier and void regions are ideal for staging areas and warp corridors precisely because warp is cheapest there.

### Warp Core Reserve

- Each player (or faction) maintains a shared **Warp Core Reserve**.
- Collector ships, bases, and Flux Relay pylons harvest Dsek Flux from sectors over time.
- Larger fleets and longer jumps drain the reserve faster.
- Multiple simultaneous jumps increase total drain.
- Large fleets are powerful but **slow to reposition** because warp reserves refill gradually.

### Gate-Assisted Warp

Static **warp gates** reduce both warp cost and risk:

- **Benefits:** cheaper jumps, reliable routes, higher fleet throughput.
- **Costs:** expensive to build, easy to locate — obvious strategic targets.

Gates define important trade lanes and military choke points throughout the galaxy.

---

## Additional Rules

### Galaxy Structure

- The galaxy uses a **hex-style sector grid** (default 500 × 500, totalling 250,000 sectors).
- Each hex sector represents a **100,000 × 100,000 unit** area of local space.
- Galaxy is divided into three concentric bands:
  - **Core** (inner ~33%) – dense, politically important, warp is expensive.
  - **Mid** (33–66%) – frontier civilization, mixed infrastructure.
  - **Outer** (66–100%) – vast, sparse, dangerous, rich in Dsek Flux.

### Sector Contents

Every sector contains exactly one main content type:

- **Empty Space** – strategically valuable for warp staging.
- **Black Hole** – extremely rare; at most **3 per galaxy**; severe warp hazard.
- **Asteroid Field** – rare; good for mining, low infrastructure.
- **Star System** – contains a star and **0–7 planets**.

### Mass Model (Mass Units)

A ship's **EffectiveMass** is the sum of:

- Hull + armor + modules
- Cargo and containers
- Fuel and special loads
- Any towed or linked ships

Example reference masses:

| Ship / Cargo | Mass Units (MU) |
|--------------|-----------------|
| Fighter | 2 MU |
| Light tug | 4 MU |
| Container pod | 8 MU |
| Frigate | 12 MU |
| Cruiser | 30 MU |
| Heavy ore load | +10 MU |

### Navigation Layers

Movement operates on three distinct layers:

1. **Intra-sector** – free movement inside the currently loaded sector.
2. **Adjacent-sector travel** – short sector hops using Helion.
3. **Warp jumps** – long-range sector travel using Dsek Flux.

Each sector has up to **6 neighbors** (hex adjacency).

### Ports, Bases, and Infrastructure

**Port types:**

| Type | Function |
|------|----------|
| Civilian Port | Basic trade, docking, refuel, small repairs |
| Industrial Port | Refining and production |
| Military Port | Weapons, defense, doctrines |
| Ancient Trade Port | Neutral, policed hub (run by Ancient Traders) |

**Base types:**

| Type | Function |
|------|----------|
| Outpost | Cheap foothold, light defenses |
| Forward Base | Resupply and staging |
| Shipyard Base | Hull assembly and fitting |
| Flux Harvester Base | Converts local Dsek Flux into warp charge |

**Pylons (small structures):**

- **Scan Pylon** – extends sensor coverage.
- **Navigation Beacon** – aids routing.
- **Flux Relay** – small boost to local flux harvesting or warp range.

### Ancient Traders and Lawful Space

The Ancient Traders act as a **lawful neutral** force:

- Maintain **100 neutral trade ports**, each with a nearby jump gate, spread across the galaxy.
- Offer bounties and contracts.
- Punish repeated piracy near protected space.
- Stabilize early-game trade routes and new-player zones.
- Use simple reputation thresholds to decide patrol responses.

### Death, Fleet Inheritance, and Cannibalism

- When a player is defeated, their ships can be **absorbed** by another player of the same species in the region.
- A **command capacity cap** limits how many ships one player can effectively control at once.
- Newly absorbed ships carry **temporary penalties** (reduced effectiveness) to prevent instant power spikes.
- Players can set an optional **heir** preference to choose who inherits their fleet.

### Economy and Resources

**Core raw resources:** Iron Ore, Rare Metals, Crystal, Water/Ice, Volatile Gas, Radioactives, Biomass, Dsek Residue.

**Core refined goods:** Structural Alloys, Electronics, Helion Fuel Cells, Flux Capacitors, Weapon Components, Hull Plating, Industrial Parts.

**Race market biases (simplified):**
- Mining races pay less for ore, more for tech.
- Tech races pay less for electronics, more for raw materials.
- Trade-oriented races get small margin bonuses on all transactions.

### Guiding Principles

- **Clarity over complexity** – rules should be easy to explain and to code.
- **Mass and distance matter** – big fleets are powerful but slow and expensive to move.
- **Empty space is valuable** – frontier and void regions are key for warp strategy.
- **Lawful vs frontier tension** – safe, profitable core vs dangerous, empowering outer space.
- **Teach through the code** – all implementation should be readable for new programmers.

---

*Drawn from `Docs/GDD.md` and `Docs/Inspiration.md`. See those files for full design details.*
