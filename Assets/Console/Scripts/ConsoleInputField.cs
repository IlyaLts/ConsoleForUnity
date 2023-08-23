using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Console
{
    public class ConsoleInputField : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField inputField;
        [SerializeField]
        private TMP_Text scrollIndicator;
        [SerializeField]
        private TMP_Text content;

        private int caretPosition;
        private int prevCommandPos;
        private List<string> prevCommands = new List<string>();

        private const float initialDelay = 0.5f;
        private const float subsequentDelay = 0.05f;

        private Coroutine prevCommand;
        private Coroutine nextCommand;

        void Start()
        {
            // Excludes adding backquotes when opening/closing console
            inputField.onValidateInput += delegate (string text, int charIndex, char addedChar) { return Input.GetKeyDown(KeyCode.BackQuote) ? '\0' : addedChar; };
        }

        void Update()
        {
            if (inputField.IsActive())
            {
                inputField.ActivateInputField();

                // Next/Previous commands
                if (Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
                    prevCommand = StartCoroutine(PrevCommand());

                if (Input.GetKeyDown(KeyCode.DownArrow) && !Input.GetKey(KeyCode.UpArrow))
                    nextCommand = StartCoroutine(NextCommand());

                if (Input.GetKeyUp(KeyCode.UpArrow))
                    StopCoroutine(prevCommand);

                if (Input.GetKeyUp(KeyCode.DownArrow))
                    StopCoroutine(nextCommand);

                // Prevents the caret from moving when using up/down keys
                if (!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
                    caretPosition = inputField.caretPosition;

                if (!string.IsNullOrEmpty(inputField.text))
                {
                    string[] tokens = inputField.text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    // Search for a command/variable
                    if (Input.GetKeyDown(KeyCode.Tab) && tokens.Length == 1)
                    {
                        Console.Add("\\" + inputField.text);

                        foreach (string cmd in Console.Instance.cmdList.Keys)
                            if (cmd.StartsWith(inputField.text, StringComparison.OrdinalIgnoreCase))
                                Console.Add(cmd);

                        foreach (string cvar in Console.Instance.cvarList.Keys)
                            if (cvar.StartsWith(inputField.text, StringComparison.OrdinalIgnoreCase))
                                Console.Add("    " + cvar + " = \"" + Console.Instance.cvarList[cvar].Get() + "\"");
                    }

                    // Enter a command
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        prevCommands.Add(inputField.text);
                        prevCommandPos = prevCommands.Count;

                        Console.Add("\\" + inputField.text);

                        if (tokens.Length > 0 && Console.Instance.cmdList.ContainsKey(tokens[0]))
                        {
                            string[] args = new string[tokens.Length - 1];

                            for (int i = 1; i < tokens.Length; i++)
                            {
                                args[i - 1] = tokens[i];
                            }

                            Console.Instance.cmdList[tokens[0]].Invoke(args);
                        }
                        else if (tokens.Length > 0 && Console.Instance.cvarList.ContainsKey(tokens[0]))
                        {
                            if (tokens.Length == 1)
                            {
                                var cvar = Console.Instance.cvarList[tokens[0]];
                                Console.Add("\"" + tokens[0] + "\" is: \"" + cvar.Get() + "\" Default: \"" + cvar.GetDefault() + "\"");
                            }
                            else
                            {
                                Type type = Console.Instance.cvarList[tokens[0]].Get().GetType();
                                Console.Instance.cvarList[tokens[0]].Set(Convert.ChangeType(tokens[1], type));
                            }
                        }
                        else
                        {
                            Console.Add("Unknown command \"" + tokens[0] + "\"");
                        }

                        inputField.text = "";
                        inputField.ActivateInputField();
                    }
                }
            }
        }

        void LateUpdate()
        {
            // Prevents the caret from moving when using up/down keys
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                inputField.caretPosition = caretPosition;
        }

        IEnumerator PrevCommand()
        {
            float delay = initialDelay;

            while (Input.GetKey(KeyCode.UpArrow))
            {
                if (prevCommandPos > 0)
                {
                    prevCommandPos--;
                    inputField.text = prevCommands[prevCommandPos];
                    inputField.MoveToEndOfLine(false, false);
                    caretPosition = inputField.caretPosition;
                }

                yield return new WaitForSeconds(delay);
                delay = subsequentDelay;
            }
        }

        IEnumerator NextCommand()
        {
            float delay = initialDelay;

            while (Input.GetKey(KeyCode.DownArrow))
            {
                if (prevCommandPos < prevCommands.Count)
                {
                    prevCommandPos++;
                    inputField.text = prevCommandPos < prevCommands.Count ? prevCommands[prevCommandPos] : "";
                    inputField.MoveToEndOfLine(false, false);
                    caretPosition = inputField.caretPosition;
                }

                yield return new WaitForSeconds(delay);
                delay = subsequentDelay;
            }
        }
    }
}
