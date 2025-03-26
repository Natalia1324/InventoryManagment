using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryManagment.Data
{
    public class MegaService
    {
        private readonly MegaApiClient _client = new MegaApiClient();
        private readonly string _credentialsPath = Path.Combine(FileSystem.AppDataDirectory, "credentials.json");
        private string _email = string.Empty;
        private string _password = string.Empty;

        public void Login()
        {
            try
            {
                LoadCredentials();
                _client.Login(_email, _password);
                Console.WriteLine("Logged in to MEGA as " + _email);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z logowaniem do MEGA", ex);
            }
        }

        private void LoadCredentials()
        {
            if (!File.Exists(_credentialsPath))
                throw new FileNotFoundException("Credentials file not found at: " + _credentialsPath);

            var json = File.ReadAllText(_credentialsPath);
            var doc = JsonDocument.Parse(json);
            _email = doc.RootElement.GetProperty("email").GetString() ?? throw new Exception("Email not found in credentials.");
            _password = doc.RootElement.GetProperty("password").GetString() ?? throw new Exception("Password not found in credentials.");
        }

        public void UploadFile(string localFilePath)
        {
            try
            {
                string todayFolderName = DateTime.Now.ToString("yyyy-MM-dd");

                // Pobierz wszystkie foldery w root
                var nodes = _client.GetNodes();
                var root = nodes.Single(n => n.Type == NodeType.Root);

                // Szukaj folderu z dzisiejszą datą
                var dateFolder = nodes
                    .Where(n => n.Type == NodeType.Directory && n.ParentId == root.Id)
                    .FirstOrDefault(n => n.Name == todayFolderName);

                // Jeśli folder nie istnieje – utwórz go
                if (dateFolder == null)
                {
                    dateFolder = _client.CreateFolder(todayFolderName, root);
                }

                // Upload pliku do folderu z dzisiejszą datą
                var uploadedNode = _client.UploadFile(localFilePath, dateFolder);
                var link = _client.GetDownloadLink(uploadedNode);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z wrzucaniem pliku do MEGA", ex);
            }
        }


        public void Logout()
        {
            try { 
            _client.Logout();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Problem z wylogowaniem z MEGA", ex);
            }
        }
    }
}
