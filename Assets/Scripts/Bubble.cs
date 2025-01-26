using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class Bubble : MonoBehaviour
{
    public GameObject normalMesh;
    public GameObject poppedMesh;
    public GameObject explodedMesh;

    public AudioSource audioSource;
    public AudioClip[] popSounds;
    public AudioClip explodeNoise;

    public AudioClip flagSound;
    public AudioClip unflagSound;

    public GameObject flagVisual;
    public Canvas textCanvas;
    public TMP_Text neighborBombCountText;

    [HideInInspector]
    public int neighborBombCount = 0;
    [HideInInspector]
    public bool isBomb = false;

    [HideInInspector]
    public int gridX = 0;
    [HideInInspector]
    public int gridY = 0;

    [HideInInspector]
    public BubbleWrap board;

    public Color[] neighborCountColors;

    [HideInInspector]
    public bool blockBombDistribution = false;

    public enum State
    {
        Normal,
        Popped,
        Exploded,
        Flagged
    }

    public State state = State.Normal;

    readonly string[] NEIGHBOR_COUNTS = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    private void Awake()
    {
        ResetBubbleEffects();
    }

    public void SetFlagVisible(bool b)
    {
        if (state == State.Normal && b)
        {
            flagVisual.SetActive(b);
            state = State.Flagged;
        }
        else if (state == State.Flagged && !b)
        {
            flagVisual.SetActive(b);
            state = State.Normal;
        }
    }

    public void ToggleFlag()
    {
        if (state == State.Normal)
        {
            flagVisual.SetActive(true);
            state = State.Flagged;

            audioSource.clip = flagSound;
            audioSource.pitch = 1;
            audioSource.Play();
        }
        else if (state == State.Flagged)
        {
            flagVisual.SetActive(false);
            state = State.Normal;

            audioSource.clip = unflagSound;
            audioSource.pitch = 1;
            audioSource.Play();
        }
    }

    public void ExecuteExplodeEffects()
    {
        if (state == State.Normal)
        {
            normalMesh.SetActive(false);
            explodedMesh.SetActive(true);
            state = State.Exploded;

            audioSource.clip = explodeNoise;
            audioSource.pitch = 1;
            audioSource.Play();
        }
    }

    public void ExecutePopEffects(bool noSFX = false)
    {
        if (state == State.Normal)
        {
            normalMesh.SetActive(false);
            poppedMesh.SetActive(true);

            if (neighborBombCount > 0)
            {
                neighborBombCountText.SetText(NEIGHBOR_COUNTS[neighborBombCount]);
                textCanvas.gameObject.SetActive(true);

                if (neighborCountColors != null && neighborBombCount < neighborCountColors.Length)
                {
                    neighborBombCountText.color = neighborCountColors[neighborBombCount];
                }
            }

            if (!noSFX)
            {
                AudioClip clip = popSounds[(int)(Random.value * popSounds.Length)];
                audioSource.clip = clip;
                audioSource.pitch = 0.8f + Random.value * 0.4f;
                audioSource.Play();
            }

            state = State.Popped;
        }
    }

    public void ResetBubbleEffects()
    {
        audioSource.Stop();
        audioSource.pitch = 1;
        audioSource.clip = null;

        normalMesh.SetActive(true);
        poppedMesh.SetActive(false);
        explodedMesh.SetActive(false);
        textCanvas.gameObject.SetActive(false);
        flagVisual.SetActive(false);
        state = State.Normal;
    }
}
