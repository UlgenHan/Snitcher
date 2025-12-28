using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.WorkSpaces
{
    public partial class Project : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _workspaceId = string.Empty;

        [ObservableProperty]
        private string _namespaceId = string.Empty;

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;

        [ObservableProperty]
        private DateTime _updatedAt = DateTime.Now;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _path = string.Empty;

        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]
        private DateTime? _lastAnalyzedAt;

        /// <summary>
        /// Gets the display name for the project.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unnamed Project" : Name;

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
        /// Gets the status text based on analysis state.
        /// </summary>
        public string StatusText
        {
            get
            {
                if (LastAnalyzedAt.HasValue)
                {
                    var timeSince = DateTime.Now - LastAnalyzedAt.Value;
                    if (timeSince.TotalMinutes < 60)
                        return $"Analyzed {timeSince.Minutes} minute{(timeSince.Minutes != 1 ? "s" : "")} ago";
                    else if (timeSince.TotalHours < 24)
                        return $"Analyzed {timeSince.Hours} hour{(timeSince.Hours != 1 ? "s" : "")} ago";
                    else
                        return $"Analyzed {timeSince.Days} day{(timeSince.Days != 1 ? "s" : "")} ago";
                }
                return "Not analyzed";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the project has been analyzed.
        /// </summary>
        public bool IsAnalyzed => LastAnalyzedAt.HasValue;

        /// <summary>
        /// Creates a copy of this project for editing.
        /// </summary>
        /// <returns>A copy of the project</returns>
        public Project Clone()
        {
            return new Project
            {
                Id = Id,
                Name = Name,
                Description = Description,
                WorkspaceId = WorkspaceId,
                NamespaceId = NamespaceId,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Path = Path,
                Version = Version,
                LastAnalyzedAt = LastAnalyzedAt
            };
        }

        /// <summary>
        /// Updates the project properties from another project.
        /// </summary>
        /// <param name="other">The project to copy from</param>
        public void UpdateFrom(Project other)
        {
            if (other == null) return;

            Name = other.Name;
            Description = other.Description;
            UpdatedAt = other.UpdatedAt;
            Path = other.Path;
            Version = other.Version;
            LastAnalyzedAt = other.LastAnalyzedAt;
        }

        /// <summary>
        /// Updates the last analyzed timestamp to now.
        /// </summary>
        public void UpdateAnalyzedTimestamp()
        {
            LastAnalyzedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
