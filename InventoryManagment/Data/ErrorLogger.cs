using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagment.Data
{
    public class ErrorLogger
    {
        private static readonly string _logFilePath = Path.Combine(FileSystem.AppDataDirectory, "error.log");
        public static void LogError(string message, Exception ex)
        {
            var logMessage = $"{DateTime.UtcNow}: {message}\n{ex}\n\n";
            File.AppendAllText(_logFilePath, logMessage);
        }
    }
}
