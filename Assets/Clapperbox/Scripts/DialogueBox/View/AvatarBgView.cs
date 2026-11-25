using UnityEngine;
using UnityEngine.UI;

public class AvatarBgView : MonoBehaviour
{
    [SerializeField] private Image bg;
        
    public void Show(Color color)
    {
        bg.color = color;
    }
}
