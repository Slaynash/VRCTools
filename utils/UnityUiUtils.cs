using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRCTools.utils;

namespace VRCTools
{
    public static class UnityUiUtils
    {
        private static Sprite toggle_background;
        private static Sprite toggle_cursor;


        public static Transform DuplicateButton(Transform baseButton, string buttonText, Vector2 posDelta)
        {
            GameObject buttonGO = new GameObject("DuplicatedButton", new Type[] {
                typeof(Button),
                typeof(Image)
            });

            RectTransform rtO = baseButton.GetComponent<RectTransform>();
            RectTransform rtT = buttonGO.GetComponent<RectTransform>();

            buttonGO.transform.SetParent(baseButton.parent);
            buttonGO.GetComponent<Image>().sprite = baseButton.GetComponent<Image>().sprite;
            buttonGO.GetComponent<Image>().type = baseButton.GetComponent<Image>().type;
            buttonGO.GetComponent<Image>().fillCenter = baseButton.GetComponent<Image>().fillCenter;
            buttonGO.GetComponent<Button>().colors = baseButton.GetComponent<Button>().colors;
            buttonGO.GetComponent<Button>().targetGraphic = buttonGO.GetComponent<Image>();

            rtT.localScale = rtO.localScale;

            rtT.anchoredPosition = rtO.anchoredPosition;
            rtT.sizeDelta = rtO.sizeDelta;

            rtT.localPosition = rtO.localPosition + new Vector3(posDelta.x, posDelta.y, 0);
            rtT.localRotation = rtO.localRotation;

            GameObject textGO = new GameObject("Text", typeof(Text));
            textGO.transform.SetParent(buttonGO.transform);

            RectTransform rtO2 = baseButton.Find("Text").GetComponent<RectTransform>();
            RectTransform rtT2 = textGO.GetComponent<RectTransform>();
            rtT2.localScale = rtO2.localScale;

            rtT2.anchorMin = rtO2.anchorMin;
            rtT2.anchorMax = rtO2.anchorMax;
            rtT2.anchoredPosition = rtO2.anchoredPosition;
            rtT2.sizeDelta = rtO2.sizeDelta;

            rtT2.localPosition = rtO2.localPosition;
            rtT2.localRotation = rtO2.localRotation;

            Text tO = baseButton.Find("Text").GetComponent<Text>();
            Text tT = textGO.GetComponent<Text>();
            tT.text = buttonText;
            tT.font = tO.font;
            tT.fontSize = tO.fontSize;
            tT.fontStyle = tO.fontStyle;
            tT.alignment = tO.alignment;
            tT.color = tO.color;

            return buttonGO.transform;
        }

        public static Transform DuplicateImage(Transform baseImage, Vector2 posDelta)
        {
            Transform imageT = GameObject.Instantiate(baseImage, baseImage.parent);
            Image imageImage = imageT.GetComponent<Image>();
            imageImage.sprite = baseImage.GetComponent<Image>().sprite;
            imageImage.type = baseImage.GetComponent<Image>().type;
            imageImage.fillCenter = baseImage.GetComponent<Image>().fillCenter;

            imageT.localPosition += new Vector3(posDelta.x, posDelta.y);

            return imageT;
        }

