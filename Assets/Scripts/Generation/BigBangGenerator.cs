using System;
using System.Collections.Generic;
using UnityEngine;

// Represents the possible content types for a single square sector.
public enum SectorContentType
{
    // Sector has nothing special in it.
    Empty,
    // Sector contains a single black hole.
    BlackHole,
    // Sector contains an asteroid field.
    AsteroidField,
    // Sector contains a star system with zero to eight planets.
    StarSystem
}

// Represents which band a sector belongs to in the galaxy.
public enum GalaxyBand
{
    // Inner radius region of the galaxy.
    Core,
    // Middle radius region of the galaxy.
    Mid,
    // Outer radius region of the galaxy.
    Outer
}

// Simple data class to represent a planet in a star system.
[Serializable]
public class PlanetData
{
    // Orbit index of this planet, where 0 is the innermost orbit at 5000 units
    // and 7 is the outermost orbit at 40000 units. Each step adds 5000 units.
    public int orbitIndex;

    // Angle in degrees at which this planet sits along its orbit.
    // A random angle gives each planet a natural-looking position.
    public float orbitAngleDegrees;

    // Local position of the planet inside the square sector in world units,
    // computed from the orbit index and angle.
    public Vector2 localPosition;

    // True if this planet is a homeworld for a playable species.
    public bool isHomeworld;
}

// Simple data class to represent a star system inside a sector.
[Serializable]
public class StarSystemData
{
    // Local position of the star inside the sector. The star is always at the
    // center of the sector, so this is always (0, 0).
    public Vector2 starLocalPosition;

    // List of planets that belong to this star system.
    public List<PlanetData> planets = new List<PlanetData>();

    // True if this star system is a homeworld for a species.
    public bool isHomeworld;
}

// Simple data class to represent a jump gate near a port.
[Serializable]
public class JumpGateData
{
    // Local position of the jump gate inside the square sector in world units.
    public Vector2 localPosition;
}

// Simple data class to represent a starport in orbit around a planet.
[Serializable]
public class StarportData
{
    // Local position of the starport inside the square sector in world units.
    public Vector2 localPosition;

    // True if this port belongs to the Ancient Traders faction.
    public bool isAncientTraderPort;
}

// Represents all data for a single square sector.
[Serializable]
public class SectorData
{
    // X index of this sector in the galaxy grid.
    public int x;

    // Y index of this sector in the galaxy grid.
    public int y;

    // Band classification for this sector (Core, Mid, or Outer).
    public GalaxyBand band;

    // Main content type of this sector.
    public SectorContentType contentType;

    // Star system data if this sector contains a star system, otherwise null.
    public StarSystemData starSystem;

    // Optional starport data in this sector (can be null if no port here).
    public StarportData starport;

    // Optional jump gate data in this sector (can be null if no gate here).
    public JumpGateData jumpGate;
}

// Configuration values for the Big Bang galaxy generator.
[Serializable]
public class BigBangConfig
{
    // Number of sectors along the X axis.
    public int mapWidth = 500;

    // Number of sectors along the Y axis.
    public int mapHeight = 500;

    // Size of each square sector in world units. Each sector spans this many units
    // on each side, centered at (0, 0) so local coordinates run from
    // -sectorSizeUnits/2 to +sectorSizeUnits/2 on both axes.
    public float sectorSizeUnits = 100000f;

    // Random seed used to make generation reproducible.
    public int seed = 1;

    // Maximum number of black holes allowed in the entire galaxy.
    public int maxBlackHoles = 3;

    // Number of Ancient Trader starports to create, each with a nearby jump gate.
    public int ancientTraderPortCount = 100;

    // Chance that a core-band sector becomes a star system.
    public float coreStarSystemChance = 0.25f;

    // Chance that a mid-band sector becomes a star system.
    public float midStarSystemChance = 0.25f;

    // Chance that an outer-band sector becomes a star system.
    public float outerStarSystemChance = 0.25f;

    // Chance that a core-band sector becomes an asteroid field.
    public float coreAsteroidFieldChance = 0.03f;

    // Chance that a mid-band sector becomes an asteroid field.
    public float midAsteroidFieldChance = 0.03f;

    // Chance that an outer-band sector becomes an asteroid field.
    public float outerAsteroidFieldChance = 0.03f;

    // Chance that a core-band sector becomes a black hole.
    public float coreBlackHoleChance = 0.001f;

    // Chance that a mid-band sector becomes a black hole.
    public float midBlackHoleChance = 0.001f;

