using System.Text;
using TMPro;
using UnityEngine;

public class InventoryHUD : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;
    [SerializeField] TMP_Text label;

    void OnEnable()  { inventory.Changed += Refresh; Refresh(); }
    void OnDisable() { inventory.Changed -= Refresh; }

    void Refresh()
    {
        if (inventory.Items.Count == 0) { label.text = "(empty)"; return; }
        var sb = new StringBuilder();
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            string name = inventory.Items[i].definition.displayName;
            sb.AppendLine(i == inventory.ActiveIndex ? $"> {name}" : $"  {name}");
        }
        label.text = sb.ToString();
    }
}
