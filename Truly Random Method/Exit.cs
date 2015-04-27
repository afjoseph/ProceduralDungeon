using System.Collections;
using UnityEngine;

/**
* The exit object that would spawn on some random tile. Give it a "Beeping" sound
* and allow it to repeat the same scene when the game ends.
*/ 

public class Exit : MonoBehaviour
{
    public AudioClip[] m_SoundOnEnd;
    public float m_BeepDelay = 8f;


    //Unity Functions

    void Awake()
    {
        StartCoroutine(UpdateCo());
    }

    private IEnumerator UpdateCo()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(m_BeepDelay);
            GetComponent<AudioSource>().Play();
        }
    }

    void OnTriggerEnter(Collider a_Other)
    {
        if (a_Other.tag != "Player")
            return;

        AudioSource.PlayClipAtPoint(
            m_SoundOnEnd[Random.Range(0, m_SoundOnEnd.Length)],
            transform.position);

        a_Other.GetComponent<PlayerActions>().SetInvin(true);

        DungeonManager.Instance.SetSavedPlayerHealth(a_Other.GetComponent<PlayerActions>().GetHealth());
        CameraFade.Instance.FadeOut();
        StartCoroutine(LoadLevelDelay(2f));
    }

    private IEnumerator LoadLevelDelay(float a_Delay)
    {
        yield return new WaitForSeconds(a_Delay);
        Application.LoadLevel(Application.loadedLevel);
    }
}