    // Chance that an outer-band sector becomes a black hole.
    public float outerBlackHoleChance = 0.001f;

    // Number of playable species that need homeworlds.
    public int playableSpeciesCount = 3;
}

// MonoBehaviour helper that can generate a galaxy using the Big Bang rules.
// This script is kept very simple and focused on data generation only.
public class BigBangGenerator : MonoBehaviour
{
    // Configuration used for this generator instance.
    public BigBangConfig config = new BigBangConfig();

    // Internal random number generator used during generation.
    private System.Random random;

    // List of all generated sectors in the galaxy.
    private List<SectorData> sectors = new List<SectorData>();

    // Public accessor to read back the generated sectors after generation.
    public List<SectorData> Sectors
    {
        // Returns the internal list of sectors.
        get { return sectors; }
    }

    // List of all generated sectors converted into the new GalaxySectorData format.
    // This is the data-driven representation used by SectorActivationManager,
    // SectorVisualFactory, and the rest of the runtime sector systems.
    private List<GalaxySectorData> galaxySectors = new List<GalaxySectorData>();

    // Public accessor to read back the data-driven sector list after generation.
    public List<GalaxySectorData> GalaxySectors
    {
        // Returns the list of GalaxySectorData built from the generated SectorData.
        get { return galaxySectors; }
    }

    // Called by Unity when the scene starts playing. For now we
    // automatically generate a galaxy as soon as the game runs so
    // that the debug views and console stats have data to work with.
    private void Start()
    {
        // Generate the galaxy using the current configuration.
        GenerateGalaxy();
    }

    // Generates the entire galaxy using the current configuration values.
    public void GenerateGalaxy()
    {
        // Create a new random generator with the configured seed.
        random = new System.Random(config.seed);

        // Clear any existing sectors from a previous generation.
        sectors.Clear();

        // Clear any existing data-driven sector entries from a previous generation.
        galaxySectors.Clear();

        // Generate all sector data for the map.
        GenerateAllSectors();

        // Assign homeworlds for each playable species in the core or mid bands.
        AssignHomeworlds();

        // Create Ancient Trader starports and jump gates.
        CreateAncientTraderPortsAndGates();

        // Convert all generated SectorData into the new GalaxySectorData format
        // so the runtime sector systems can work with data-driven objects.
        BuildGalaxySectorData();

        // After generation is finished, write a summary of the results to the console.
        LogGalaxyStats();
    }

    // Generates all sectors and rolls their main content type.
    private void GenerateAllSectors()
    {
        // Loop through every X index in the galaxy grid.
        for (int x = 0; x < config.mapWidth; x++)
        {
            // Loop through every Y index in the galaxy grid.
            for (int y = 0; y < config.mapHeight; y++)
            {
                // Create a new sector data object.
                SectorData sector = new SectorData();

                // Store the X index of the sector.
                sector.x = x;

                // Store the Y index of the sector.
                sector.y = y;

                // Compute which band this sector is in.
                sector.band = GetBandForSector(x, y);

                // Roll and assign the content type for this sector.
                RollSectorContent(sector);

                // If the sector is a star system, generate its star system details.
                if (sector.contentType == SectorContentType.StarSystem)
                {
                    // Create planets for the star system.
                    GenerateStarSystemForSector(sector);
                }

                // Add the completed sector to the list of sectors.
                sectors.Add(sector);
            }
        }
    }

    // Determines which galaxy band a given sector belongs to based on its distance from the center.
    private GalaxyBand GetBandForSector(int x, int y)
    {
        // Compute the center X index of the map.
        float centerX = (config.mapWidth - 1) / 2f;

        // Compute the center Y index of the map.
        float centerY = (config.mapHeight - 1) / 2f;

        // Compute the delta X from the center.
        float dx = x - centerX;

        // Compute the delta Y from the center.
        float dy = y - centerY;

        // Compute the approximate radial distance using Pythagorean length.
        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        // Compute the maximum possible distance to any corner.
        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        // Compute the normalized radius between 0 and 1.
        float radius = distance / maxDistance;

        // Core is inner 0.0 to 0.33 radius.
        if (radius < 0.33f)
        {
            // Return that this sector is in the core band.
            return GalaxyBand.Core;
        }

        // Mid is 0.33 to 0.66 radius.
        if (radius < 0.66f)
        {
            // Return that this sector is in the mid band.
            return GalaxyBand.Mid;
        }

        // Outer is 0.66 to 1.0 radius.
        return GalaxyBand.Outer;
    }

