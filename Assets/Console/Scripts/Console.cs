using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using static Console.Console;

namespace Console
{
    public class Console : MonoBehaviour
    {
        public enum StateType
        {
            Closed,
            Closing,
            Opening,
            Open,
            Mini,
            MiniOpening
        }

        [SerializeField]
        private GameObject console;
        [SerializeField]
        private GameObject scrollView;
        [SerializeField]
        private GameObject content;
        [SerializeField]
        private TMP_Text log;
        [SerializeField]
        private TMP_Text scrollIndicator;
        [SerializeField]
        private GameObject borderTop;
        [SerializeField]
        private TMP_InputField inputField;
        [SerializeField]
        private GameObject borderBottom;

        [SerializeField]
        private StateType state;
        [SerializeField]
        private bool scrollDownOnAdding = true;
        [SerializeField, Range(10, 100)]
        private int size = 50;
        [SerializeField, Range(8, 64)]
        private int fontSize = 24;
        [SerializeField, Range(0.0f, 1.0f)]
        private float openingTime = 0.1f;
        [SerializeField, Range(0.0f, 1.0f)]
        private float transparency = 1.0f;

        private static Console instance = null;

        public delegate void CommandCallback(string[] tokens);

        public SortedDictionary<string, CommandCallback> cmdList = new SortedDictionary<string, CommandCallback>(StringComparer.OrdinalIgnoreCase);
        public SortedDictionary<string, IVariable> cvarList = new SortedDictionary<string, IVariable>(StringComparer.OrdinalIgnoreCase);
        
        public interface IVariable
        {
            void Set(object value);
            void SetDefault(object value);
            object Get();
            object GetDefault();
            Type GetDataType();
        }

        public class Variable<T> : IVariable
        {
            public Variable(T value, T defaultValue)
            {
                this.value = value;
                this.defaultValue = defaultValue;
            }

            public Variable(Variable<T> other)
            {
                value = other.value;
                defaultValue = other.defaultValue;
            }

            public void Set(object value)
            {
                this.value = (T)value;
            }

            public void SetDefault(object value)
            {
                this.defaultValue = (T)value;
            }

            public object Get()
            {
                return value;
            }

            public object GetDefault()
            {
                return defaultValue;
            }

            public Type GetDataType()
            {
                return GetType();
            }

            private T value;
            private T defaultValue;
        }

        public static Console Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType(typeof(Console)) as Console;

