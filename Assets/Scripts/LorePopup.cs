using UnityEngine;

public class LorePopup : MonoBehaviour
{
    [TextArea(5, 20)]
    public string loreText =
@"Greyhaven was once a quiet town, surrounded by rolling grassy fields and a small river. 
But now, the dead walk. From a graveyard across the river, hordes of zombies spill onto the land, seeking to overwhelm any survivors.

You stand alone on the grassy terrain, with only a bridge connecting you to the danger beyond. At the end of the path, a hospital lab works tirelessly to create a cure—but it won’t be ready overnight.

Your task is simple… survive the nights. Each wave of zombies brings more danger, and every kill brings the cure closer to completion. Can you hold the line, protect the bridge, and survive long enough to see Greyhaven’s salvation?";

    public int fontSize = 20;
    private bool showLore = true;

    private void Start()
    {
        // Pauzeer de tijd bij start
        Time.timeScale = 0f;

        // Optioneel: blokkeren van player movement scripts
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
                playerMovement.enabled = false;
        }
    }

    private void Update()
    {
        if (showLore && Input.anyKeyDown)
        {
            showLore = false;

            // Hervat de tijd
            Time.timeScale = 1f;

            // Schakel player movement weer in
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                    playerMovement.enabled = true;
            }
        }
    }

    private void OnGUI()
    {
        if (!showLore) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.wordWrap = true;
        style.normal.textColor = Color.white; // witte tekst

        float width = Screen.width * 0.7f;
        float height = Screen.height * 0.7f;
        float x = (Screen.width - width) / 2;
        float y = (Screen.height - height) / 2;

        // Semi-transparante donkere achtergrond
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.98f); // bijna volledig zwart, beter leesbaar
        GUI.Box(new Rect(x - 5, y - 5, width + 10, height + 10), "");
        GUI.color = previousColor;

        GUI.Label(new Rect(x, y, width, height), loreText + "\n\nPress any key to start", style);
    }
}
