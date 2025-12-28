using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.WorkSpaces
{
    public partial class Workspace : ObservableObject
    {
        [ObservableProperty]
        private string _id = "";

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private DateTime _createdAt;

        [ObservableProperty]
        private DateTime _updatedAt;

        [ObservableProperty]
        private bool _isDefault = false;

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private ObservableCollection<Namespace> _namespaces = new();

        /// <summary>
        /// Gets or sets a value indicating whether this workspace is selected in the UI.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected = false;

        /// <summary>
        /// Gets or sets a value indicating whether this workspace is being loaded.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        /// <summary>
        /// Gets the display name for the workspace.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unnamed Workspace" : Name;

        /// <summary>
        /// Gets the project count display text.
        /// </summary>
        public string ProjectCountText => $"{Projects.Count} project{(Projects.Count != 1 ? "s" : "")}";

        /// <summary>
        /// Gets the formatted updated date for display.
        /// </summary>
        public string UpdatedAtFormatted => UpdatedAt.ToString("MMMM d, yyyy");

        /// <summary>
        /// Gets the relative time since last update.
        /// </summary>
        public string UpdatedAtRelative
        {
            get
            {
                var timeSince = DateTime.Now - UpdatedAt;
                if (timeSince.TotalDays < 1)
                    return "Updated today";
                else if (timeSince.TotalDays < 7)
                    return $"Updated {timeSince.Days} day{(timeSince.Days != 1 ? "s" : "")} ago";
                else if (timeSince.TotalDays < 30)
                    return $"Updated {timeSince.Days / 7} week{(timeSince.Days / 7 != 1 ? "s" : "")} ago";
                else
                    return $"Updated {UpdatedAt:MMMM d}";
            }
        }

        /// <summary>
        /// Creates a copy of this workspace for editing.
        /// </summary>
        /// <returns>A copy of the workspace</returns>
        public Workspace Clone()
        {
            return new Workspace
            {
                Id = Id,
                Name = Name,
                Description = Description,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                IsDefault = IsDefault,
                Projects = new ObservableCollection<Project>(Projects),
                Namespaces = new ObservableCollection<Namespace>(Namespaces)
            };
        }

        /// <summary>
        /// Updates the workspace properties from another workspace.
        /// </summary>
        /// <param name="other">The workspace to copy from</param>
        public void UpdateFrom(Workspace other)
        {
            if (other == null) return;

            Name = other.Name;
            Description = other.Description;
            UpdatedAt = other.UpdatedAt;
            
            // Update projects collection
            Projects.Clear();
            foreach (var project in other.Projects)
            {
                Projects.Add(project);
            }

            // Update namespaces collection
            Namespaces.Clear();
            foreach (var ns in other.Namespaces)
            {
                Namespaces.Add(ns);
            }
        }
    }
}
