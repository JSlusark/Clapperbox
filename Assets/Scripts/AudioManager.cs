using UnityEngine;

    public class AudioManager : MonoBehaviour
    {
        
        public static AudioManager Instance;
        [SerializeField] private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;
        }
        

        public void PlaySound(AudioClip clip)
        {
            if(clip == null) return;
            _audioSource.PlayOneShot(clip);
        }

    }