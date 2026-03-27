using UnityEngine;

public class B_SelectionSettings : MonoBehaviour
{
    public enum Choose
    {
        None,
        SkillSelection,
        ItemSelection,
        Switchingsides,
        Skill_Use,
        Item_Use,
        Character_Switching,
        AreaofEffect,
        run_away,
        StatusCheck
    }
    [SerializeField, Header("選択し")]
    public Choose choose;
}