                return instance;
            }
        }

        public int Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                console.GetComponent<RectTransform>().anchorMin = new Vector2(0.0f, 1.0f / 100 * (100 - size));
            }
        }

        public int FontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                fontSize = value;

                int lines = Instance && Instance.log && (Instance.log.textInfo != null) ? Instance.log.textInfo.lineCount : 0;

                float borderBottomHeight = borderBottom.GetComponent<RectTransform>().rect.height;
                float borderTopHeight = borderTop.GetComponent<RectTransform>().rect.height;
                float inputFieldHeight = inputField.GetComponent<RectTransform>().rect.height;
                float lineSize = lines > 0 ? content.GetComponent<TMP_Text>().preferredHeight / lines : inputField.preferredHeight;

                inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, fontSize * 1.2f);
                inputField.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, borderBottomHeight);
                borderTop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, borderBottomHeight + inputFieldHeight);
                scrollIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, borderBottomHeight + inputFieldHeight + borderTopHeight);
                content.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, log.preferredHeight);

                log.fontSize = fontSize;
                scrollIndicator.fontSize = fontSize;
                inputField.pointSize = fontSize;
            }
        }

        public float OpeningTime
        {
            get
            {
                return openingTime;
            }
            set
            {
                openingTime = value;
            }
        }

        public float Transparency
        {
            get
            {
                return transparency;
            }
            set
            {
                transparency = value;
                Image image = console.GetComponent<Image>();
                image.color = new Color(image.color.r, image.color.g, image.color.b, transparency);
            }
        }

        public StateType State
        {
            get
            {
                return state;
            }
            set
            {
                StopAllCoroutines();

                state = value;
                RectTransform transform = console.GetComponent<RectTransform>();

                switch (state)
                {
                    case StateType.Closed:
                        {
                            transform.anchoredPosition = new Vector2(0.0f, transform.rect.height);
                            inputField.enabled = false;
                            break;
                        }
                    case StateType.Closing:
                    case StateType.Opening:
                    case StateType.MiniOpening:
                        {
                            inputField.enabled = false;
                            StartCoroutine(Move());
                            break;
                        }
                    case StateType.Open:
                    case StateType.Mini:
                        {
                            float inputHeight = borderBottom.GetComponent<RectTransform>().rect.height + inputField.GetComponent<RectTransform>().rect.height + borderTop.GetComponent<RectTransform>().rect.height;
                            transform.anchoredPosition = new Vector2(0.0f, State == StateType.Mini ? transform.rect.height - inputHeight : 0.0f);
                            inputField.enabled = true;
                            inputField.ActivateInputField();
                            break;
                        }
                }
            }
        }

        public static void Add(string line)
        {
            if (Instance && !string.IsNullOrEmpty(line))
            {
                bool isOnBottom = Instance.scrollView.GetComponent<ScrollRect>().verticalNormalizedPosition < 1e-06f;

                if (!string.IsNullOrEmpty(Instance.log.text))
                    Instance.log.text += '\n';

                Instance.log.text += line;
                Instance.content.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, Instance.log.preferredHeight);
                Canvas.ForceUpdateCanvases();

                // Sets scroll view to the bottom
                if (Instance.scrollDownOnAdding || isOnBottom)
                {
                    Instance.scrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.0f;
                    Instance.StartCoroutine(Instance.ScrollDown());
                }
            }
        }

        public static void Clear()
        {
            if (Instance)
            {
                Instance.log.text = "";

                // Collapses content
                Instance.content.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, 0.0f);
            }
        }

        public static void AddCommand(string command, CommandCallback callback)
        {
            if (Instance)
            {
                if (Instance.cmdList.ContainsKey(command))
                {
                    Instance.cmdList[command] += callback;
                }
                else
                {
                    Instance.cmdList.Add(command, callback);
                }
            }
        }

        public static void RemoveCommand(string command)
        {
            if (Instance)
            {
                Instance.cmdList.Remove(command);
            }
        }

        public static void AddVariable<T>(string name, Variable<T> cvar)
        {
            if (Instance)
            {
                if (Instance.cvarList.ContainsKey(name)) return;

                Instance.cvarList.Add(name, new Variable<T>(cvar));
            }
        }

        public static void RemoveVariable<T>(string name, T value, T defaultValue)
        {
            if (Instance)
            {
                Instance.cvarList.Remove(name);
            }
        }

        public static void SetVariable<T>(string name, T value)
        {
            if (Instance)
            {
                Instance.cvarList[name].Set(value);
            }
        }

        public static T GetVariable<T>(string name, T value)
        {
            if (Instance && Instance.cvarList.ContainsKey(name))
            {
                return (T)Instance.cvarList[name].Get();
            }

            return default(T);
        }

        public void CmdList()
        {
            foreach (string cmd in cmdList.Keys)
            {
                Console.Add(cmd);
            }
        }

        public void CvarList()
        {
            foreach (string cmd in cvarList.Keys)
            {
                Console.Add(cmd);
            }
        }

        public void Help()
        {
            if (cmdList.Count != 0)
            {
                Console.Add("Commands:");
                CmdList();
            }

            if (cvarList.Count != 0)
            {
                Console.Add("Variables:");
                CvarList();
            }
        }

        public void Screenshot()
        {
            string scrPath = System.AppDomain.CurrentDomain.BaseDirectory + "/Screenshots";

            if (!System.IO.Directory.Exists(scrPath))
                System.IO.Directory.CreateDirectory(scrPath);

            for (int i = 0; ; i++)
            {
                string fullPath = scrPath + "/Screenshot" + Convert.ToString(i) + ".png";

                if (!System.IO.File.Exists(fullPath))
                {
                    Console.Add("Wrote " + fullPath);
                    ScreenCapture.CaptureScreenshot(fullPath);
                    break;
                }
            }
        }

        void UpdateConsole()
        {
            Size = size;
            FontSize = fontSize;
            Transparency = transparency;
            
            if (state == StateType.Closing)
                State = StateType.Closed;
            else if (state == StateType.Opening)
                State = StateType.Open;
            else
                State = state;
            
            // Sets scroll view to the bottom
            Instance.scrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.0f;
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void OnValidate()
        {
            // Suppresses SendMessage cannot be called during Awake, CheckConsistency, or OnValidate warnings in editor
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) UpdateConsole(); };
#else
            UpdateConsole();
