using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This component provides two ways to visualize the generated galaxy map:
//
//   1. Scene-view Gizmos - When working in the Unity Editor you will see a grid
//      of colored cubes in the Scene window that represent every sector in the
//      galaxy. This is useful for a quick sanity-check during development.
//
//   2. Canvas overlay map - During Play mode you can press the toggle key
//      (default M) to show or hide a full-screen image of the galaxy. The image
//      is rendered into a Texture2D and assigned to a RawImage on a UI Canvas.
//      If no Canvas/RawImage is assigned, a simple mini-map is drawn directly
//      in the Game view using Unity's built-in GUI system as a fallback.
//
// Attach this script to any GameObject in your scene. Assign the required
// references in the Inspector (see the public fields below).
public class BigBangMapView : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    // Reference to the BigBangGenerator that holds all sector data.
    // This must be assigned for any part of the view to work.
    public BigBangGenerator generator;

    // ---- Gizmo (Scene view) settings ----------------------------------------

    // Scale factor used to shrink the gizmo cubes so the entire galaxy map
    // fits comfortably inside the Scene view window.
    public float gizmoScale = 0.1f;

    // World-space offset added to every gizmo cube position so the map
    // can be centered on the owning GameObject or shifted as needed.
    public Vector3 gizmoMapOffset = Vector3.zero;

    // ---- Canvas (Game view) settings ----------------------------------------

    // Optional Canvas that contains the RawImage. When the map is toggled on
    // this GameObject is enabled; when toggled off it is disabled again.
    // Leave this null if you want to use the built-in OnGUI fallback instead.
    public Canvas mapCanvas;

    // RawImage that will display the generated map texture. This should
    // typically be stretched to fill the entire Canvas.
    // Only used when mapCanvas is assigned.
    public RawImage mapImage;

    // Keyboard key that toggles the map overlay on and off during Play mode.
    public KeyCode toggleKey = KeyCode.M;

    // ---- Mini-map fallback settings (used when no Canvas is assigned) -------

    // Size of the mini-map square in screen pixels when drawing the fallback
    // mini-map via OnGUI.
    public float miniMapSizePixels = 300f;

    // Screen-space top-left corner of the fallback mini-map in pixels.
    public Vector2 miniMapOffset = new Vector2(10f, 10f);

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // True while the map overlay is visible. Toggled by the toggleKey.
    private bool isVisible = false;

    // Texture that holds the pixel-per-sector map image for the Canvas view.
    private Texture2D mapTexture;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    // Called by Unity when the scene first starts playing.
    private void Start()
    {
        // Hide the canvas at startup so the map does not appear until the
        // player deliberately toggles it on.
        if (mapCanvas != null)
        {
            // Disable the canvas so it is invisible at startup.
            mapCanvas.gameObject.SetActive(false);
        }

        // Clear any stale texture that may be left over on the RawImage.
        if (mapImage != null)
        {
            // Remove the previous texture reference so nothing misleading shows.
            mapImage.texture = null;
        }
    }

    // Called by Unity once per frame. Used to watch for the toggle key.
    private void Update()
    {
        // Key input is only meaningful while the application is running.
        if (!Application.isPlaying)
        {
            // Do nothing outside of Play mode.
            return;
        }

        // Check whether the player pressed the toggle key this frame.
        if (Input.GetKeyDown(toggleKey))
        {
            // Flip the visibility flag.
            isVisible = !isVisible;

            // Show or hide the Canvas when one is assigned.
            if (mapCanvas != null)
            {
                // Activate or deactivate the canvas to match the new visibility.
                mapCanvas.gameObject.SetActive(isVisible);
            }

            // When turning the map on, refresh the texture so it reflects the
            // latest galaxy data.
            if (isVisible)
            {
                // Rebuild the texture from the current sector data.
                RebuildMapTexture();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Scene view Gizmos
    // -------------------------------------------------------------------------

    // Called by Unity to draw gizmos in the Scene view. Each sector is drawn
    // as a small colored cube at its grid position.
    private void OnDrawGizmos()
    {
        // While the game is playing only draw gizmos when the map is visible.
        if (Application.isPlaying && !isVisible)
        {
            // Map is hidden - skip gizmo drawing.
            return;
        }

        // Nothing to draw without a generator.
        if (generator == null)
        {
            // No generator assigned - cannot draw.
            return;
        }

        // Grab the current list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If the sector list is empty the galaxy has not been generated yet.
        if (sectors == null || sectors.Count == 0)
        {
            // No sectors to draw yet.
            return;
        }

        // Read the map dimensions from the generator configuration.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Compute half-extents so the gizmo grid is centered on the GameObject.
        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        // Draw one cube for each sector in the galaxy.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the sector we are about to draw.
            SectorData sector = sectors[i];

            // Compute a 2D offset from the center of the grid.
            float localX = (sector.x - halfWidth) * gizmoScale;
            float localZ = (sector.y - halfHeight) * gizmoScale;

            // Translate to a world position using the GameObject's position and the user offset.
            Vector3 worldPos = transform.position + gizmoMapOffset + new Vector3(localX, 0f, localZ);

            // Pick a color that represents what is in this sector.
            Gizmos.color = GetColorForSector(sector);

            // Draw a small cube at the sector position. The 0.9 factor leaves a
            // tiny gap between cubes so the grid lines are visible.
            Gizmos.DrawCube(worldPos, Vector3.one * gizmoScale * 0.9f);
        }
    }

    // -------------------------------------------------------------------------
    // Fallback mini-map (used when no Canvas is assigned)
    // -------------------------------------------------------------------------

    // Called by Unity to draw immediate-mode GUI in the Game view.
    // This draws a simple mini-map when no Canvas/RawImage has been assigned,
    // so you can still see the map without setting up UI components.
    private void OnGUI()
    {
        // Only draw the fallback mini-map when the app is running, the map is
        // visible, and no Canvas is assigned (the Canvas replaces this).
        if (!Application.isPlaying || !isVisible || mapCanvas != null)
        {
            // Either not playing, hidden, or Canvas is handling the display.
            return;
        }

        // Nothing to draw without a generator.
        if (generator == null)
        {
            // No generator assigned - cannot draw.
            return;
        }

        // Grab the current list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If the sector list is empty there is nothing to draw.
        if (sectors == null || sectors.Count == 0)
        {
            // No sectors to draw yet.
            return;
        }

        // Read the map dimensions from the generator configuration.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Scale each cell to fit the entire map inside the mini-map square.
        float maxDimension = Mathf.Max(width, height);

        // Pixel size of one sector cell in the mini-map.
        float cellSize = miniMapSizePixels / maxDimension;

        // Top-left corner of the mini-map in screen pixels.
        float startX = miniMapOffset.x;
        float startY = miniMapOffset.y;

        // Draw one small colored rectangle for each sector.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the sector we are about to draw.
            SectorData sector = sectors[i];

            // Compute the screen position of this cell.
            float x = startX + sector.x * cellSize;
            float y = startY + sector.y * cellSize;

            // Choose a display color for this sector.
            Color color = GetColorForSector(sector);

            // Save the current GUI color so we can restore it afterwards.
            Color previousColor = GUI.color;

            // Apply the sector color to the GUI system.
            GUI.color = color;

            // Draw a filled rectangle using Unity's built-in white texture.
            GUI.DrawTexture(new Rect(x, y, cellSize, cellSize), Texture2D.whiteTexture);

            // Restore the previous GUI color so other GUI elements are unaffected.
            GUI.color = previousColor;
        }
    }

    // -------------------------------------------------------------------------
    // Canvas texture builder
    // -------------------------------------------------------------------------

    // Rebuilds the Texture2D map from the generator's current sector data and
    // assigns it to the RawImage so the Canvas view shows the latest galaxy.
    private void RebuildMapTexture()
    {
        // Cannot build a texture without a generator.
        if (generator == null)
        {
            // No generator - nothing to rebuild.
            return;
        }

        // Grab the current list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If there are no sectors the galaxy has not been generated yet.
        if (sectors == null || sectors.Count == 0)
        {
            // No sectors - nothing to render.
            return;
        }

        // Read the map dimensions from the generator configuration.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Use a square texture so one pixel can represent one sector along
        // the larger axis without any distortion.
        int textureSize = Mathf.Max(width, height);

        // If no texture exists yet, or if the size has changed, create a new one.
        if (mapTexture == null || mapTexture.width != textureSize || mapTexture.height != textureSize)
        {
            // Allocate a new RGBA32 texture with no mipmaps for crisp rendering.
            mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

            // Point filtering keeps pixel edges sharp when the image is scaled up.
            mapTexture.filterMode = FilterMode.Point;

            // Clamp wrapping prevents the texture from tiling at the edges.
            mapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Fill every pixel with a near-black background before painting sectors.
        Color backgroundColor = Color.black * 0.8f;

        // Allocate a flat pixel array for the whole texture.
        Color[] pixels = new Color[textureSize * textureSize];

        // Set every pixel to the background color.
        for (int i = 0; i < pixels.Length; i++)
        {
            // Paint this pixel with the background color.
            pixels[i] = backgroundColor;
        }

        // Apply the background fill to the texture.
        mapTexture.SetPixels(pixels);

        // Paint one pixel per sector on top of the background.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the sector we are about to paint.
            SectorData sector = sectors[i];

            // The sector grid X maps directly to texture X.
            int pixelX = sector.x;

            // Flip the Y axis so that Y=0 appears at the bottom of the texture,
            // which matches standard screen-space conventions.
            int flippedY = textureSize - 1 - sector.y;

            // Skip any sector whose coordinates fall outside the texture bounds.
            if (pixelX < 0 || pixelX >= textureSize || flippedY < 0 || flippedY >= textureSize)
            {
                // Out-of-range sector - skip it.
                continue;
            }

            // Choose a display color for this sector.
            Color color = GetColorForSector(sector);

            // Write the color to the correct pixel in the texture.
            mapTexture.SetPixel(pixelX, flippedY, color);
        }

        // Flush all SetPixel calls to the GPU so the changes become visible.
        mapTexture.Apply();

        // Assign the finished texture to the RawImage so the UI displays it.
        if (mapImage != null)
        {
            // Give the RawImage our freshly built texture.
            mapImage.texture = mapTexture;
        }
    }

    // -------------------------------------------------------------------------
    // Shared color helper
    // -------------------------------------------------------------------------

    // Returns the display color to use for the given sector in both the Gizmo
    // view and the Canvas map. Keeping this in one place means the colors are
    // always consistent between the two rendering paths.
    private Color GetColorForSector(SectorData sector)
    {
        // Black holes are drawn in purple to make them stand out.
        if (sector.contentType == SectorContentType.BlackHole)
        {
            // Return a vivid purple for black holes.
            return new Color(0.6f, 0.0f, 0.8f);
        }

        // Asteroid fields are drawn in yellow to distinguish them from stars.
        if (sector.contentType == SectorContentType.AsteroidField)
        {
            // Return yellow for asteroid fields.
            return Color.yellow;
        }

        // Star systems may be colored differently depending on special properties.
        if (sector.contentType == SectorContentType.StarSystem)
        {
            // Homeworld systems are drawn in cyan so they are easy to spot.
            if (sector.starSystem != null && sector.starSystem.isHomeworld)
            {
                // Return cyan for homeworld star systems.
                return Color.cyan;
            }

            // Star systems that host an Ancient Trader starport are drawn in red.
            if (sector.starport != null && sector.starport.isAncientTraderPort)
            {
                // Return red for Ancient Trader port systems.
                return Color.red;
            }

            // All other star systems use green.
            return Color.green;
        }

        // Empty sectors use a dim gray so they recede into the background.
        return Color.gray * 0.6f;
    }
}
