using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snitcher.UI.Desktop.Models.WorkSpaces;

namespace Snitcher.UI.Desktop.Services.WorkSpace
{
    public class WorkspaceManagerService
    {
        private readonly string _workspacePath;
        private List<Workspace> _workspaces;
        public WorkspaceManagerService()
        {
            _workspacePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snitcher", "Workspaces");
            _workspaces = new List<Workspace>();

            Directory.CreateDirectory(_workspacePath);
            LoadWorkspaces();
        }
        public IEnumerable<Workspace> GetWorkspaces() => _workspaces;
        public Workspace CreateWorkspace(string name, string description = "")
        {
            var workspace = new Workspace
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now
            };
            _workspaces.Add(workspace);
            SaveWorkspace(workspace);

            return workspace;
        }
        public Project CreateProject(string workspaceId, string name, string description = "")
        {
            var workspace = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (workspace == null) throw new ArgumentException("Workspace not found");
            var project = new Project
            {
                Name = name,
                Description = description,
                WorkspaceId = workspaceId,
                CreatedAt = DateTime.Now
            };
            workspace.Projects.Add(project);
            SaveWorkspace(workspace);

            return project;
        }
        private void LoadWorkspaces()
        {
            _workspaces.Clear();

            foreach (var file in Directory.GetFiles(_workspacePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var workspace = JsonSerializer.Deserialize<Workspace>(json);
                    if (workspace != null)
                    {
                        _workspaces.Add(workspace);
                    }
                }
                catch
                {
                    // Skip corrupted files
                }
            }
        }
        private void SaveWorkspace(Workspace workspace)
        {
            var filePath = Path.Combine(_workspacePath, $"{workspace.Id}.json");
            var json = JsonSerializer.Serialize(workspace, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
