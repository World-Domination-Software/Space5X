# Editor To-Do List

This document describes how to wire up the generation scripts inside the Unity
Editor so the Big Bang galaxy system works correctly at runtime.

---

## Files Added

| Script | Purpose |
|--------|---------|
| `Assets/Scripts/Generation/BigBangGenerator.cs` | Procedurally generates all galaxy sectors, star systems, planets, homeworlds, Ancient Trader starports, and jump gates. All creation logic lives in this single file. |
| `Assets/Scripts/Generation/BigBangMapView.cs` | Visualizes the generated galaxy as a gizmo grid in the Scene view and as a toggleable map overlay in the Game view. |

---

## Setup Steps

### 1. Create the Generator GameObject

1. In the **Hierarchy** window click **+** → **Create Empty**.
2. Rename the new GameObject to `BigBangGenerator`.
3. With the object selected, click **Add Component** in the Inspector and search for **BigBangGenerator**.
4. Adjust the **Config** fields in the Inspector as needed:
   - `Map Width` / `Map Height` — number of sectors along each axis (default 500 × 500).
   - `Sector Size Units` — size of one sector in world units (default 100 000).
   - `Seed` — integer seed for reproducible generation.
   - `Max Black Holes` — cap on the total number of black-hole sectors.
   - `Ancient Trader Port Count` — how many Ancient Trader starports to scatter across the galaxy.
   - `Core / Mid / Outer Star System Chance` — probability (0–1) that a sector in each band becomes a star system.
   - `Core / Mid / Outer Asteroid Field Chance` — probability (0–1) that a sector becomes an asteroid field.
   - `Core / Mid / Outer Black Hole Chance` — probability (0–1) that a sector becomes a black hole (capped by Max Black Holes).
   - `Playable Species Count` — number of homeworld star systems to place.

> The generator runs automatically on Start and prints a statistics summary to the Unity Console.

---

### 2. Add the Map View (Scene + Game view visualization)

1. You can attach **BigBangMapView** to the same `BigBangGenerator` GameObject, or to a dedicated empty object named `BigBangMapView`.
2. Click **Add Component** and search for **BigBangMapView**.
3. Drag the **BigBangGenerator** object from the Hierarchy into the **Generator** field.
4. Configure the view options:
   - `Gizmo Scale` — controls the size of the per-sector cubes drawn in the Scene view.
   - `Gizmo Map Offset` — shifts the gizmo grid so it does not overlap other scene objects.
   - `Toggle Key` — the keyboard key that shows/hides the map during Play mode (default **M**).
   - `Mini Map Size Pixels` / `Mini Map Offset` — size and position of the fallback mini-map drawn in the Game view when no Canvas is assigned.

---

### 3. (Optional) Set Up a Full-Screen Canvas Map

For a nicer in-game map overlay instead of the small fallback mini-map:

1. In the Hierarchy click **+** → **UI** → **Canvas**.
   - Set the Canvas **Render Mode** to *Screen Space - Camera* or *Screen Space - Overlay*.
2. Right-click the Canvas in the Hierarchy and choose **UI** → **Raw Image**.
   - Rename it `MapImage`.
   - In the Rect Transform, anchor it to **Stretch – Stretch** so it fills the screen.
3. Select the GameObject that has **BigBangMapView** attached.
4. Drag the **Canvas** GameObject into the **Map Canvas** field.
5. Drag the **MapImage** Raw Image into the **Map Image** field.

With those fields filled in, pressing the toggle key during Play mode will show the full-screen texture map instead of the small OnGUI mini-map.

---

### 4. Verify in Play Mode

1. Press **Play**.
2. Check the Unity **Console** for the Big Bang stats summary — it lists total sectors, star systems, homeworlds, Ancient Trader ports, and a breakdown of planet counts.
3. Press **M** (or your chosen toggle key) to open the map overlay and confirm the galaxy looks correct.
4. In the **Scene** view (with the game running or stopped) you should see colored cubes representing the galaxy grid. Colors:
   - **Green** — normal star system
   - **Cyan** — homeworld star system
   - **Red** — Ancient Trader starport system
   - **Yellow** — asteroid field
   - **Purple** — black hole
   - **Dark gray** — empty space

---

### 5. Future Integration Notes

The `BigBangGenerator` currently runs as a standalone MonoBehaviour that generates the entire galaxy on `Start`. When integrating with the rest of the game:

- Call `generator.GenerateGalaxy()` from your game-manager or lobby script instead of relying on `Start` so you control when generation happens (e.g., after a seed is chosen in a lobby).
- The public `generator.Sectors` list is the single source of truth for all galaxy data. Other systems (sector activation, pathfinding, rendering) should read from that list rather than generating their own data.
- If the project adds a `SectorActivationManager` or visual factory, pass sectors from `generator.Sectors` into those systems rather than regenerating.
