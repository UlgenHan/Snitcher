using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.WorkSpaces
{
    public partial class Namespace : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _workspaceId = string.Empty;

        [ObservableProperty]
        private string? _parentNamespaceId;

        [ObservableProperty]
        private int _depth = 0;

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private ObservableCollection<Namespace> _childNamespaces = new();

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;

        [ObservableProperty]
        private DateTime _updatedAt = DateTime.Now;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private bool _isExpanded = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private DateTime? _lastAnalyzedAt;

        /// <summary>
        /// Gets the display name for the namespace.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unnamed Namespace" : Name;

        /// <summary>
        /// Gets the full path display name.
        /// </summary>
        public string FullDisplayName => string.IsNullOrWhiteSpace(FullName) ? Name : FullName;

        /// <summary>
        /// Gets the indented name for tree view display.
        /// </summary>
        public string IndentedName => new string(' ', Depth * 4) + DisplayName;

        /// <summary>
        /// Gets the total count of items in this namespace (projects + child namespaces).
        /// </summary>
        public int TotalItemCount => Projects.Count + ChildNamespaces.Sum(ns => ns.TotalItemCount);

        /// <summary>
        /// Gets a value indicating whether this namespace has children.
        /// </summary>
        public bool HasChildren => Projects.Count > 0 || ChildNamespaces.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this is a root namespace.
        /// </summary>
        public bool IsRootNamespace => string.IsNullOrEmpty(ParentNamespaceId);

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
        /// Gets a value indicating whether the namespace has been analyzed.
        /// </summary>
        public bool IsAnalyzed => LastAnalyzedAt.HasValue;

        /// <summary>
        /// Creates a copy of this namespace for editing.
        /// </summary>
        /// <returns>A copy of the namespace</returns>
        public Namespace Clone()
        {
            return new Namespace
            {
                Id = Id,
                Name = Name,
                FullName = FullName,
                WorkspaceId = WorkspaceId,
                ParentNamespaceId = ParentNamespaceId,
                Depth = Depth,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                LastAnalyzedAt = LastAnalyzedAt
            };
        }

        /// <summary>
        /// Updates the namespace properties from another namespace.
        /// </summary>
        /// <param name="other">The namespace to copy from</param>
        public void UpdateFrom(Namespace other)
        {
            if (other == null) return;

            Name = other.Name;
            FullName = other.FullName;
            UpdatedAt = other.UpdatedAt;
            LastAnalyzedAt = other.LastAnalyzedAt;
            
            // Update collections
            Projects.Clear();
            foreach (var project in other.Projects)
            {
                Projects.Add(project);
            }

            ChildNamespaces.Clear();
            foreach (var childNs in other.ChildNamespaces)
            {
                ChildNamespaces.Add(childNs);
            }
        }

        /// <summary>
        /// Adds a child namespace to this namespace.
        /// </summary>
        /// <param name="childNamespace">The child namespace to add</param>
        public void AddChildNamespace(Namespace childNamespace)
        {
            if (childNamespace != null && !ChildNamespaces.Contains(childNamespace))
            {
                childNamespace.ParentNamespaceId = Id;
                childNamespace.Depth = Depth + 1;
                ChildNamespaces.Add(childNamespace);
            }
        }

        /// <summary>
        /// Removes a child namespace from this namespace.
        /// </summary>
        /// <param name="childNamespace">The child namespace to remove</param>
        public void RemoveChildNamespace(Namespace childNamespace)
        {
            if (childNamespace != null && ChildNamespaces.Contains(childNamespace))
            {
                ChildNamespaces.Remove(childNamespace);
                childNamespace.ParentNamespaceId = null;
                childNamespace.Depth = 0;
            }
        }

        /// <summary>
        /// Updates the last analyzed timestamp to now.
        /// </summary>
        public void UpdateAnalyzedTimestamp()
        {
            LastAnalyzedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Gets all descendant namespaces recursively.
        /// </summary>
        /// <returns>All descendant namespaces</returns>
        public IEnumerable<Namespace> GetAllDescendants()
        {
            var descendants = new List<Namespace>();
            
            foreach (var child in ChildNamespaces)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetAllDescendants());
            }
            
            return descendants;
        }
    }
}
