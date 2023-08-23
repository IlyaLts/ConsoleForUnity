using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Console
{
    public class ConsoleScrollView : MonoBehaviour
    {
        [SerializeField]
        private GameObject viewport;
        [SerializeField]
        private GameObject content;
        [SerializeField]
        private GameObject scrollIndicator;

        private const float initialDelay = 0.5f;
        private const float subsequentDelay = 0.05f;

        private Coroutine scrollUp;
        private Coroutine scrollDown;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageUp) && !Input.GetKey(KeyCode.PageDown))
                scrollUp = StartCoroutine(ScrollUp());
            
            if (Input.GetKeyDown(KeyCode.PageDown) && !Input.GetKey(KeyCode.PageUp))
                scrollDown = StartCoroutine(ScrollDown());

            if (Input.GetKeyUp(KeyCode.PageUp))
                StopCoroutine(scrollUp);
            
            if (Input.GetKeyUp(KeyCode.PageDown))
                StopCoroutine(scrollDown);
        }

        void LateUpdate()
        {
            scrollIndicator.SetActive(GetComponent<ScrollRect>().verticalNormalizedPosition > 1e-06f);
        }

        IEnumerator ScrollUp()
        {
            float delay = initialDelay;

            while (Input.GetKey(KeyCode.PageUp))
            {
                ScrollRect schrollRect = GetComponent<ScrollRect>();

                if (schrollRect.verticalNormalizedPosition < 1.0f)
                {
                    float contentHeight = content.GetComponent<RectTransform>().rect.height;
                    float viewportHeight = viewport.GetComponent<RectTransform>().rect.height;

                    schrollRect.verticalNormalizedPosition += schrollRect.scrollSensitivity / (contentHeight - viewportHeight);
                }
                else
                {
                    schrollRect.verticalNormalizedPosition = 1.0f;
                }

                yield return new WaitForSeconds(delay);
                delay = subsequentDelay;
            }
        }

        IEnumerator ScrollDown()
        {
            float delay = initialDelay;

            while (Input.GetKey(KeyCode.PageDown))
            {
                ScrollRect schrollRect = gameObject.GetComponent<ScrollRect>();

                if (schrollRect.verticalNormalizedPosition > 0.0f)
                {
                    float contentHeight = content.GetComponent<RectTransform>().rect.height;
                    float viewportHeight = viewport.GetComponent<RectTransform>().rect.height;

                    schrollRect.verticalNormalizedPosition -= schrollRect.scrollSensitivity / (contentHeight - viewportHeight);
                }
                else
                {
                    schrollRect.verticalNormalizedPosition = 0.0f;
                }

                yield return new WaitForSeconds(delay);
                delay = subsequentDelay;
            }
        }
    }
}
