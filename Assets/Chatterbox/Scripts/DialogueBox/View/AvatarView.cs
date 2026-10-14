using UnityEngine;
using UnityEngine.UI;


public class AvatarView : MonoBehaviour
    {
        [SerializeField] private Image avatar;
        
        public void Show(Sprite sprite)
        {
            avatar.sprite = sprite;
            avatar.color = Color.white;
        }
        
        
    }