    // Rolls and assigns the main content type for a sector.
    private void RollSectorContent(SectorData sector)
    {
        // Start with the empty content type as a default.
        sector.contentType = SectorContentType.Empty;

        // Track how many black holes have already been created.
        int currentBlackHoles = CountExistingBlackHoles();

        // If we still have room to create more black holes, we may roll for one.
        if (currentBlackHoles < config.maxBlackHoles)
        {
            // Compute the black hole chance for this sector based on its band.
            float blackHoleChance = GetBlackHoleChance(sector.band);

            // Roll a random value between 0 and 1.
            float roll = (float)random.NextDouble();

            // If the roll is below the black hole chance, we place a black hole here.
            if (roll < blackHoleChance)
            {
                // Assign the content type as a black hole.
                sector.contentType = SectorContentType.BlackHole;

                // Return early because this sector now has a final content type.
                return;
            }
        }

        // Compute the asteroid field chance for this sector based on its band.
        float asteroidChance = GetAsteroidFieldChance(sector.band);

        // Roll for an asteroid field.
        float asteroidRoll = (float)random.NextDouble();

        // If the roll is below the asteroid chance, we place an asteroid field here.
        if (asteroidRoll < asteroidChance)
        {
            // Assign the content type as an asteroid field.
            sector.contentType = SectorContentType.AsteroidField;

            // Return early because this sector now has a final content type.
            return;
        }

        // Compute the star system chance for this sector based on its band.
        float starChance = GetStarSystemChance(sector.band);

        // Roll for a star system.
        float starRoll = (float)random.NextDouble();

        // If the roll is below the star system chance, we place a star system here.
        if (starRoll < starChance)
        {
            // Assign the content type as a star system.
            sector.contentType = SectorContentType.StarSystem;

            // Return because we have finished assigning content for this sector.
            return;
        }

        // If we reach this point, the sector stays as empty space.
    }

    // Returns the black hole chance for the given galaxy band.
    private float GetBlackHoleChance(GalaxyBand band)
    {
        // If the band is the core band, return the core black hole chance.
        if (band == GalaxyBand.Core)
        {
            return config.coreBlackHoleChance;
        }

        // If the band is the mid band, return the mid black hole chance.
        if (band == GalaxyBand.Mid)
        {
            return config.midBlackHoleChance;
        }

        // Otherwise this is the outer band, so return the outer black hole chance.
        return config.outerBlackHoleChance;
    }

    // Returns the asteroid field chance for the given galaxy band.
    private float GetAsteroidFieldChance(GalaxyBand band)
    {
        // If the band is the core band, return the core asteroid field chance.
        if (band == GalaxyBand.Core)
        {
            return config.coreAsteroidFieldChance;
        }

        // If the band is the mid band, return the mid asteroid field chance.
        if (band == GalaxyBand.Mid)
        {
            return config.midAsteroidFieldChance;
        }

        // Otherwise this is the outer band, so return the outer asteroid field chance.
        return config.outerAsteroidFieldChance;
    }

    // Returns the star system chance for the given galaxy band.
    private float GetStarSystemChance(GalaxyBand band)
    {
        // If the band is the core band, return the core star system chance.
        if (band == GalaxyBand.Core)
        {
            return config.coreStarSystemChance;
        }

        // If the band is the mid band, return the mid star system chance.
        if (band == GalaxyBand.Mid)
        {
            return config.midStarSystemChance;
        }

        // Otherwise this is the outer band, so return the outer star system chance.
        return config.outerStarSystemChance;
    }

    // Counts how many black hole sectors already exist in the list.
    private int CountExistingBlackHoles()
    {
        // Start with a count of zero.
        int count = 0;

        // Loop through each sector in the list.
        for (int i = 0; i < sectors.Count; i++)
        {
            // If this sector contains a black hole, increase the count.
            if (sectors[i].contentType == SectorContentType.BlackHole)
            {
                count++;
            }
        }

        // Return the final count.
        return count;
    }

