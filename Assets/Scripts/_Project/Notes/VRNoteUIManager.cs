using UnityEngine;

public class VRNoteUIManager : MonoBehaviour
{
    public static VRNoteUIManager Instance { get; private set; }

    [SerializeField] private int collectedNotes = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCollectedNote()
    {
        collectedNotes++;
        Debug.Log("[VRNoteUIManager] Notes = " + collectedNotes);
    }

    public int GetCollectedNotesCount() => collectedNotes;
}
