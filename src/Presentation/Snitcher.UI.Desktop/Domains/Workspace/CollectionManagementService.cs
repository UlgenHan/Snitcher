using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snitcher.UI.Desktop.Models.Network;

namespace Snitcher.UI.Desktop.Services.WorkSpace
{
    public class CollectionManagementService
    {
        private readonly string _collectionsPath;
        private Dictionary<string, RequestCollection> _collections;
        public CollectionManagementService()
        {
            _collectionsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snitcher", "Collections");
            _collections = new Dictionary<string, RequestCollection>();

            Directory.CreateDirectory(_collectionsPath);
            LoadCollections();
        }
        public RequestCollection CreateCollection(string projectId, string name, string description = "")
        {
            var collection = new RequestCollection
            {
                Name = name,
                Description = description,
                ProjectId = projectId
            };
            _collections[collection.Id] = collection;
            SaveCollection(collection);

            return collection;
        }
        public void AddRequestToCollection(string collectionId, HttpRequest request)
        {
            if (_collections.TryGetValue(collectionId, out var collection))
            {
                collection.Requests.Add(request);
                SaveCollection(collection);
            }
        }
        private void LoadCollections()
        {
            _collections.Clear();

            foreach (var file in Directory.GetFiles(_collectionsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var collection = JsonSerializer.Deserialize<RequestCollection>(json);
                    if (collection != null)
                    {
                        _collections[collection.Id] = collection;
                    }
                }
                catch
                {
                    // Skip corrupted files
                }
            }
        }
        private void SaveCollection(RequestCollection collection)
        {
            var filePath = Path.Combine(_collectionsPath, $"{collection.Id}.json");
            var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
