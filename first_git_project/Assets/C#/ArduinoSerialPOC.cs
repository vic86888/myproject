using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

public class ArduinoSerialPOC : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "/dev/cu.usbmodem114401"; // macOS 範例
    // public string portName = "COM3";              // Windows 範例
    public int baudRate = 9600;

    [Header("Button Mapping")]
    public string defaultButtonName = "BUTTON";

    private static ArduinoSerialPOC instance;

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning;

    private readonly object buttonStateLock = new object();
    private readonly Dictionary<string, bool> rawButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> buttonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> previousButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public static bool GetButton()
    {
        return GetButton(null);
    }

    public static bool GetButton(string buttonName)
    {
        if (instance == null)
        {
            return false;
        }

        return instance.GetButtonState(instance.buttonStates, buttonName);
    }

    public static bool GetButtonDown()
    {
        return GetButtonDown(null);
    }

    public static bool GetButtonDown(string buttonName)
    {
        if (instance == null)
        {
            return false;
        }

        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return !previous && current;
    }

    public static bool GetButtonUp()
    {
        return GetButtonUp(null);
    }

    public static bool GetButtonUp(string buttonName)
    {
        if (instance == null)
        {
            return false;
        }

        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return previous && !current;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;

        try
        {
            serialPort.Open();
            isRunning = true;

            readThread = new Thread(ReadSerialLoop);
            readThread.Start();

            Debug.Log("Serial connected: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Serial open failed: " + e.Message);
        }
    }

    void Update()
    {
        SyncButtonStates();

        // === Arduino Button -> Unity Input ===
        if (GetButtonDown())
        {
            Debug.Log("Unity sees button down: " + ResolveButtonName(null));
        }

        if (GetButtonUp())
        {
            Debug.Log("Unity sees button up: " + ResolveButtonName(null));
        }

        // === Unity -> Arduino LED ===
        if (Input.GetKeyDown(KeyCode.L))
        {
            SendCommand("LED_ON");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            SendCommand("LED_OFF");
        }

    }

    public void SendCommand(string command)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.WriteLine(command);
            Debug.Log("Sent: " + command);
        }
    }

    void ReadSerialLoop()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string message = serialPort.ReadLine();
                message = message.Trim();

                if (TryParseButtonMessage(message, out string buttonName, out bool isPressed))
                {
                    SetRawButtonState(buttonName, isPressed);
                    Debug.Log(message);
                }
            }
            catch (System.TimeoutException)
            {
                // ignore
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Serial read error: " + e.Message);
            }
        }
    }



    void OnDestroy()
    {
        Shutdown();
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }

    private void Shutdown()
    {
        isRunning = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void SyncButtonStates()
    {
        previousButtonStates.Clear();

        foreach (KeyValuePair<string, bool> pair in buttonStates)
        {
            previousButtonStates[pair.Key] = pair.Value;
        }

        lock (buttonStateLock)
        {
            foreach (KeyValuePair<string, bool> pair in rawButtonStates)
            {
                buttonStates[pair.Key] = pair.Value;
            }
        }
    }

    private void SetRawButtonState(string buttonName, bool isPressed)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);

        lock (buttonStateLock)
        {
            rawButtonStates[resolvedButtonName] = isPressed;
        }
    }

    private bool GetButtonState(Dictionary<string, bool> states, string buttonName)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);
        return states.TryGetValue(resolvedButtonName, out bool isPressed) && isPressed;
    }

    private string ResolveButtonName(string buttonName)
    {
        return string.IsNullOrWhiteSpace(buttonName) ? defaultButtonName : buttonName.Trim();
    }

    private bool TryParseButtonMessage(string message, out string buttonName, out bool isPressed)
    {
        buttonName = null;
        isPressed = false;

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        string[] parts = message.Split('_');
        if (parts.Length < 2)
        {
            return false;
        }

        string action = parts[parts.Length - 1];
        if (action.Equals("DOWN", StringComparison.OrdinalIgnoreCase))
        {
            isPressed = true;
        }
        else if (action.Equals("UP", StringComparison.OrdinalIgnoreCase))
        {
            isPressed = false;
        }
        else
        {
            return false;
        }

        if (parts.Length == 2)
        {
            buttonName = parts[0];
            return true;
        }

        buttonName = string.Join("_", parts, 0, parts.Length - 1);
        return true;
    }
}