    // Creates a star system with zero to eight planets for the given sector.
    // The star is placed at the center (0, 0) of the sector. Each planet is
    // assigned to one of eight concentric orbits spaced 5000 units apart,
    // from 5000 units out to 40000 units away from the star. Planets are
    // placed at a random angle in their orbit so the system looks natural.
    private void GenerateStarSystemForSector(SectorData sector)
    {
        // Create a new star system data object.
        StarSystemData starSystem = new StarSystemData();

        // The star always sits at the center of the sector.
        starSystem.starLocalPosition = Vector2.zero;

        // There are eight orbit slots, one per concentric ring.
        const int totalOrbits = 8;

        // Each orbit ring is this many world units further from the star.
        const float orbitSpacingUnits = 5000f;

        // Build an array of orbit indices and shuffle it so that planet
        // assignments are not always biased toward the innermost orbits.
        int[] orbitSlots = new int[totalOrbits];
        for (int i = 0; i < totalOrbits; i++)
        {
            orbitSlots[i] = i;
        }

        // Fisher-Yates shuffle so each run produces a different orbit order.
        for (int i = totalOrbits - 1; i > 0; i--)
        {
            // Pick a random index at or before the current position.
            int j = random.Next(0, i + 1);

            // Swap the two elements.
            int temp = orbitSlots[i];
            orbitSlots[i] = orbitSlots[j];
            orbitSlots[j] = temp;
        }

        // Roll how many planets this star system will have (0 to 8 inclusive).
        int planetCount = random.Next(0, totalOrbits + 1);

        // Create one planet for each of the chosen orbit slots.
        for (int i = 0; i < planetCount; i++)
        {
            // Create a new planet data object.
            PlanetData planet = new PlanetData();

            // Assign this planet to the next shuffled orbit slot.
            planet.orbitIndex = orbitSlots[i];

            // Compute the orbital radius for this slot.
            // Orbit 0 is at 5000 units, orbit 7 is at 40000 units.
            float orbitRadius = (planet.orbitIndex + 1) * orbitSpacingUnits;

            // Choose a random angle so that planets appear at natural-looking
            // positions rather than all lining up in the same direction.
            float angleDegrees = (float)(random.NextDouble() * 360.0);
            planet.orbitAngleDegrees = angleDegrees;

            // Convert the angle to radians for the trigonometry functions.
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            // Compute the local position of the planet within the sector.
            planet.localPosition = new Vector2(
                Mathf.Cos(angleRadians) * orbitRadius,
                Mathf.Sin(angleRadians) * orbitRadius
            );

            // Add the planet to the star system.
            starSystem.planets.Add(planet);
        }

        // Assign the completed star system to the sector.
        sector.starSystem = starSystem;
    }

    // Assigns a homeworld for each playable species in a core or mid band star system.
    // The homeworld planet must be in orbit 2, 3, or 4 (orbit indices 1, 2, or 3).
    // A starport and jump gate are placed in orbit around the homeworld planet
    // at randomly chosen angles.
    private void AssignHomeworlds()
    {
        // Homeworld planets must sit in orbit 2, 3, or 4 (0-based indices 1, 2, or 3).
        const int minHomeworldOrbitIndex = 1;
        const int maxHomeworldOrbitIndex = 3;

        // Orbital radius used to place the port and jump gate around the homeworld planet.
        const float portOrbitRadius = 500f;

        // Create a list to store all candidate sectors that can be homeworlds.
        List<SectorData> candidates = new List<SectorData>();

        // Loop through all sectors in the galaxy.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Check that the sector is in the core or mid band.
            bool isCoreOrMid = sector.band == GalaxyBand.Core || sector.band == GalaxyBand.Mid;

            // Check that the sector has a planet in one of the valid homeworld orbits.
            bool hasValidPlanet = HasPlanetInOrbits(sector, minHomeworldOrbitIndex, maxHomeworldOrbitIndex);

            // If both conditions are true, this sector is a valid homeworld candidate.
            if (isCoreOrMid && hasValidPlanet)
            {
                // Add the sector to the list of candidates.
                candidates.Add(sector);
            }
        }

        // If there are no candidates, we cannot assign homeworlds.
        if (candidates.Count == 0)
        {
            // Simply return and leave homeworlds unassigned.
            return;
        }

