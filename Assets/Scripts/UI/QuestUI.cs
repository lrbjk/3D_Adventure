using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Astraparasitus
{
    public class QuestUI : MonoBehaviour
    {
        public Image questIcon;
        public Text questName;
        public Text questDescription;
        private Coroutine hideCoroutine;
        // Start is called before the first frame update
        public void SetQuest(Quest quest)
        {
            //questIcon.sprite = quest.info.icon;
            questName.text = quest.info.displayName;
            questDescription.text = quest.info.description;
        }
        private void OnEnable()
        {
            hideCoroutine = StartCoroutine(HideQuest());
        }
        private void OnDisable()
        {

        }
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        private IEnumerator HideQuest()
        {
            yield return new WaitForSecondsRealtime(5);
            gameObject.SetActive(false);
        }
    }
}