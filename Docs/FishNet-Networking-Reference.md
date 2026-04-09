# FishNet Networking Deep Research Reference Package for Space5X

## Executive summary

FishNet’s documentation and API surface (GitBook + DocFX API reference) is mature enough in 2026 to support Space5X’s hard requirements:

- Dedicated Linux authoritative servers.
- High object counts via area-of-interest (AOI) filtering.
- Deterministic tick scheduling for sector simulation.
- Multiple transports (direct UDP, WebGL websockets, Steam/EOS, Unity Transport + Relay).

Space5X’s “single-sector client” premise maps cleanly onto FishNet’s observer system: if a client is not an observer of an object, the object won’t be active/spawned for that client, cutting bandwidth and client CPU. This is exactly the kind of AOI behavior we want for a huge persistent galaxy.

For responsive ship piloting with a firm anti-cheat stance, FishNet supports full server authority, plus client-side prediction with replicate/reconcile patterns and tick callbacks (TimeManager) when we need high responsiveness without trusting the client.

For fleet "cannibalism" and proximity-based control transfers, FishNet’s ownership model and PredictedOwner can give instant-feeling swaps while still requiring server validation.

Persistence should be implemented as a server-only "single-writer" pattern: game state changes flow from authoritative tick simulation into an append-only event log plus periodic snapshots in MySQL. MySQL/InnoDB’s ACID focus supports resilient server restarts and crash safety, while single-player SQLite can use WAL mode to improve read/write concurrency on one machine.

**Assumptions for Space5X:**

- Engine: Unity 2022 LTS or newer.
- Production hosting: dedicated Linux servers.
- Persistence: multiplayer uses MySQL (InnoDB assumed); single-player uses SQLite (WAL enabled).
- Networking library: FishNet (current public release line as of 2026).
- Matchmaking/relay strategy: configurable (Unity Relay, Steam P2P, EOS relay, or direct IP/port).

---

## FishNet documentation overview

This section is a short map of FishNet’s GitBook docs and API areas that matter most to Space5X.

### Overview

- **What Is FishNet?**
  - Feature list: prediction, AOI, scene management, transports, SyncTypes, etc.
  - Unity compatibility: 2021.3+ supported, 2020 and earlier not supported.
  - Performance: benchmarking scenarios and guidance.
- **Pro, Projects, and Support**
  - Pro-only features (lag compensation, code stripping, network LOD).
  - Paid projects (lobby/world templates).
- **Legal Restrictions**
  - No concurrent user caps; custom license; must review before shipping.

### Tutorials (quick-start)

- Installing FishNet.
- Getting connected (host/server/client flows).
- Preparing a player object.
- Moving a player around (simple movement + NetworkTransform).
- Spawning and despawning items.
- Basic SyncVar example (syncing a color).
- Camera setup.
- Testing with multiple editors.
- Simulating bad network connections.
- Building a dedicated server.

### Guides and features

- **Fundamentals and terminology**
  - Server, client, host; communication patterns.
- **Transports**
  - Overview of supported transports and where to use them.
- **Networked GameObjects and scripts**
  - NetworkObjects and NetworkBehaviour.
  - Spawning and despawning.
  - Predicted spawning.
  - Object pooling.
- **Network communication**
  - Remote Procedure Calls (ServerRpc, ObserversRpc, TargetRpc).
  - SyncTypes (SyncVar, SyncList, SyncDictionary, etc.).
  - Broadcasts.
- **Data serialization**
  - Custom serializers.
  - Interface and inheritance serializers.
- **Ownership**
  - Server-managed ownership.
  - Using ownership for reading values and controlling authority.
- **Area of Interest (AOI / Observer System)**
  - Modifying conditions.
  - Custom observer conditions ScriptableObjects.
- **Scene Management**
  - Scene events, scene data, loading/unloading.
  - Scene stacking and caching.
  - Scene visibility and persisting NetworkObjects.
  - Addressables support.
- **Prediction**
  - Client-side prediction concepts.
  - Configuring PredictionManager and TimeManager.
  - Replicate/reconcile patterns for controlled objects.
  - PredictionRigidbody and NetworkColliders.
- **Lag Compensation (Pro)**
  - State rewind, raycast, projectiles.
