using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    public class UIToggleSwitch : MonoBehaviour
    {

        public Image backgroundFilling;
        public Image cursor;
        public float switchDuration;

        private Toggle toggle;
        private float startTime = 0;
        private bool lastToggle = false;

        private float fillValue = 0.0f;

        void Start()
        {
            toggle = GetComponent<Toggle>();
            lastToggle = toggle.isOn;
            fillValue = lastToggle ? 1.0f : 0.0f;
        }

        void Update()
        {
            if (toggle.isOn != lastToggle)
            {
                lastToggle = !lastToggle;
                VRCModLogger.Log("Toggle switched to " + lastToggle);
                if (Time.time - startTime >= switchDuration) startTime = Time.time;
                else startTime = Time.time - ((startTime - Time.time) * switchDuration);
            }
            fillValue = Mathf.Clamp((Time.time - startTime) * (1 / switchDuration), 0, 1);
            if (!lastToggle) fillValue = 1 - fillValue;

            if(fillValue != 0 && fillValue != 1)
            {
                VRCModLogger.Log("fillValue: " + fillValue);
            }

            cursor.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetSwitchValue(-37, 37), 0);
            backgroundFilling.fillAmount = GetSwitchValue(0.1f, 0.9f);
        }

        private float GetSwitchValue(float min, float max)
        {
            return (fillValue * (max - min)) + min;
        }

        public bool IsOn()
        {
            return toggle.isOn;
        }
    }
}
