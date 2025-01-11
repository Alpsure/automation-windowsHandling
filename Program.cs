using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

    static void Main(string[] args)
    {
        // Check if two arguments are provided
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PopupHandler.exe <PopupTitle> <ButtonText>");
            return;
        }

        string popupTitle = args[0];
        string buttonText = args[1];

        // Retry mechanism
        const int maxRetrySeconds = 5; // Total time to search for the popup
        const int retryDelayMilliseconds = 1000; // Time to wait between retries
        int elapsedMilliseconds = 0;

        bool popupFound = false;

        while (elapsedMilliseconds < maxRetrySeconds * 1000)
        {
            // Start looking for the popup window
            IntPtr desktop = GetDesktopWindow();
            IntPtr child = GetWindow(desktop, GW_CHILD);

            while (child != IntPtr.Zero)
            {
                string windowText = HandleToText(child);

                if (windowText == popupTitle) // Compare with user input
                {
                    popupFound = true;
                    Console.WriteLine($"Popup found: {windowText}");

                    IntPtr subChild = GetWindow(child, GW_CHILD);
                    bool buttonClicked = false;

                    while (subChild != IntPtr.Zero)
                    {
                        string subWindowText = HandleToText(subChild);

                        if (subWindowText == buttonText) // Compare with user input
                        {
                            Console.WriteLine($"Button found: {subWindowText}");

                            // Simulate button click
                            SendMessage(subChild, BM_CLICK, 0, 0);
                            Thread.Sleep(retryDelayMilliseconds);
                            SendMessage(subChild, BM_CLICK, 0, 0);
                            Console.WriteLine("Button clicked!");

                            buttonClicked = true;
                            break;
                        }

                        subChild = GetWindow(subChild, GW_HWNDNEXT);
                    }

                    if (!buttonClicked)
                    {
                        Console.WriteLine("Button not found.");
                    }

                    break;
                }

                child = GetWindow(child, GW_HWNDNEXT);
            }

            if (popupFound)
                break; // Exit loop if popup is found

            // Retry delay
            Thread.Sleep(retryDelayMilliseconds);
            elapsedMilliseconds += retryDelayMilliseconds;

            Console.WriteLine($"Retrying... ({elapsedMilliseconds / 1000}s elapsed)");
        }

        if (!popupFound)
        {
            Console.WriteLine("Popup not found within the timeout period.");
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
