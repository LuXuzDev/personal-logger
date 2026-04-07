using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LuxuzDev.PersonalLogger;

public static class PersonalLogger
{
    private static string _logFilePath =
        Path.Combine(AppContext.BaseDirectory, "Logs", "personal.log");

    private static Process? _consoleProcess;
    private static bool _initialized = false;
    private static TelegramService _service;
    
    

    private static IExternalNotifier? _notifier;

    public static void Configure(IExternalNotifier notifier)
    {
        _notifier = notifier;
    }

    public static void Initialize(string? logFilePath = null)
    {
        if (_initialized) return;

        if (!string.IsNullOrWhiteSpace(logFilePath))
            _logFilePath = logFilePath;

        // Crear carpeta de logs
        var logDir = Path.GetDirectoryName(_logFilePath)!;
        Directory.CreateDirectory(logDir);
        
        if(service is not null)_service = service;

        _initialized = true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StartConsole();
            System.Threading.Thread.Sleep(1000);
        }

       

    }

    public static void Log(string message, LogType type = LogType.Info, bool notify = false)
    {
        if (!_initialized)
            Initialize();

        string typeName = type.ToString().ToUpper();
        string logMessage = $"[{typeName}] {DateTime.Now:dd/MM/yyyy HH:mm:ss} {message}";

        // Consola principal (sin colores)
        Console.WriteLine(logMessage);

        // Guardar en archivo UTF-8
        File.AppendAllText(_logFilePath, logMessage + Environment.NewLine, Encoding.UTF8);

        if (notify && _notifier != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notifier.NotifyAsync(logMessage);
                }
                catch (Exception ex)
                {
                    PersonalLogger.Log($"[LOGGER ERROR] Enviar notificacion fallida: {ex.Message}",LogType.Error);
                }
            });
        }
    }
    
    public static void Log(string message, bool sendTelegram, ITelegramBotSettings telegramBotSettings,
        string endpoint,string path, string method,LogType type = LogType.Info)
    {
        
        Log(message, type);

        // 2. Envío a Telegram si se solicita y el servicio existe
        if (sendTelegram && _service != null)
        {
            
            Task.Run(async () => {
                try {
                    await _service.SendAlertAsync(message,endpoint,path,method);
                } catch { }
            });
        }
        
    }

    private static void StartConsole()
    {
        if (_consoleProcess != null && !_consoleProcess.HasExited)
            return;

        try
        {
            // Script de PowerShell para colorear según tipo
            string psScript = @"
Get-Content -Path '" + _logFilePath + @"' -Wait -Tail 0 |
ForEach-Object {
    $line = $_
    if ($line.StartsWith('[INFO]')) { Write-Host $line -ForegroundColor Cyan }
    elseif ($line.StartsWith('[SUCCESS]')) { Write-Host $line -ForegroundColor Green }
    elseif ($line.StartsWith('[WARNING]')) { Write-Host $line -ForegroundColor Yellow }
    elseif ($line.StartsWith('[ERROR]')) { Write-Host $line -ForegroundColor Red }
    elseif ($line.StartsWith('[DEBUG]')) { Write-Host $line -ForegroundColor Gray }
    else { Write-Host $line }
}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{psScript}\"",
                UseShellExecute = true, // Ventana independiente
                CreateNoWindow = false
            };

            _consoleProcess = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ No se pudo abrir la consola PowerShell extra: {ex.Message}");
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                if (_consoleProcess != null && !_consoleProcess.HasExited)
                    _consoleProcess.Kill();
            }
            catch { }
        };
    }
}
