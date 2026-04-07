using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This component draws the Big Bang generated map onto a UI Canvas
// as a full-screen image. It is designed to be simple and easy to
// understand for new programmers.
//
// To use this:
// - Create a Canvas in your scene (Screen Space - Camera or Overlay).
// - Add a RawImage under the Canvas and stretch it to fill the screen.
// - Attach this script to any GameObject.
// - Assign the BigBangGenerator and RawImage in the inspector.
// - Optionally assign the Canvas so it can be toggled on and off.
// - Press the M key during play mode to show or hide the map.
public class BigBangMapCanvasView : MonoBehaviour
{
    // Reference to the BigBangGenerator that holds the sector data.
    public BigBangGenerator generator;

    // Canvas which contains the RawImage for the map. This will be
    // enabled and disabled when toggling the map.
    public Canvas mapCanvas;

    // RawImage that will display the generated map texture. This
    // should usually be stretched to fill the entire screen.
    public RawImage mapImage;

    // Key used to toggle the visibility of the map while playing.
    public KeyCode toggleKey = KeyCode.M;

    // True if the map is currently visible in the UI.
    private bool isVisible = false;

    // Texture which holds the generated map pixels.
    private Texture2D mapTexture;

    // Called by Unity when the game starts.
    private void Start()
    {
        // If a canvas was assigned, hide it at the start so the map
        // does not show until the player presses the toggle key.
        if (mapCanvas != null)
        {
            // Disable the canvas GameObject.
            mapCanvas.gameObject.SetActive(false);
        }

        // Make sure the RawImage starts without a texture so there is
        // no misleading old data shown.
        if (mapImage != null)
        {
            // Clear the texture reference on the RawImage.
            mapImage.texture = null;
        }
    }

    // Called by Unity every frame. We use this to watch for the
    // toggle key and show or hide the map accordingly.
    private void Update()
    {
        // Only process input while the application is playing.
        if (!Application.isPlaying)
        {
            // If the game is not running, there is nothing to do.
            return;
        }

        // If the player pressed the toggle key this frame, switch the
        // visibility of the map.
        if (Input.GetKeyDown(toggleKey))
        {
            // Flip the visibility flag.
            isVisible = !isVisible;

            // If we have a canvas assigned, enable or disable it.
            if (mapCanvas != null)
            {
                // Set the active state of the canvas GameObject.
                mapCanvas.gameObject.SetActive(isVisible);
            }

            // When turning the map on, rebuild the map texture so the
            // latest galaxy is shown.
            if (isVisible)
            {
                // Rebuild the map texture from the generator data.
                RebuildMapTexture();
            }
        }
    }

    // Rebuilds the map texture from the generator's sector data.
    private void RebuildMapTexture()
    {
        // If there is no generator assigned, we cannot build a map.
        if (generator == null)
        {
            // Stop here to avoid null reference errors.
            return;
        }

        // Read the list of sectors from the generator.
        List<SectorData> sectors = generator.Sectors;

        // If there are no sectors, the galaxy probably has not been generated yet.
        if (sectors == null || sectors.Count == 0)
        {
            // Stop here because there is nothing to draw.
            return;
        }

        // Read configuration values from the generator for width and height.
        int width = generator.config.mapWidth;
        int height = generator.config.mapHeight;

        // Decide how large the texture should be. We use the larger of
        // width and height so we can draw one pixel per sector along
        // at least one axis.
        int textureSize = Mathf.Max(width, height);

        // If we do not have a texture yet or the size is wrong, create a new one.
        if (mapTexture == null || mapTexture.width != textureSize || mapTexture.height != textureSize)
        {
            // Create a new square texture with RGBA32 format and no mipmaps.
            mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

            // Use point filtering so the pixels stay crisp when scaled.
            mapTexture.filterMode = FilterMode.Point;

            // Clamp the texture so it does not repeat.
            mapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Fill the entire texture with a dark background color first.
        Color backgroundColor = Color.black * 0.8f;

        // Create a one-dimensional array of pixels to fill the texture.
        Color[] pixels = new Color[textureSize * textureSize];

        // Loop through every pixel and set it to the background color.
        for (int i = 0; i < pixels.Length; i++)
        {
            // Set this pixel to the background color.
            pixels[i] = backgroundColor;
        }

        // Apply the background pixels to the texture.
        mapTexture.SetPixels(pixels);

        // Loop through each sector and set a pixel color for it.
        for (int i = 0; i < sectors.Count; i++)
        {
            // Get the current sector.
            SectorData sector = sectors[i];

            // Compute the X and Y pixel positions for this sector.
            int pixelX = sector.x;
            int pixelY = sector.y;

            // If the map is not square, we leave empty pixels on one side.
            // Here we simply flip the Y axis so that Y grows downward
            // in the texture, which matches typical screen coordinates.
            int flippedY = textureSize - 1 - pixelY;

            // Guard against any out-of-range values just in case.
            if (pixelX < 0 || pixelX >= textureSize || flippedY < 0 || flippedY >= textureSize)
            {
                // Skip this sector if it is outside the texture bounds.
                continue;
            }

            // Choose a color based on the sector content type.
            Color color = GetColorForSector(sector);

            // Set the pixel in the texture to the chosen color.
            mapTexture.SetPixel(pixelX, flippedY, color);
        }

        // Apply all pixel changes to the texture so they become visible.
        mapTexture.Apply();

        // If we have a RawImage assigned, assign the texture to it so
        // the UI can display the map.
        if (mapImage != null)
        {
            // Assign the generated texture to the RawImage.
            mapImage.texture = mapTexture;
        }
    }

    // Chooses a color to represent the given sector in the map.
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
