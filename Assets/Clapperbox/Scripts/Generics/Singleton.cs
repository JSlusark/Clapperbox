using UnityEngine;

namespace Clapperbox.Runtime.Scripts.Generics
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance; // static means it belongs to the class
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = ReturnInstance();
                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this as T;
        }


        private static T ReturnInstance()
        {
            var instance = FindAnyObjectByType<T>();
            if(instance != null)
                return instance;
            
            var name = typeof(T).Name + "_Singleton";
            var createdInstance = new GameObject(name).AddComponent<T>();
            
            return createdInstance;
        }
    }
}