        public static Transform CreateScrollView(RectTransform parentTransform, int viewWidth, int viewHeight, int maxWidth, int maxHeight, bool scrollHorizontally, bool scrollVertically)
        {
            GameObject scrollView = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect));
            scrollView.transform.SetParent(parentTransform);
            scrollView.transform.localScale = Vector3.one;
            scrollView.transform.localRotation = Quaternion.identity;
            scrollView.transform.localPosition = Vector3.zero;
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(viewWidth, viewHeight);
            scrollView.GetComponent<ScrollRect>().horizontal = scrollHorizontally;
            scrollView.GetComponent<ScrollRect>().vertical = scrollVertically;
            scrollView.layer = parentTransform.gameObject.layer;


            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform);
            viewport.transform.localScale = Vector3.one;
            viewport.transform.localRotation = Quaternion.identity;
            viewport.transform.localPosition = Vector3.zero;
            viewport.GetComponent<RectTransform>().sizeDelta = new Vector2(viewWidth, viewHeight);
            viewport.layer = scrollView.layer;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // Create a transparent sprite for the Viewport
            Texture2D tex = new Texture2D(2, 2);
            Color alpha = new Color(1, 2, 3, 1);
            tex.SetPixels(new Color[] { alpha, alpha, alpha, alpha });
            tex.Apply();
            viewport.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));


            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform);
            content.transform.localScale = Vector3.one;
            content.transform.localRotation = Quaternion.identity;
            content.transform.localPosition = Vector3.zero;
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollHorizontally ? maxWidth : 0, scrollVertically ? maxHeight : 0);
            content.GetComponent<RectTransform>().anchorMin = new Vector2(scrollHorizontally ? 1 : 0, scrollVertically ? 1 : 0);
            content.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            content.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            content.layer = viewport.layer;

            scrollView.GetComponent<ScrollRect>().content = content.GetComponent<RectTransform>();
            scrollView.GetComponent<ScrollRect>().viewport = viewport.GetComponent<RectTransform>();

            return content.transform;
        }

        public static UIToggleSwitch CreateUIToggleSwitch(RectTransform parent)
        {
            if(toggle_background == null)
            {
                Texture2D tex = new Texture2D(2, 2);
                Texture2DUtils.LoadImage(tex, Convert.FromBase64String(ImageDatas.UI_TOGGLE_BACKGROUND));
                toggle_background = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            if (toggle_cursor == null)
            {
                Texture2D tex = new Texture2D(2, 2);
                Texture2DUtils.LoadImage(tex, Convert.FromBase64String(ImageDatas.UI_TOGGLE_CURSOR));
                toggle_cursor = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            // Create objects
            GameObject toggleGO = new GameObject("ToggleGO", typeof(RectTransform), typeof(Toggle), typeof(UIToggleSwitch));
            toggleGO.transform.SetParent(parent, false);
            GameObject backgroundGO = new GameObject("background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundGO.transform.SetParent(toggleGO.transform, false);
            GameObject fillImageGO = new GameObject("fillImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillImageGO.transform.SetParent(backgroundGO.transform, false);
            GameObject cursorGO = new GameObject("cursor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cursorGO.transform.SetParent(toggleGO.transform, false);

            // Set ToggleGO values
            toggleGO.layer = parent.gameObject.layer;
            toggleGO.GetComponent<Toggle>().interactable = true;
            toggleGO.GetComponent<Toggle>().transition = Selectable.Transition.None;
            toggleGO.GetComponent<Toggle>().isOn = false;
            toggleGO.GetComponent<UIToggleSwitch>().backgroundFilling = fillImageGO.GetComponent<Image>();
            toggleGO.GetComponent<UIToggleSwitch>().cursor = cursorGO.GetComponent<Image>();
            toggleGO.GetComponent<UIToggleSwitch>().switchDuration = 0.12f;
            toggleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(134, 60);

            // Set background values
            backgroundGO.layer = toggleGO.layer;
            backgroundGO.GetComponent<Image>().sprite = toggle_background;
            backgroundGO.GetComponent<Image>().color = new Color(0, 0.957f, 1);
            backgroundGO.GetComponent<Image>().raycastTarget = false;
            backgroundGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);

            // Set fillImage values
            fillImageGO.layer = backgroundGO.layer;
            fillImageGO.GetComponent<Image>().sprite = toggle_background;
            fillImageGO.GetComponent<Image>().color = new Color(0, 0.67f, 1);
            fillImageGO.GetComponent<Image>().type = Image.Type.Filled;
            fillImageGO.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
            fillImageGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);

            // Set cursor values
            cursorGO.layer = toggleGO.layer;
            cursorGO.GetComponent<Image>().sprite = toggle_cursor;
            cursorGO.GetComponent<Image>().color = new Color(0, 0.67f, 1);
            cursorGO.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            cursorGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-37, 0);

            return toggleGO.GetComponent<UIToggleSwitch>();
        }
    }
}
