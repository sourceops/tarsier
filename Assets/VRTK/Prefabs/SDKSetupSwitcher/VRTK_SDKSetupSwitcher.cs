﻿// SDK Setup Switcher|Prefabs|0010
namespace VRTK
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides a GUI overlay to allow switching the loaded VRTK_SDKSetup of the the current VRTK_SDKManager.
    /// </summary>
    /// <remarks>
    /// **Prefab Usage:**
    ///  * Place the `VRTK/Prefabs/SDKSetupSwitcher/SDKSetupSwitcher` prefab into the scene hierarchy.
    /// </remarks>
    public class VRTK_SDKSetupSwitcher : MonoBehaviour
    {
        [Header("Fallback Objects")]

        [SerializeField]
        protected Camera fallbackCamera;
        [SerializeField]
        protected EventSystem eventSystem;

        [Header("Object References")]

        [SerializeField]
        protected Text currentText;
        [SerializeField]
        protected RectTransform statusPanel;
        [SerializeField]
        protected RectTransform selectionPanel;

        [SerializeField]
        protected Button switchButton;
        [SerializeField]
        protected Button cancelButton;
        [SerializeField]
        protected Button chooseButton;
        [SerializeField]
        protected bool playareaSync = true;

        protected enum ViewingState
        {
            Status,
            Selection
        }

        protected readonly List<GameObject> chooseButtonGameObjects = new List<GameObject>();
        protected Transform currentPlayarea;

        protected virtual void Awake()
        {
            fallbackCamera.gameObject.SetActive(false);
            eventSystem.gameObject.SetActive(false);
            if (chooseButton)
            {
                chooseButton.gameObject.SetActive(false);
            }
        }

        protected virtual void OnEnable()
        {
            VRTK_SDKManager.SubscribeLoadedSetupChanged(OnLoadedSetupChanged);

            switchButton.onClick.AddListener(OnSwitchButtonClick);
            cancelButton.onClick.AddListener(OnCancelButtonClick);

            Show(ViewingState.Status);
        }

        protected virtual void OnDisable()
        {
            VRTK_SDKManager.UnsubscribeLoadedSetupChanged(OnLoadedSetupChanged);

            if (switchButton)
            {
                switchButton.onClick.RemoveListener(OnSwitchButtonClick);
            }
            cancelButton.onClick.RemoveListener(OnCancelButtonClick);

            Show(ViewingState.Status);
        }

        protected virtual void OnLoadedSetupChanged(VRTK_SDKManager sender, VRTK_SDKManager.LoadedSetupChangeEventArgs e)
        {
            Show(ViewingState.Status);
            if (playareaSync && currentPlayarea != null)
            {
                Transform newPlayarea = VRTK_DeviceFinder.PlayAreaTransform();
                newPlayarea.transform.position = currentPlayarea.transform.position;
                newPlayarea.transform.rotation = currentPlayarea.transform.rotation;
                VRTK_SharedMethods.SetGlobalScale(newPlayarea, currentPlayarea.transform.lossyScale);
            }
            currentPlayarea = VRTK_DeviceFinder.PlayAreaTransform();
        }

        protected virtual void OnSwitchButtonClick()
        {
            Show(ViewingState.Selection);
        }

        protected virtual void OnCancelButtonClick()
        {
            Show(ViewingState.Status);
        }

        protected virtual void Show(ViewingState viewingState)
        {
            switch (viewingState)
            {
                case ViewingState.Status:
                    RemoveCreatedChooseButtons();
                    UpdateCurrentText();
                    selectionPanel.gameObject.SetActive(false);
                    statusPanel.gameObject.SetActive(true);

                    break;
                case ViewingState.Selection:
                    AddSelectionButtons();
                    selectionPanel.gameObject.SetActive(true);
                    statusPanel.gameObject.SetActive(false);

                    break;
                default:
                    VRTK_Logger.Fatal(new ArgumentOutOfRangeException("viewingState", viewingState, null));
                    return;
            }

            bool isAnyOtherCameraUsed = VRTK_SDKManager.GetAllSDKSetups().Any(setup => setup != null && setup.gameObject.activeSelf)
                                        || VRTK_DeviceFinder.HeadsetCamera() != null;
            fallbackCamera.gameObject.SetActive(!isAnyOtherCameraUsed);
            eventSystem.gameObject.SetActive(EventSystem.current == null || EventSystem.current == eventSystem);
        }

        protected virtual void UpdateCurrentText()
        {
            VRTK_SDKSetup loadedSetup = VRTK_SDKManager.GetLoadedSDKSetup();
            currentText.text = (loadedSetup == null ? "None" : loadedSetup.name);
        }

        protected virtual void AddSelectionButtons()
        {
            if (VRTK_SDKManager.GetLoadedSDKSetup() != null)
            {
                GameObject chooseNoneButton = Instantiate(chooseButton.gameObject, chooseButton.transform.parent);
                chooseNoneButton.GetComponentInChildren<Text>().text = "None";
                chooseNoneButton.name = "ChooseNoneButton";
                chooseNoneButton.SetActive(true);

                chooseNoneButton.GetComponent<Button>().onClick.AddListener(
                    () => VRTK_SDKManager.AttemptUnloadSDKSetup(true)
                );

                chooseButtonGameObjects.Add(chooseNoneButton);
            }

            VRTK_SDKSetup[] setups = VRTK_SDKManager.GetAllSDKSetups();
            for (int index = 0; index < setups.Length; index++)
            {
                VRTK_SDKSetup setup = setups[index];
                if (setup == null || setup == VRTK_SDKManager.GetLoadedSDKSetup())
                {
                    continue;
                }

                GameObject chooseButtonCopy = Instantiate(chooseButton.gameObject, chooseButton.transform.parent);
                chooseButtonCopy.GetComponentInChildren<Text>().text = setup.name;
                chooseButtonCopy.name = string.Format("Choose{0}Button", setup.name);
                chooseButtonCopy.SetActive(true);

                int indexCopy = index;
                Button button = chooseButtonCopy.GetComponent<Button>();
                button.onClick.AddListener(
                    () => VRTK_SDKManager.AttemptTryLoadSDKSetup(indexCopy, true, setups)
                );

                ColorBlock buttonColors = button.colors;
                buttonColors.colorMultiplier = setup.isValid ? 1.0f : 0.8f;
                button.colors = buttonColors;

                chooseButtonGameObjects.Add(chooseButtonCopy);
            }
        }

        protected virtual void RemoveCreatedChooseButtons()
        {
            chooseButtonGameObjects.ForEach(Destroy);
            chooseButtonGameObjects.Clear();
        }
    }
}