- **Managers and components**
  - NetworkManager, TimeManager, ServerManager, ClientManager, TransportManager, ObserverManager.
  - NetworkTransform, NetworkAnimator, NetworkObserver.

### Transports

FishNet has multiple transports, including:

- **Tugboat** – default UDP transport for most dedicated server cases.
- **Multipass** – multi-transport selection.
- **Bayou** – WebGL-focused transport (websocket/WSS).
- **FishyUnityTransport** – wrapper over Unity Transport (1.x/2.x) with Relay support.
- **FishySteamworks / FishyFacepunch** – Steam transports (P2P, IP hiding).
- **FishyEOS** – Epic Online Services (matchmaking/relay).
- **FishyRealtime** – Photon Realtime transport (requires Photon license).

---

## API highlights and Space5X patterns

This section summarizes the main FishNet features we will use and how they map onto Space5X.

### Connection setup and lifecycle

**What FishNet provides**

- NetworkManager coordinates client/server lifecycle.
- ServerManager and ClientManager have `StartConnection()` methods to begin server or client modes from code.
- TransportManager exposes the active transport, including runtime client address changes.
- TimeManager exposes tick callbacks (OnTick/OnPostTick) that we can use for deterministic server simulation.

**Space5X usage**

- Use a bootstrap scene with NetworkManager, TimeManager, and a small "NetworkBootstrap" script that starts dedicated server or host based on command-line or editor flags.
- Use TimeManager tick callbacks for the server’s authoritative sector simulation loop.

### RPCs and buffered state

**What FishNet provides**

- **ServerRpc** – client to server calls; usually restricted to owning client unless configured otherwise.
- **ObserversRpc** – server to all observing clients; can optionally buffer last call for late joiners.
- **TargetRpc** – server to one specific client.
- RPCs can be sent over reliable or unreliable channels.

**Space5X usage**

- Use ServerRpc for player actions: warp requests, trade actions, ownership transfer requests.
- Use ObserversRpc (with BufferLast where appropriate) for sector-level events that joiners must see: sector ownership changes, gate state changes.
- Use TargetRpc for private responses: trade quotes, personal notifications.

### SyncTypes and delta sync

**What FishNet provides**

- SyncTypes (SyncVar, SyncList, SyncDictionary, etc.) automatically replicate server-side changes to clients.
- SyncTypes send only changed data (deltas).
- SyncTypeSettings control send rate and channel.

**Space5X usage**

- Use SyncVars for small, frequently read values such as ship hull percentage, ship mass class, or gate open/closed state.
- Use SyncLists for small lists like visible modules or short market summaries when needed.
- Prefer RPCs for bursty events rather than SyncTypes.

### Serialization and spawn payloads

**What FishNet provides**

- Custom serializers for complex types.
- Spawn payload hooks for sending initial state when an object spawns.

**Space5X usage**

- Use spawn payloads for initial ship blueprint IDs, race ownership, or gate configuration.
- Keep payloads small and move large, persistent details into a separate data loading step.

### Ownership and fleet cannibalism

**What FishNet provides**

- Ownership is server-controlled, indicating which client can issue commands to an object.
- PredictedOwner allows a client to briefly "own" an object immediately, with server validation.

**Space5X usage**

- For control transfers, server checks adjacency/proximity and permissions and then transfers ownership to the new pilot.
- For fleet inheritance on player death, server transfers ownership of eligible ships to another player of the same race in the same sector, or leaves them as claimable salvage.

### AOI and sector-based visibility

**What FishNet provides**

- Observer system determines which clients see which objects.
- ObserverManager can apply default conditions.
- NetworkObserver supports per-object conditions.
- Custom ObserverCondition ScriptableObjects allow arbitrary visibility rules.

**Space5X usage**

- Use a custom `HexSectorCondition` ScriptableObject that compares a connection’s current sector to an object’s sector.
- Default rule: clients only observe objects in their current sector.
- Optional rule: allow limited visibility for adjacent sectors for long-range sensors.

---

## Prediction, reconciliation, and latency

**What FishNet provides**

