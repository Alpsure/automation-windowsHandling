// program.exe "window name" "button name" optional_maxRetries optional_sleepAfterAction

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Program
{
    // Win32 API constants
    private const int GW_CHILD = 5;
    private const int GW_HWNDNEXT = 2;
    private const uint BM_CLICK = 0x00F5;
    private const uint WM_GETTEXT = 0x000D;
    private const uint WM_GETTEXTLENGTH = 0x000E;

    // Import necessary functions from user32.dll
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    static void Main(string[] args)
    {
        // Check if at least two arguments are provided
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PopupHandler.exe <PopupTitle> <ButtonText> [maxTries] [sleepAfterAction]");
            return;
        }

        string popupTitle = args[0];
        string buttonText = args[1];

        const int retryDelayMilliseconds = 1000;

        // Parse optional arguments or set default values
        int maxTries = args.Length > 2 && int.TryParse(args[2], out int tries) ? tries : 10;
        int sleepAfterAction = args.Length > 3 && int.TryParse(args[3], out int sleep) ? sleep : 500;

        Console.WriteLine($"PopupTitle: {popupTitle}, ButtonText: {buttonText}, maxTries: {maxTries}, sleepAfterAction: {sleepAfterAction}ms");

        int elapsedTries = 0;
        bool popupFound = false;

        while (elapsedTries < maxTries)
        {
            // Start looking for the popup window
            IntPtr desktop = GetDesktopWindow();
            IntPtr child = GetWindow(desktop, GW_CHILD);

            while (child != IntPtr.Zero)
            {
                string windowText = HandleToText(child);

                // Console.WriteLine($"Checking window with title: {windowText}"); // Debug log to see all window titles

                if (!string.IsNullOrEmpty(windowText) && windowText.Contains(popupTitle, StringComparison.OrdinalIgnoreCase))
                {
                    popupFound = true;
                    Console.WriteLine($"Popup found: {windowText}");

                    // Bring the popup to the foreground
                    if (SetForegroundWindow(child))
                    {
                        Console.WriteLine("Popup window brought to the foreground successfully.");
                        Thread.Sleep(sleepAfterAction);
                    }
                    else
                    {
                        Console.WriteLine("Failed to bring popup window to the foreground.");
                    }

                    IntPtr subChild = GetWindow(child, GW_CHILD);
                    bool buttonClicked = false;

                    var foundButtons = new List<string>(); // To store all found button names

                    while (subChild != IntPtr.Zero)
                    {
                        string subWindowText = HandleToText(subChild);

                        if (!string.IsNullOrEmpty(subWindowText))
                        {
                            foundButtons.Add(subWindowText); // Collect button names
                        }

                        if (subWindowText == buttonText)
                        {
                            Console.WriteLine($"Button found: {subWindowText}");

                            // Simulate button click
                            SendMessage(subChild, BM_CLICK, 0, 0);
                            Thread.Sleep(sleepAfterAction);

                            Console.WriteLine("Button clicked!");

                            buttonClicked = true;
                            break;
                        }

                        subChild = GetWindow(subChild, GW_HWNDNEXT);
                    }

                    if (!buttonClicked)
                    {
                        Console.WriteLine("Button not found. Here are all the buttons found in the popup:");
                        foreach (string btn in foundButtons)
                        {
                            Console.WriteLine($"- {btn}");
                        }
                    }

                    break;
                }

                child = GetWindow(child, GW_HWNDNEXT);
            }

            if (popupFound)
                break; // Exit loop if popup is found

            // Retry delay
            Thread.Sleep(retryDelayMilliseconds);
            elapsedTries++;

            Console.WriteLine($"Retrying... ({elapsedTries}/{maxTries} attempts)");
        }

        if (!popupFound)
        {
            Console.WriteLine("Popup not found within the maximum number of retries.");
        }
    }

    // Method to get text from a window handle
    private static string HandleToText(IntPtr hWnd)
    {
        int length = SendMessage(hWnd, WM_GETTEXTLENGTH, 0, 0);
        if (length == 0) return string.Empty;

        StringBuilder sb = new StringBuilder(length + 1);
        SendMessage(hWnd, WM_GETTEXT, sb.Capacity, sb);
        return sb.ToString();
    }
}
