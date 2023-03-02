using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

[RequireComponent(typeof(Button))]
[ExecuteInEditMode]
public class ToggleButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<ToggleButton> OnDown;
    public event Action<ToggleButton> OnEnter;
    public event Action<ToggleButton> OnExit;

    public int Id;
    [SerializeField] SelectableTarget[] m_selectableTargets;

    [SerializeField] bool m_selected = false;
    public bool Selected => m_selected;
    Button m_button;

    private void OnEnable()
    {
        m_button = GetComponent<Button>();
        if (m_button.transition != Selectable.Transition.None)
        {
            m_button.transition = Selectable.Transition.None;
            Debug.LogWarning("ToggleButton -> OnEnable() :: setting button transition to None", this);
        }
        Select(Selected);
    }

    public void Select(bool selected)
    {
        if (!gameObject.activeSelf) return;
        // Debug.Log($"{name} :: ToggleButton -> Select(selected = {selected})");
        m_selected = selected;
        foreach (var s in m_selectableTargets)
            s.Select(selected);
    }

    public void Enter()
    {
        if (!gameObject.activeSelf) return;
        // Debug.Log($"{name} :: ToggleButton -> Enter()");
        foreach (var s in m_selectableTargets)
            s.Enter();
    }

    public void Exit()
    {
        if (!gameObject.activeSelf) return;
        // Debug.Log($"{name} :: ToggleButton -> Exit()");
        foreach (var s in m_selectableTargets)
            s.Exit();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDown?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke(this);
    }	
}


[Serializable]
public class SelectableTarget
{
    [SerializeField] Image[] m_targetImages;
    [SerializeField] TMP_Text[] m_targetTexts;
    [SerializeField] Color m_selectedColor = Color.white;
    [SerializeField] Color m_highlightedColor = Color.white;
    [SerializeField] Color m_unselectedColor = Color.white;
    [SerializeField] bool m_hideWhenNotSelected = true;

    bool m_selected = false;
    public void Enter()
    {
        if (m_selected) return;
        foreach (var i in m_targetImages)
            i.color = m_highlightedColor;
        foreach (var t in m_targetTexts)
            t.color = m_highlightedColor;
    }

    public void Exit()
    {
        if (m_selected) return;
        foreach (var i in m_targetImages)
            i.color = m_unselectedColor;
        foreach (var t in m_targetTexts)
            t.color = m_unselectedColor;
    }

    public void Select(bool selected)
    {
        m_selected = selected;
        foreach (var i in m_targetImages)
        {
            i.gameObject.SetActive(selected || !m_hideWhenNotSelected);
            i.color = selected ? m_selectedColor : m_unselectedColor;
        }
        foreach (var t in m_targetTexts)
        {
            t.gameObject.SetActive(selected || !m_hideWhenNotSelected);
            t.color = selected ? m_selectedColor : m_unselectedColor;
        }
    }
}