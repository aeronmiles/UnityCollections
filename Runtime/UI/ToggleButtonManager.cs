using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ToggleButtonManager : MonoBehaviour
{
    public event Action<ToggleButton> OnButtOnDown;
    [SerializeField] List<ToggleButton> m_buttons = new List<ToggleButton>();
    [SerializeField] bool m_toggleOnPointerDown = true;
    [SerializeField] bool m_toggleOnPointerEnter = true;
    [SerializeField] int m_maxSelectionCount = 3;
    [SerializeField] bool m_deselectOnEnable = true;

    public List<ToggleButton> ToggleButtons => m_buttons;

    public List<ToggleButton> ActiveToggleButtons => m_buttons.Where(b => b.gameObject.activeSelf).ToList();

    private List<ToggleButton> m_registeredButtons = new List<ToggleButton>();

    int SelectionCount
    {
        get
        {
            int count = 0;
            foreach (var btn in m_buttons)
                if (btn.Selected && btn.gameObject.activeSelf) count++;

            return count;
        }
    }

    public int[] SelectedIndexes
    {
        get
        {
            int[] indexes = new int[SelectionCount];
            int i = 0;
            foreach (var btn in m_buttons)
                if (btn.Selected && btn.gameObject.activeSelf)
                    indexes[i++] = btn.Id;

            return indexes;
        }
    }

    public void OnEnable()
    {
        validateToggleButtons();
        foreach (var button in m_buttons)
            RegisterButton(button);

        if (m_deselectOnEnable) DeselectAll();
    }

    public void ValidateEnumReference(Type type)
    {
        if (!type.IsEnum)
        {
            Debug.LogError("ToggleButtonManager -> ValidateByEnumType() :: type is not an enum", this);
            return;
        }

        foreach (var btn in m_buttons)
            btn.ValidateEnumReference(type);
    }

    public void SetTextsByEnum(Type type)
    {
        if (!type.IsEnum)
        {
            Debug.LogError("ToggleButtonManager -> SetTextsByEnum() :: type is not an enum", this);
            return;
        }

        int i = 0;
        foreach (var btn in m_buttons)
            btn.SetTextByEnum(type, i++);
    }

    private void OnDisable()
    {
        foreach (var button in m_buttons)
            UnRegister(button);
    }

    public void AddButton(ToggleButton button)
    {
        if (!m_buttons.Contains(button))
            m_buttons.Add(button);

        RegisterButton(button);
    }

    public void RemoveButton(ToggleButton button)
    {
        m_buttons.Remove(button);

        UnRegister(button);
    }

    private void RegisterButton(ToggleButton button)
    {
        if (!m_registeredButtons.Contains(button))
        {
            if (m_toggleOnPointerDown)
                button.OnDown += OnDown;

            if (m_toggleOnPointerEnter)
            {
                button.OnEnter += OnEnter;
                button.OnExit += OnExit;
            }
            m_registeredButtons.Add(button);
        }
    }


    private void UnRegister(ToggleButton button)
    {
        if (m_toggleOnPointerDown)
            button.OnDown -= OnDown;

        if (m_toggleOnPointerEnter)
        {
            button.OnEnter -= OnEnter;
            button.OnExit -= OnExit;
        }
        m_registeredButtons.Remove(button);
    }

    public void DestroyButtons()
    {
        foreach (var button in m_buttons)
            Destroy(button.gameObject);

        m_buttons.Clear();
    }

    private void validateToggleButtons()
    {
        int n = m_buttons.Count;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if (m_buttons[i] == m_buttons[j]) continue;
                if (m_buttons[i].Id == m_buttons[j].Id)
                {
                    Debug.LogError($"ToggleButtonManager -> validateToggleButtons() :: duplicate Id {m_buttons[i].Id}", this);
                }
            }
        }
    }

    public void SelectById(int id)
    {
        Debug.Log($"ToggleButtonManager -> SelectById(id = {id})", this);
        var buttonOfId = m_buttons.FirstOrDefault(b => b.Id == id);
        if (buttonOfId == null)
        {
            Debug.LogError("Button of id not found");
            return;
        }

        if (m_maxSelectionCount == 1)
        {
            foreach (var b in m_buttons)
                b.Select(b.Id == id);
        }
        else if (SelectionCount == m_maxSelectionCount && !buttonOfId.Selected)
        {
            return;
        }
        else
        {
            buttonOfId.Select(!buttonOfId.Selected);
        }


    }

    public void SelectByIds(IEnumerable<int> ids)
    {
        foreach (var b in m_buttons)
            b.Select(ids.Contains(b.Id));
    }

    public void SetButtonObjectsActive(bool show)
    {
        foreach (var b in m_buttons)
            b.SetActive(show);
    }

    public void DeselectAll()
    {
        foreach (var b in m_buttons)
            b.Select(false);
    }

    private void OnExit(ToggleButton btn)
    {
        foreach (var b in m_buttons)
            b.Exit();
    }

    private void OnEnter(ToggleButton btn)
    {
        foreach (var b in m_buttons)
            if (b == btn)
                b.Enter();
            else
                b.Exit();
    }

    private void OnDown(ToggleButton btn)
    {
        Toggle(btn);
        OnButtOnDown?.Invoke(btn);
    }

    private void Toggle(ToggleButton btn)
    {
        SelectById(btn.Id);
    }
}