using UnityEngine;

public class SkillUIManager : MonoBehaviour//ñvÅ@ñ{ìñÇ…égÇÌÇ»Ç¢Ç»ÇÁå„Ç…è¡Ç∑Ç±Ç∆
{
    public bool IsOpen { get; private set; }

    public void OpenUI() { IsOpen = true; gameObject.SetActive(true); }
    public void CloseUI() { IsOpen = false; gameObject.SetActive(false); }
}