- Client-side prediction and reconciliation patterns built around TimeManager ticks.
- PredictionManager settings for buffering, state interpolation, and reconciliation.
- PredictedRigidbodies and NetworkColliders for physics-aware prediction.

**Space5X usage**

- Use prediction only where we need tight responsiveness, such as first-person piloting or close combat.
- Do not use prediction for long-range orders or warp; those should remain server-authoritative transactions.

---

## Deployment, transports, and scaling

### Dedicated servers

- Unity dedicated server build target (Linux) is supported on 2021.3+.
- FishNet provides guidance for headless startup, logging setup, and dedicated server builds.

**Space5X notes**

- Use Linux Dedicated Server builds.
- Configure command-line arguments for port, address, and instance ID.
- Use server-side logging to capture tick rate, connection counts, and AOI statistics.

### Transport selection

**Recommended defaults**

- Dedicated Linux servers: Tugboat or FishyUnityTransport.
- WebGL clients: Bayou (or Unity Transport via FishyUnityTransport where applicable).
- Steam distribution: FishySteamworks or FishyFacepunch.
- Epic integration: FishyEOS.

**Multipass**

- Use Multipass when we need to support different transports on different platforms while sharing the same logic.

### Performance tuning

- AOI/Observer conditions should be the primary scaling lever.
- Tune SyncType send rates for low-priority UI data.
- Use object pooling for frequently spawned/removed entities (e.g., fighters, projectiles).
- Use network simulator tools early to test high-latency and packet-loss scenarios.

---

## Space5X network/world architecture

### High-level architecture

- Server simulates the full galaxy.
- Clients load and simulate only one sector at a time.
- Non-observed objects are never spawned locally on clients.

**Server responsibilities**

- Maintain the authoritative galaxy model (sectors, ships, ports, gates, etc.).
- Run a deterministic tick loop for sector simulations.
- Apply player commands and AI decisions.
- Persist state and append events into MySQL.

**Client responsibilities**

- Render current sector and its objects.
- Show galaxy map, but treat it as UI-only (not fully simulated).
- Send input and orders via ServerRpc.
- Use prediction only when required for responsiveness.

---

## Persistence design: MySQL and SQLite

### Principles

- The server is the only writer to the database.
- Multiplayer: one server process (or shard) per galaxy writes to MySQL.
- Single-player: one local server process writes to SQLite (WAL).

### Schema outline (MySQL)

Key tables:

- `races` – playable and NPC races.
- `players` – player accounts and race choices.
- `galaxies` – galaxy configurations and seeds.
- `sectors` – sector-level data (band, flux level, owner).
- `star_systems` – systems per sector.
- `planets` – planets per star system.
- `ships` – ship positions, owners, and hull state.
- `ship_modules` – per-ship module loadout.
- `cargos` – per-ship cargo content.
- `structures` – ports, bases, gates, pylons.
- `galaxy_events` – append-only event log.

### SQLite single-player

- Same schema, but possibly without `galaxy_id` if we only have one galaxy.
- Use WAL mode for better concurrent reads/writes.

---

## Repository and documentation layout recommendations

To help AI agents and developers find networking references quickly, we recommend:

- Keeping this file in `Docs/FishNet-Networking-Reference.md`.
- Keeping Space5X-specific networking architecture notes in `Docs` alongside the main GDD.
- Using the FishNet demo scenes and scripts under `Assets/FishNet/Demos` as concrete examples of:
  - NetworkManager configuration.
  - Observer conditions.
  - Prediction patterns.
  - Transport configuration.

---

## Integration checklist for Space5X

1. Install FishNet into the Unity project.
2. Add NetworkManager, TimeManager, and other core managers to the bootstrap scene.
3. Choose and configure the transport (Tugboat, FishyUnityTransport, Bayou, etc.).
4. Implement a simple ConnectionManager for starting host/server/client via code.
5. Implement a custom observer condition for sector-based AOI.
6. Implement a basic prediction controller for piloted ships if needed.
7. Add a server tick loop using TimeManager for deterministic simulation.
8. Integrate persistence writes into the server tick loop for MySQL/SQLite.

This document is intended as a high-level reference. When implementing features, always cross-check current FishNet docs and the demo scripts in `Assets/FishNet/Demos` for up-to-date API usage and patterns.
