using System;
using System.Collections.Generic;
using UnityEngine;

// Represents the possible content types for a single hex sector.
public enum SectorContentType
{
    // Sector has nothing special in it.
    Empty,
    // Sector contains a single black hole.
    BlackHole,
    // Sector contains an asteroid field.
    AsteroidField,
    // Sector contains a star system with zero to seven planets.
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
    // Local position of the planet inside the hex sector in world units.
    public Vector2 localPosition;
}

// Simple data class to represent a star system inside a sector.
[Serializable]
public class StarSystemData
{
    // List of planets that belong to this star system.
    public List<PlanetData> planets = new List<PlanetData>();

    // True if this star system is a homeworld for a species.
    public bool isHomeworld;
}

// Simple data class to represent a jump gate near a port.
[Serializable]
public class JumpGateData
{
    // Local position of the jump gate inside the hex sector in world units.
    public Vector2 localPosition;
}

// Simple data class to represent a starport in orbit around a planet.
[Serializable]
public class StarportData
{
    // Local position of the starport inside the hex sector in world units.
    public Vector2 localPosition;

    // True if this port belongs to the Ancient Traders faction.
    public bool isAncientTraderPort;
}

// Represents all data for a single hex sector.
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

    // Size of each hex sector in world units.
    public float hexSizeUnits = 100000f;

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

        // Generate all sector data for the map.
        GenerateAllSectors();

        // Assign homeworlds for each playable species in the core or mid bands.
        AssignHomeworlds();

        // Create Ancient Trader starports and jump gates.
        CreateAncientTraderPortsAndGates();

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

    // Creates a star system with zero to seven planets for the given sector.
    private void GenerateStarSystemForSector(SectorData sector)
    {
        // Create a new star system data object.
        StarSystemData starSystem = new StarSystemData();

        // Roll how many planets this star system will have.
        int planetCount = random.Next(0, 8);

        // Loop once for each planet that should be created.
        for (int i = 0; i < planetCount; i++)
        {
            // Create a new planet data object.
            PlanetData planet = new PlanetData();

            // Choose a random local X position inside the hex using the configured hex size.
            float localX = ((float)random.NextDouble() - 0.5f) * config.hexSizeUnits;

            // Choose a random local Y position inside the hex using the configured hex size.
            float localY = ((float)random.NextDouble() - 0.5f) * config.hexSizeUnits;

            // Assign the local position to the planet.
            planet.localPosition = new Vector2(localX, localY);

            // Add the planet to the star system planet list.
            starSystem.planets.Add(planet);
        }

        // Assign the completed star system to the sector.
        sector.starSystem = starSystem;
    }

    // Assigns a homeworld for each playable species in a core or mid band star system.
    private void AssignHomeworlds()
    {
        // Create a list to store all candidate sectors that can be homeworlds.
        List<SectorData> candidates = new List<SectorData>();

        // Loop through all sectors in the galaxy.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Check that the sector is in the core or mid band.
            bool isCoreOrMid = sector.band == GalaxyBand.Core || sector.band == GalaxyBand.Mid;

            // Check that the sector has a star system with at least one planet.
            bool hasStarSystemWithPlanets = sector.contentType == SectorContentType.StarSystem && sector.starSystem != null && sector.starSystem.planets.Count > 0;

            // If both conditions are true, this sector is a valid homeworld candidate.
            if (isCoreOrMid && hasStarSystemWithPlanets)
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

            // Create a starport for this homeworld.
            StarportData starport = new StarportData();

            // Mark that this starport does not belong to the Ancient Traders.
            starport.isAncientTraderPort = false;

            // If the star system has at least one planet, place the starport near the first planet.
            if (chosenSector.starSystem.planets.Count > 0)
            {
                // Read the first planet local position.
                Vector2 planetPosition = chosenSector.starSystem.planets[0].localPosition;

                // Offset the port slightly from the planet position.
                starport.localPosition = planetPosition + new Vector2(1000f, 0f);
            }
            else
            {
                // If there are no planets, place the starport at the center of the hex.
                starport.localPosition = Vector2.zero;
            }

            // Assign the starport to the sector.
            chosenSector.starport = starport;

            // Create a jump gate near the port.
            JumpGateData gate = new JumpGateData();

            // Place the gate slightly offset from the starport.
            gate.localPosition = starport.localPosition + new Vector2(1000f, 1000f);

            // Assign the gate to the sector.
            chosenSector.jumpGate = gate;
        }
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

            // Offset the port slightly from the planet position.
            starport.localPosition = planetPosition + new Vector2(1500f, 0f);

            // Assign the starport to the sector.
            chosenSector.starport = starport;

            // Create a jump gate near the port.
            JumpGateData gate = new JumpGateData();

            // Place the gate slightly offset from the starport.
            gate.localPosition = starport.localPosition + new Vector2(1500f, 1500f);

            // Assign the gate to the sector.
            chosenSector.jumpGate = gate;
        }
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

        // Track how many star systems have each possible planet count from zero to seven.
        int[] planetCountBuckets = new int[8];

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
                    else if (planetCount > 7)
                    {
                        planetCount = 7;
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
            "\nStar systems by planet count (0 to 7):" +
            "\n  0 planets: " + planetCountBuckets[0] +
            "\n  1 planet: " + planetCountBuckets[1] +
            "\n  2 planets: " + planetCountBuckets[2] +
            "\n  3 planets: " + planetCountBuckets[3] +
            "\n  4 planets: " + planetCountBuckets[4] +
            "\n  5 planets: " + planetCountBuckets[5] +
            "\n  6 planets: " + planetCountBuckets[6] +
            "\n  7+ planets (clamped): " + planetCountBuckets[7];

        // Write the summary to the Unity console so it is easy to read.
        Debug.Log(summary);
    }
}
