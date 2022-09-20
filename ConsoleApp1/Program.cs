// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Timers;
using SharpAdbClient;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Timers.Timer;

internal class Program

{
    private static int batteryLevel = 0;
    private static AdbClient client;
    private static DeviceData device;
    private static string FILE_PATH = Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\stats") + "\\";
    private static void Main(string[] args)
    {
        AdbServer server = new AdbServer();
        var result = server.StartServer(@"D:\platform-tools_r33.0.3-windows\platform-tools\adb.exe", restartServerIfNewer: false);
        client = new AdbClient();
        device = client.GetDevices().First();
        var receiver = new ConsoleOutputReceiver();
        List<string> appList = getAppList(receiver);
        Random r = new Random();
        int randomAppIndex = r.Next(0, appList.Count() - 1);
        string command = $"monkey -p {appList[randomAppIndex]} -c android.intent.category.LAUNCHER 1";
        Console.WriteLine($"Preparing to open the application '{appList[randomAppIndex]}' on the mobile phone");
        client.ExecuteRemoteCommandAsync(command, device, receiver, CancellationToken.None);
        var receiver2 = new ConsoleOutputReceiver();
        ExecuteBackgroundTaskToGetBatteryInfo($"{FILE_PATH}{device.Model}-{device.Serial}-BatteryInfo.txt");
        Console.WriteLine("Press the Enter key to exit the program at any time... ");
        Console.ReadLine();
    }

    private static async Task ExecuteBackgroundTaskToGetBatteryInfo(string filePath)
    {
        while (true)
        {
            Task.Run(() =>
            {
                getBatteryLevel();
                Console.WriteLine("Field Battery: " + batteryLevel);
                writeBatteryLevelToFile(filePath);
            });
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    private static void writeBatteryLevelToFile(string filePath)
    {
        // This text is added only once to the file.
        if (!File.Exists(filePath))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine($"Battery statistics for {device.Model}-{device.Serial}");
                sw.WriteLine($"{DateTime.Now} : Battery Level is {batteryLevel}%");
                sw.WriteLine("-----------------------------------------------------");
            }
        }

        // This text is always added, making the file longer over time
        // if it is not deleted.
        using (StreamWriter sw = File.AppendText(filePath))
        {
            sw.WriteLine($"{DateTime.Now} : Battery Level is {batteryLevel}%");
            sw.WriteLine("-----------------------------------------------------");
        }
    }

    private static void getBatteryLevel()
    {
        var receiver = new ConsoleOutputReceiver();
        client.ExecuteRemoteCommand("dumpsys battery | grep level", device, receiver);
        string result = receiver.ToString().Trim();
        batteryLevel = int.Parse(result.Replace("level:", ""));
    }

    private static List<string> getAppList(ConsoleOutputReceiver receiver)
    {
        client.ExecuteRemoteCommand("pm list packages -3", device, receiver);
        List<string> listOfAppNames = new List<string>(receiver.ToString().Trim().Split("package:"));
        listOfAppNames.Remove("");
        for (int i = 0; i < listOfAppNames.Count; i++)
        {
            listOfAppNames[i] = listOfAppNames[i].Trim();
        }
        return listOfAppNames;
    }
}