using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrimeInputActions
{
    public class InputManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            if (Application.isEditor
                && Application.isPlaying)
            {
                InputManager inputManager = InputManager.Instance;
                if (inputManager != null
                    && inputManager.copyInputActionAssetToPersistentDataPath)
                {
                    inputManager.DeleteInputActionAssetFile();
                }
            }
            inputActionAsset = null;
            pathToInputAction.Clear();
        }

        protected static InputManager instance;
        public static InputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindComponentWithTag<InputManager>("InputManager");
                    if (instance != null)
                    {
                        instance.InitSingleInstance();
                    }
                }

                return instance;
            }
        }

        /**
         * Map of used InputActions.
         * Static reference to allow access without searching for the InputManager, after the InputAction has been used once.
         */
        private static readonly Dictionary<string, ObservableCancelablePriorityInputAction> pathToInputAction = new Dictionary<string, ObservableCancelablePriorityInputAction>();
        
        /**
         * Loaded InputActionAsset.
         * Static reference to be persisted across scenes.
         */
        private static InputActionAsset inputActionAsset;
        
        /**
         * Loaded InputActionAsset.
         * Depending on the configuration, this will be loaded from Application.persistentDataPath.
         * or the defaultInputActionMap will be used.
         */
        private InputActionAsset InputActionAsset
        {
            get
            {
                if (inputActionAsset == null)
                {
                    if (copyInputActionAssetToPersistentDataPath)
                    {
                        string absoluteFilePath = GetInputActionAssetFilePath();
                        if (!File.Exists(absoluteFilePath))
                        {
                            SaveInputActionAssetToFile(defaultInputActionAsset, absoluteFilePath);
                        }
                        inputActionAsset = LoadInputActionAssetFromFile(absoluteFilePath);
                    }
                    else
                    {
                        inputActionAsset = defaultInputActionAsset;
                    }
                    inputActionAsset.Enable();
                }

                return inputActionAsset;
            }
        }
        
        public InputActionAsset defaultInputActionAsset;
        
        /**
         * If true, defaultInputActionAsset will be copied to Application.persistentDataPath such that users can edit it to their preferences.
         */
        public bool copyInputActionAssetToPersistentDataPath;
        public string relativeInputActionAssetInPersistentDataPath = "InputActions.inputactions";
        
        public bool generateConstantsOnResourceChange;
        public string generatedConstantsFolder = "Assets/GeneratedScripts";
        
        public bool useDontDestroyOnLoad;
        
        public bool logInfoInEditMode;
        public bool logInfoInPlayMode = true;
        public bool LogInfoNow => Application.isPlaying && logInfoInPlayMode 
                                  || !Application.isPlaying && logInfoInEditMode;

        protected virtual void Awake()
        {
            InitSingleInstance();
            if (instance != this)
            {
                return;
            }
        }

        private void SaveInputActionAssetToFile(InputActionAsset theInputActionAsset, string absoluteFilePath)
        {
            if (LogInfoNow)
            {
                Debug.Log($"Saving InputActionAsset to '{absoluteFilePath}'");
            }
            
            try
            {
                // Create parent directories recursively.
                Directory.CreateDirectory(Path.Combine(absoluteFilePath, ".."));
                File.WriteAllText(absoluteFilePath, theInputActionAsset.ToJson(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving default InputActionAsset to '{absoluteFilePath}'");
                Debug.LogException(ex);
            }
        }

        private InputActionAsset LoadInputActionAssetFromFile(string absoluteFilePath)
        {
            if (LogInfoNow)
            {
                Debug.Log($"Loading InputActionAsset from '{absoluteFilePath}'");
            }

            try
            {
                string inputActionMapJson = File.ReadAllText(absoluteFilePath, Encoding.UTF8);
                return InputActionAsset.FromJson(inputActionMapJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading InputActionAsset from '{absoluteFilePath}'. Using default InputActionAsset instead.");
                Debug.LogException(ex);
                return defaultInputActionAsset;
            }
        }

        private void DeleteInputActionAssetFile()
        {
            string absoluteFilePath = GetInputActionAssetFilePath();
            try
            {
                if (File.Exists(absoluteFilePath))
                {
                    File.Delete(absoluteFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deleting InputActionAsset '{absoluteFilePath}'");
                Debug.LogException(ex);
            }        
        }
        
        private string GetInputActionAssetFilePath()
        {
            return Application.persistentDataPath + "/" + relativeInputActionAssetInPersistentDataPath;
        }

        public static ObservableCancelablePriorityInputAction GetInputAction(string path)
        {
            if (pathToInputAction.TryGetValue(path, out ObservableCancelablePriorityInputAction observableInputAction))
            {
                return observableInputAction;
            }

            InputAction inputAction = Instance.InputActionAsset.FindAction(path, true);
            // Set the InputManager as Owner for the InputAction.
            // This way, when the InputManager is destroyed, all subscriptions to InputActions will be removed.
            observableInputAction = new ObservableCancelablePriorityInputAction(inputAction, Instance.gameObject);
            pathToInputAction[path] = observableInputAction;
            return observableInputAction;
        }

        public static List<ObservableCancelablePriorityInputAction> GetInputActionsWithSubscribers()
        {
            return pathToInputAction.Values
                .Where(it => it.HasAnySubscribers())
                .ToList();
        }
        
        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                // All subscriptions to InputActions are removed when the InputManager is destroyed.
                // Thus, the map can be cleared.
                pathToInputAction.Clear();
            }
        }
        
        private void InitSingleInstance()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (instance != null
                && instance != this)
            {
                // This instance is not needed.
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (useDontDestroyOnLoad)
            {
                // Move object to top level in scene hierarchy.
                // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used.
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
        
        /**
         * Searches the GameObject with the given tag
         * and attempts to get its Component that is specified by the generic type parameter.
         */
        private static T FindComponentWithTag<T>(string tag)
        {
            T component;
            GameObject obj = GameObject.FindGameObjectWithTag(tag);
            if (obj)
            {
                component = obj.GetComponent<T>();
                if (component == null)
                {
                    Debug.LogError($"Did not find Component '{typeof(T)}' in GameObject with tag '{tag}'.", obj);
                }
                return component;
            }

            return default(T);
        }
    }
}
