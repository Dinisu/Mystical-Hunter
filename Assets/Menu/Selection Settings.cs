using UnityEngine;
using static SelectionSettings;

public class SelectionSettings : MonoBehaviour
{
    public enum Choose
    {
        None,
        StatusIcon,
        StatusMenu,
        ItemMenu,
        Consumables,
        Equipment,
        Valuables,
        ItemUsechoice,
        ItemUse,
        WeaponChoice,
        ArmorChoice,
        Accessories1Choice,
        Accessories2Choice
    }
    [SerializeField, Header("‘I‘ð‚µ")]
    public Choose choose;
}
