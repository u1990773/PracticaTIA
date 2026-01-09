using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VRNoteUIManager : MonoBehaviour
{
    [Header("Note Panel")]
    public CanvasGroup notePanel;
    public TMP_Text noteBodyTMP;
    public TMP_Text hintTMP;
    public RawImage noteRawImage;

    [Header("Journal Panel")]
    public CanvasGroup journalPanel;
    public TMP_Text journalTMP;

    [Header("Keyboard fallback (Simulator)")]
    public bool enableKeyboardFallback = true;

    private Action _onConfirm;
    private bool _noteOpen;
    private bool _journalOpen;
    private readonly List<string> _journalEntries = new();

    private void Awake()
    {
        // Oculta SIEMPRE al inicio
        SetGroup(notePanel, false);
        SetGroup(journalPanel, false);

        if (hintTMP != null)
            hintTMP.text = "ENTER = Guardar/Recoger | ESC = Cerrar | J = Diario";
    }

    private void Update()
    {
        if (!enableKeyboardFallback || Keyboard.current == null)
            return;

        if (Keyboard.current.jKey.wasPressedThisFrame)
            ToggleJournal();

        if (!_noteOpen) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            Confirm();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseNote();
    }

    public void ShowNote(string message, Texture imageTexture, Action onConfirm)
    {
        _onConfirm = onConfirm;
        _noteOpen = true;

        if (noteBodyTMP != null)
            noteBodyTMP.text = string.IsNullOrWhiteSpace(message) ? "(nota vacía)" : message;

        if (noteRawImage != null)
        {
            noteRawImage.texture = imageTexture;
            noteRawImage.gameObject.SetActive(imageTexture != null);
        }

        if (hintTMP != null)
            hintTMP.text = "ENTER = Guardar/Recoger | ESC = Cerrar | J = Diario";

        SetGroup(notePanel, true);
    }

    public void Confirm()
    {
        if (!_noteOpen) return;
        _onConfirm?.Invoke();
        _onConfirm = null;
        CloseNote();
    }

    public void CloseNote()
    {
        _noteOpen = false;
        SetGroup(notePanel, false);
    }

    public void ToggleJournal()
    {
        _journalOpen = !_journalOpen;
        RefreshJournalText();
        SetGroup(journalPanel, _journalOpen);
    }

    public void AddToJournal(string message)
    {
        _journalEntries.Add(message ?? "");
        RefreshJournalText();
    }

    private void RefreshJournalText()
    {
        if (journalTMP == null) return;

        if (_journalEntries.Count == 0)
        {
            journalTMP.text = "Sin notas guardadas todavía.";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < _journalEntries.Count; i++)
        {
            sb.AppendLine($"[{i + 1}] {_journalEntries[i]}");
            sb.AppendLine();
        }

        journalTMP.text = sb.ToString();
    }

    private static void SetGroup(CanvasGroup g, bool on)
    {
        if (g == null) return;

        g.gameObject.SetActive(on);   // <- clave (evita “se queda en pantalla”)
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}