#endif
        }

        void Start()
        {
            AddCommand("Clear", var => Clear());
            AddCommand("CmdList", var => CmdList());
            AddCommand("CvarList", var => CvarList());
            AddCommand("Help", var => Help());
            AddCommand("Screenshot", var => Screenshot());
            AddCommand("Quit", var => Application.Quit());

            AddVariable("Boolean", new Variable<bool>(true, false));
            AddVariable("Number", new Variable<int>(123, 456));
            AddVariable("String", new Variable<string>("John", "Empty"));

            UpdateConsole();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (state == StateType.Closed && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    State = StateType.MiniOpening;
                }
                else if (state == StateType.Closed || state == StateType.Closing)
                {
                    State = StateType.Opening;
                }
                else if (state == StateType.Open || state == StateType.Opening || State == StateType.Mini)
                {
                    State = StateType.Closing;
                }
            }

            if (Input.GetKeyDown(KeyCode.F3)) Instance.scrollView.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0.0f, 0.0f);
        }

        void LateUpdate()
        {
            int lines = Instance.log.textInfo.lineCount;

            float borderTopHeight = borderTop.GetComponent<RectTransform>().rect.height;
            float borderBottomHeight = borderBottom.GetComponent<RectTransform>().rect.height;
            float consoleHeight = console.GetComponent<RectTransform>().rect.height;
            float inputFieldHeight = inputField.GetComponent<RectTransform>().rect.height;
            float textAreaHeight = consoleHeight - scrollIndicator.preferredHeight - borderTopHeight - inputFieldHeight - borderBottomHeight;
            float lineSize = lines > 0 ? content.GetComponent<TMP_Text>().preferredHeight / lines : inputField.preferredHeight;
            int linesVisible = (int)Math.Floor(textAreaHeight / lineSize);

            scrollView.GetComponent<ScrollRect>().scrollSensitivity = lineSize;
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, linesVisible * lineSize);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, content.GetComponent<TMP_Text>().preferredHeight);
        }

        IEnumerator ScrollDown()
        {
            yield return new WaitForEndOfFrame();
            Instance.scrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.0f;
        }

        IEnumerator Move()
        {
            float borderBottomHeight = borderBottom.GetComponent<RectTransform>().rect.height;
            float inputFieldHiegh = inputField.GetComponent<RectTransform>().rect.height;
            float borderTopHeight = borderTop.GetComponent<RectTransform>().rect.height;
            float inputHeight = borderBottomHeight + inputFieldHiegh + borderTopHeight;

            RectTransform transform = console.GetComponent<RectTransform>();
            Vector2 closedPosition = new Vector2(0.0f, transform.rect.height);
            Vector2 openingPosition = new Vector2(0.0f, State == StateType.MiniOpening ? transform.rect.height - inputHeight : 0.0f);
            float openingTime = State != StateType.MiniOpening ? OpeningTime : OpeningTime * (inputHeight / transform.rect.height);
            float distance = 0.0f;

            if (state == StateType.Opening || State == StateType.MiniOpening)
                distance = Vector2.Distance(transform.anchoredPosition, closedPosition);
            else if (state == StateType.Closing)
                distance = Vector2.Distance(transform.anchoredPosition, new Vector2(0.0f, 0.0f));

            float time = distance / transform.rect.height * openingTime;

            while (time < openingTime)
            {
                if (state == StateType.Opening || State == StateType.MiniOpening)
                    transform.anchoredPosition = Vector2.Lerp(closedPosition, openingPosition, time / openingTime);
                else if (state == StateType.Closing)
                    transform.anchoredPosition = Vector2.Lerp(new Vector2(0.0f, 0.0f), closedPosition, time / openingTime);

                time += Time.deltaTime;
                yield return null;
            }

            if (state == StateType.Opening)
                State = StateType.Open;
            else if (state == StateType.MiniOpening)
                State = StateType.Mini;
            else if (state == StateType.Closing)
                State = StateType.Closed;
        }
    }
}
