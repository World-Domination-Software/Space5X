using System.Collections.Generic;
using UnityEngine;

// This component draws a simple debug view of the generated galaxy
// in the Unity Scene view using Gizmos. It is designed to be easy
// to understand for new programmers and works together with the
// BigBangGenerator component.
public class BigBangDebugView : MonoBehaviour
{
    // Reference to the BigBangGenerator that holds the sector data.
    public BigBangGenerator generator;

    // Size factor used to scale down the drawn gizmos so the map
    // fits inside the Scene view more easily.
    public float gizmoScale = 0.1f;

    // Offset added to all drawn positions so the map is centered
    // around the GameObject that owns this component.
    public Vector3 mapOffset = Vector3.zero;

    // True if we should draw the map gizmos while the game is running.
    // This is toggled by pressing the M key during play mode.
    private bool showMapGizmos = false;

    // Size of the mini-map square in screen pixels when drawing
    // the map in the Game view.
    public float miniMapSizePixels = 300f;

    // Screen-space offset for the mini-map in the Game view.
    // This controls where the mini-map rectangle starts.
    public Vector2 miniMapOffset = new Vector2(10f, 10f);

    // Called by Unity every frame. We use this to watch for the M key
    // during play mode and toggle whether gizmos are drawn.
    private void Update()
    {
        // Only react to key input while the application is playing.
        if (!Application.isPlaying)
        {
            // If the game is not running, do nothing here.
            return;
        }

        // If the player presses the M key down this frame, toggle the flag.
        if (Input.GetKeyDown(KeyCode.M))
        {
            // Flip the showMapGizmos value so pressing M turns it on and off.
            showMapGizmos = !showMapGizmos;
        }
    }

    // Called by Unity to draw gizmos in the Scene view.
    private void OnDrawGizmos()
    {
        // If the game is playing and the map gizmos are not enabled, do not draw.
        if (Application.isPlaying && !showMapGizmos)
        {
            // Return early so nothing is drawn when gizmos are hidden.
            return;
        }

        // If there is no generator assigned, there is nothing to draw.
        if (generator == null)
        {
            // Stop here so we do not try to access missing data.
            return;
        }

        // Read the list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If the sector list is empty, the galaxy probably has not been generated yet.
        if (sectors == null || sectors.Count == 0)
        {
            // Stop here to avoid drawing an empty map.
            return;
        }

        // Read configuration values from the generator for width and height.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Compute half-width and half-height to help center the drawing.
        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        // Loop through every sector and draw a small colored square for it.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Compute a simple 2D position for this sector based on its grid indices.
            float localX = (sector.x - halfWidth) * gizmoScale;
            float localZ = (sector.y - halfHeight) * gizmoScale;

            // Build a world position by adding the offset and using the owner's transform position.
            Vector3 worldPos = transform.position + mapOffset + new Vector3(localX, 0f, localZ);

            // Choose a color based on the sector content type.
            Gizmos.color = GetColorForSector(sector);

            // Draw a small cube at the sector position to represent the sector.
            Gizmos.DrawCube(worldPos, Vector3.one * gizmoScale * 0.9f);
        }
    }

    // Called by Unity to draw simple GUI elements in the Game view.
    // We use this to draw a small mini-map overlay when the map
    // is toggled on during play mode.
    private void OnGUI()
    {
        // Only draw the mini-map while the application is playing
        // and the user has toggled the map on.
        if (!Application.isPlaying || !showMapGizmos)
        {
            // If the game is not running or the map is hidden, do nothing.
            return;
        }

        // If there is no generator assigned, there is nothing to draw.
        if (generator == null)
        {
            // Stop here so we do not try to access missing data.
            return;
        }

        // Read the list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If the sector list is empty, the galaxy probably has not been generated yet.
        if (sectors == null || sectors.Count == 0)
        {
            // Stop here to avoid drawing an empty map.
            return;
        }

        // Read configuration values from the generator for width and height.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Decide how large each sector square should be in pixels.
        // We base this on the largest of width or height so the
        // entire map fits inside a square of miniMapSizePixels.
        float maxDimension = Mathf.Max(width, height);

        // Compute the pixel size of one sector cell in the mini-map.
        float cellSize = miniMapSizePixels / maxDimension;

        // Compute the starting screen position of the mini-map.
        float startX = miniMapOffset.x;
        float startY = miniMapOffset.y;

        // Loop through every sector and draw a small colored rectangle.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Compute the rectangle position in screen space.
            float x = startX + sector.x * cellSize;
            float y = startY + sector.y * cellSize;

            // Choose a color based on the sector content type.
            Color color = GetColorForSector(sector);

            // Apply the chosen color to the GUI system.
            Color previousColor = GUI.color;
            GUI.color = color;

            // Draw a filled rectangle for this sector using a 1x1 white texture.
            GUI.DrawTexture(new Rect(x, y, cellSize, cellSize), Texture2D.whiteTexture);

            // Restore the previous GUI color so we do not affect other GUI.
            GUI.color = previousColor;
        }
    }

    // Chooses a color to represent the given sector in the debug view.
    private Color GetColorForSector(SectorData sector)
    {
        // If the sector contains a black hole, draw it with a purple color.
        if (sector.contentType == SectorContentType.BlackHole)
        {
            // Return a purple color for black holes.
            return new Color(0.6f, 0.0f, 0.8f);
        }

        // If the sector contains an asteroid field, draw it with a yellow color.
        if (sector.contentType == SectorContentType.AsteroidField)
        {
            // Return a yellow color for asteroid fields.
            return Color.yellow;
        }

        // If the sector contains a star system, we may choose special colors
        // depending on whether it is a homeworld or has an Ancient Trader port.
        if (sector.contentType == SectorContentType.StarSystem)
        {
            // If this star system is marked as a homeworld, draw it as cyan.
            if (sector.starSystem != null && sector.starSystem.isHomeworld)
            {
                // Return a cyan color for homeworlds.
                return Color.cyan;
            }

            // If this sector has a starport that belongs to the Ancient Traders, draw it as red.
            if (sector.starport != null && sector.starport.isAncientTraderPort)
            {
                // Return a red color for Ancient Trader ports.
                return Color.red;
            }

            // For normal star systems, use a green color.
            return Color.green;
        }

        // For empty sectors, use a dark gray color to keep them subtle.
        return Color.gray * 0.6f;
    }
}
