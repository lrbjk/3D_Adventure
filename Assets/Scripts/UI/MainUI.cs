using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class MainUI : SingletonMono<MainUI>
    {
        // Start is called before the first frame update
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