        // Loop once for each playable species that needs a homeworld.
        for (int speciesIndex = 0; speciesIndex < config.playableSpeciesCount; speciesIndex++)
        {
            // If we run out of candidates, break out of the loop.
            if (candidates.Count == 0)
            {
                break;
            }

            // Pick a random index from the remaining candidates.
            int randomIndex = random.Next(0, candidates.Count);

            // Get the sector at the chosen index.
            SectorData chosenSector = candidates[randomIndex];

            // Remove the chosen sector from the candidate list so it will not be reused.
            candidates.RemoveAt(randomIndex);

            // Mark the star system as a homeworld.
            chosenSector.starSystem.isHomeworld = true;

            // Select one of the valid homeworld planets (orbit 2, 3, or 4).
            PlanetData homeworldPlanet = PickPlanetFromOrbits(
                chosenSector.starSystem,
                minHomeworldOrbitIndex,
                maxHomeworldOrbitIndex
            );

            // Mark the selected planet as the homeworld planet.
            if (homeworldPlanet != null)
            {
                homeworldPlanet.isHomeworld = true;
            }

            // Use the homeworld planet position as the anchor for the port and gate.
            Vector2 planetPosition = homeworldPlanet != null
                ? homeworldPlanet.localPosition
                : Vector2.zero;

            // Choose a random angle for the starport so it orbits the planet naturally.
            float portAngleDegrees = (float)(random.NextDouble() * 360.0);
            float portAngleRadians = portAngleDegrees * Mathf.Deg2Rad;

            // Create a starport for this homeworld.
            StarportData starport = new StarportData();

            // Mark that this starport does not belong to the Ancient Traders.
            starport.isAncientTraderPort = false;

            // Place the starport in orbit around the homeworld planet at the random angle.
            starport.localPosition = planetPosition + new Vector2(
                Mathf.Cos(portAngleRadians) * portOrbitRadius,
                Mathf.Sin(portAngleRadians) * portOrbitRadius
            );

            // Assign the starport to the sector.
            chosenSector.starport = starport;

            // Choose a different random angle for the jump gate.
            float gateAngleDegrees = (float)(random.NextDouble() * 360.0);
            float gateAngleRadians = gateAngleDegrees * Mathf.Deg2Rad;

            // Create a jump gate near the homeworld planet.
            JumpGateData gate = new JumpGateData();

            // Place the jump gate in orbit around the homeworld planet at its own random angle.
            gate.localPosition = planetPosition + new Vector2(
                Mathf.Cos(gateAngleRadians) * portOrbitRadius,
                Mathf.Sin(gateAngleRadians) * portOrbitRadius
            );

            // Assign the gate to the sector.
            chosenSector.jumpGate = gate;
        }
    }

    // Returns true if the sector contains a star system with at least one planet
    // whose orbit index falls within the given inclusive range.
    private bool HasPlanetInOrbits(SectorData sector, int minOrbitIndex, int maxOrbitIndex)
    {
        // The sector must be a star system with valid data.
        if (sector.contentType != SectorContentType.StarSystem || sector.starSystem == null)
        {
            return false;
        }

        // Check each planet to see if any sits in the valid orbit range.
        for (int i = 0; i < sector.starSystem.planets.Count; i++)
        {
            // Get the current planet.
            PlanetData planet = sector.starSystem.planets[i];

            // If the planet is within the orbit range, we found a valid planet.
            if (planet.orbitIndex >= minOrbitIndex && planet.orbitIndex <= maxOrbitIndex)
            {
                return true;
            }
        }

        // No valid planet was found in the orbit range.
        return false;
    }

    // Returns a randomly chosen planet from a star system whose orbit index
    // falls within the given inclusive range, or null if none exist.
    private PlanetData PickPlanetFromOrbits(StarSystemData starSystem, int minOrbitIndex, int maxOrbitIndex)
    {
        // Collect all planets that are in the valid orbit range.
        List<PlanetData> validPlanets = new List<PlanetData>();

        // Loop through each planet in the star system.
        for (int i = 0; i < starSystem.planets.Count; i++)
        {
            // Get the current planet.
            PlanetData planet = starSystem.planets[i];

            // Add the planet to the valid list if its orbit is in range.
            if (planet.orbitIndex >= minOrbitIndex && planet.orbitIndex <= maxOrbitIndex)
            {
                validPlanets.Add(planet);
            }
        }

        // If no valid planets were found, return null.
        if (validPlanets.Count == 0)
        {
            return null;
        }

        // Return a randomly chosen planet from the valid list.
        return validPlanets[random.Next(0, validPlanets.Count)];
    }

    // Creates Ancient Trader starports and jump gates in orbit around planets.
    private void CreateAncientTraderPortsAndGates()
    {
        // Create a list to store potential sectors where Ancient Trader ports can be placed.
        List<SectorData> traderCandidates = new List<SectorData>();

        // Loop through all sectors.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Check that the sector contains a star system with at least one planet.
            bool hasStarSystemWithPlanets = sector.contentType == SectorContentType.StarSystem && sector.starSystem != null && sector.starSystem.planets.Count > 0;

            // Check that the sector does not already have a starport.
            bool hasNoPort = sector.starport == null;

            // If both are true, this sector can host an Ancient Trader port.
            if (hasStarSystemWithPlanets && hasNoPort)
            {
                // Add the sector to the trader candidate list.
                traderCandidates.Add(sector);
            }
        }

        // If there are no candidate sectors, we cannot create any Ancient Trader ports.
        if (traderCandidates.Count == 0)
        {
            // Return early.
            return;
        }

        // Determine how many ports we will actually create, limited by how many candidates we have.
        int portsToCreate = Mathf.Min(config.ancientTraderPortCount, traderCandidates.Count);

        // Loop once for each port that we should create.
        for (int i = 0; i < portsToCreate; i++)
        {
            // If we have no more candidates, break the loop.
            if (traderCandidates.Count == 0)
            {
                break;
            }

            // Pick a random index from the remaining trader candidates.
            int randomIndex = random.Next(0, traderCandidates.Count);

            // Get the sector at the chosen index.
            SectorData chosenSector = traderCandidates[randomIndex];

            // Remove the chosen sector from the candidate list so it is not reused.
            traderCandidates.RemoveAt(randomIndex);

            // Create a new starport data object.
            StarportData starport = new StarportData();

            // Mark that this port belongs to the Ancient Traders.
            starport.isAncientTraderPort = true;

            // Use the first planet position to place the port in orbit.
            Vector2 planetPosition = chosenSector.starSystem.planets[0].localPosition;

            // Choose a random angle so the port orbits the planet naturally.
            float portAngleDegrees = (float)(random.NextDouble() * 360.0);
            float portAngleRadians = portAngleDegrees * Mathf.Deg2Rad;

            // Place the port at a small orbital radius around the planet.
            starport.localPosition = planetPosition + new Vector2(
                Mathf.Cos(portAngleRadians) * 1500f,
                Mathf.Sin(portAngleRadians) * 1500f
            );

            // Assign the starport to the sector.
            chosenSector.starport = starport;

            // Create a jump gate near the port.
            JumpGateData gate = new JumpGateData();

            // Choose a different random angle for the gate.
            float gateAngleDegrees = (float)(random.NextDouble() * 360.0);
            float gateAngleRadians = gateAngleDegrees * Mathf.Deg2Rad;

            // Place the gate in orbit around the same planet at its own random angle.
            gate.localPosition = planetPosition + new Vector2(
                Mathf.Cos(gateAngleRadians) * 1500f,
                Mathf.Sin(gateAngleRadians) * 1500f
            );

            // Assign the gate to the sector.
            chosenSector.jumpGate = gate;
        }
    }

    // Converts all generated SectorData entries into GalaxySectorData objects.
    // Each piece of content in a sector (star, planets, ports, gates, black holes,
    // asteroid fields) becomes a GalaxyObjectData entry with a unique ID,
    // a GalaxyPosition, a SectorVisibilityCategory, and a GalaxyObjectType.
    //
    // This method runs after AssignHomeworlds and CreateAncientTraderPortsAndGates
    // so that homeworld flags and Ancient Trader port flags are already set.
    private void BuildGalaxySectorData()
    {
        // Process every generated sector.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the raw sector data produced by the generator.
            SectorData sector = sectors[i];

            // Create a new data-driven sector object for the runtime systems.
            GalaxySectorData galaxySector = new GalaxySectorData();
            galaxySector.SectorX = sector.x;
            galaxySector.SectorY = sector.y;

            // Build a base prefix for object IDs in this sector.
            string sectorPrefix = "sector_" + sector.x + "_" + sector.y;

            // Process sector content types that produce galaxy objects.
            if (sector.contentType == SectorContentType.BlackHole)
            {
                // Create a GalaxyObjectData for the black hole.
                GalaxyObjectData blackHoleObj = new GalaxyObjectData();
                blackHoleObj.Id = sectorPrefix + "_blackhole";
                blackHoleObj.ObjectType = GalaxyObjectType.BlackHole;

                // Black holes are strategic landmarks visible from adjacent sectors.
                blackHoleObj.VisibilityCategory = SectorVisibilityCategory.StrategicLandmark;

                // Black holes sit at the center of the sector (local origin).
                blackHoleObj.Position = new GalaxyPosition
                {
                    SectorX = sector.x,
                    SectorY = sector.y,
                    LocalPosition = Vector3.zero
                };

                // Add the black hole to the sector's object list.
                galaxySector.Objects.Add(blackHoleObj);
            }
            else if (sector.contentType == SectorContentType.AsteroidField)
            {
                // Create a GalaxyObjectData for the asteroid field.
                GalaxyObjectData asteroidObj = new GalaxyObjectData();
                asteroidObj.Id = sectorPrefix + "_asteroidfield";
                asteroidObj.ObjectType = GalaxyObjectType.AsteroidField;

                // Asteroid fields are background visuals; they do not appear in adjacent sectors.
                asteroidObj.VisibilityCategory = SectorVisibilityCategory.BackgroundVisual;

                // Place the asteroid field at the center of the sector.
                asteroidObj.Position = new GalaxyPosition
                {
                    SectorX = sector.x,
                    SectorY = sector.y,
                    LocalPosition = Vector3.zero
                };

                // Add the asteroid field to the sector's object list.
                galaxySector.Objects.Add(asteroidObj);
            }
            else if (sector.contentType == SectorContentType.StarSystem && sector.starSystem != null)
            {
                // Create a GalaxyObjectData for the star at the center of the system.
                GalaxyObjectData starObj = new GalaxyObjectData();
                starObj.Id = sectorPrefix + "_star";
                starObj.ObjectType = GalaxyObjectType.Star;

                // Stars are strategic landmarks visible from adjacent sectors.
                starObj.VisibilityCategory = SectorVisibilityCategory.StrategicLandmark;

                // The star is always at the local origin (0, 0, 0) inside the sector.
                starObj.Position = new GalaxyPosition
                {
                    SectorX = sector.x,
                    SectorY = sector.y,
                    LocalPosition = Vector3.zero
                };

                // Add the star to the sector's object list.
                galaxySector.Objects.Add(starObj);

                // Create a GalaxyObjectData for each planet in the star system.
                for (int p = 0; p < sector.starSystem.planets.Count; p++)
                {
                    PlanetData planet = sector.starSystem.planets[p];

                    GalaxyObjectData planetObj = new GalaxyObjectData();
                    planetObj.Id = sectorPrefix + "_planet_" + p;
                    planetObj.ObjectType = GalaxyObjectType.Planet;

                    // All planets are strategic landmarks. They are visible from
                    // adjacent sectors as distant visual markers.
                    planetObj.VisibilityCategory = SectorVisibilityCategory.StrategicLandmark;

                    // Convert the planet's 2D local position to a 3D GalaxyPosition.
                    planetObj.Position = new GalaxyPosition
                    {
                        SectorX = sector.x,
                        SectorY = sector.y,
                        LocalPosition = LocalPosition2DTo3D(planet.localPosition)
                    };

                    // Add the planet to the sector's object list.
                    galaxySector.Objects.Add(planetObj);
                }

                // If the sector has a starport, create a GalaxyObjectData for it.
                if (sector.starport != null)
                {
                    GalaxyObjectData stationObj = new GalaxyObjectData();
                    stationObj.Id = sectorPrefix + "_station";
                    stationObj.ObjectType = GalaxyObjectType.Station;

                    // Stations and ports are tactical networked objects; they require a FishNet NetworkObject.
                    stationObj.VisibilityCategory = SectorVisibilityCategory.TacticalNetworked;

                    // Convert the starport's 2D local position to a 3D GalaxyPosition.
                    stationObj.Position = new GalaxyPosition
                    {
                        SectorX = sector.x,
                        SectorY = sector.y,
                        LocalPosition = LocalPosition2DTo3D(sector.starport.localPosition)
                    };

                    // Add the station to the sector's object list.
                    galaxySector.Objects.Add(stationObj);
                }

                // If the sector has a jump gate, create a GalaxyObjectData for it.
                if (sector.jumpGate != null)
                {
                    GalaxyObjectData gateObj = new GalaxyObjectData();
                    gateObj.Id = sectorPrefix + "_jumpgate";
                    gateObj.ObjectType = GalaxyObjectType.JumpGate;

                    // Jump gates are tactical networked objects; players interact with them via FishNet.
                    gateObj.VisibilityCategory = SectorVisibilityCategory.TacticalNetworked;

                    // Convert the jump gate's 2D local position to a 3D GalaxyPosition.
                    gateObj.Position = new GalaxyPosition
                    {
                        SectorX = sector.x,
                        SectorY = sector.y,
                        LocalPosition = LocalPosition2DTo3D(sector.jumpGate.localPosition)
                    };

                    // Add the jump gate to the sector's object list.
                    galaxySector.Objects.Add(gateObj);
                }
            }

            // Add the completed galaxy sector to the list used by the runtime systems.
            galaxySectors.Add(galaxySector);
        }

        // Log how many data-driven sector entries were built.
        Debug.Log("BigBangGenerator: Built GalaxySectorData for " + galaxySectors.Count + " sectors.");
    }

    // Converts a 2D local position (Vector2) from the generator into a 3D Vector3
    // used by GalaxyPosition. The generator works in 2D (X and Y axes) while Unity
    // uses X and Z for horizontal space. The Y component of the generator's position
    // maps to the Z axis in Unity, and height (Y in Unity) is set to zero.
    // pos2D: the 2D local position to convert.
    // Returns a Vector3 with X = pos2D.x, Y = 0, Z = pos2D.y.
    private Vector3 LocalPosition2DTo3D(Vector2 pos2D)
    {
        return new Vector3(pos2D.x, 0f, pos2D.y);
    }

    // Logs a simple summary of the generated galaxy to the Unity console.
    private void LogGalaxyStats()
    {
        // Count how many sectors exist in total.
        int totalSectors = sectors.Count;

        // Count how many sectors are empty.
        int emptyCount = 0;

        // Count how many sectors contain black holes.
        int blackHoleCount = 0;

        // Count how many sectors contain asteroid fields.
        int asteroidFieldCount = 0;

        // Count how many sectors contain star systems.
        int starSystemCount = 0;

        // Count how many star systems are marked as homeworlds.
        int homeworldCount = 0;

        // Count how many sectors contain Ancient Trader starports.
        int ancientTraderPortCount = 0;

        // Track how many star systems have each possible planet count from zero to eight.
        int[] planetCountBuckets = new int[9];

        // Loop through all sectors to update the counters.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Read the current sector.
            SectorData sector = sectors[i];

            // Increase counts based on the sector content type.
            if (sector.contentType == SectorContentType.Empty)
            {
                // Increase the empty sector count.
                emptyCount++;
            }
            else if (sector.contentType == SectorContentType.BlackHole)
            {
                // Increase the black hole sector count.
                blackHoleCount++;
            }
            else if (sector.contentType == SectorContentType.AsteroidField)
            {
                // Increase the asteroid field sector count.
                asteroidFieldCount++;
            }
            else if (sector.contentType == SectorContentType.StarSystem)
            {
                // Increase the star system sector count.
                starSystemCount++;

                // If the sector has valid star system data, inspect it further.
                if (sector.starSystem != null)
                {
                    // If this star system is a homeworld, increase the homeworld count.
                    if (sector.starSystem.isHomeworld)
                    {
                        homeworldCount++;
                    }

                    // Read the number of planets in this system.
                    int planetCount = sector.starSystem.planets.Count;

                    // Clamp the planet count to the valid range for our buckets.
                    if (planetCount < 0)
                    {
                        planetCount = 0;
                    }
                    else if (planetCount > 8)
                    {
                        planetCount = 8;
                    }

                    // Increase the bucket for the clamped planet count.
                    planetCountBuckets[planetCount]++;
                }

                // If this sector also has a starport marked as an Ancient Trader port, count it.
                if (sector.starport != null && sector.starport.isAncientTraderPort)
                {
                    ancientTraderPortCount++;
                }
            }
        }

        // Build a readable summary string for the Unity console.
        string summary =
            "Big Bang generation complete" +
            "\nTotal sectors: " + totalSectors +
            "\nEmpty sectors: " + emptyCount +
            "\nBlack holes: " + blackHoleCount +
            "\nAsteroid fields: " + asteroidFieldCount +
            "\nStar systems: " + starSystemCount +
            "\nHomeworld star systems: " + homeworldCount +
            "\nAncient Trader ports: " + ancientTraderPortCount +
            "\nStar systems by planet count (0 to 8):" +
            "\n  0 planets: " + planetCountBuckets[0] +
            "\n  1 planet: " + planetCountBuckets[1] +
            "\n  2 planets: " + planetCountBuckets[2] +
            "\n  3 planets: " + planetCountBuckets[3] +
            "\n  4 planets: " + planetCountBuckets[4] +
            "\n  5 planets: " + planetCountBuckets[5] +
            "\n  6 planets: " + planetCountBuckets[6] +
            "\n  7 planets: " + planetCountBuckets[7] +
            "\n  8 planets: " + planetCountBuckets[8];

        // Write the summary to the Unity console so it is easy to read.
        Debug.Log(summary);
    }
}
