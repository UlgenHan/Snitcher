using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Snitcher.UI.Desktop.Models
{
    public partial class RequestCollection : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private ObservableCollection<HttpRequest> _requests = new();

        [ObservableProperty]
        private ObservableCollection<RequestCollection> _subCollections = new();

        [ObservableProperty]
        private RequestCollection? _parentCollection;

        [ObservableProperty]
        private bool _isExpanded = true;

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;

        [ObservableProperty]
        private DateTime _updatedAt = DateTime.Now;

        [ObservableProperty]
        private Environment _environment = new();

        public RequestCollection()
        {
        }

        public RequestCollection(string name, string description = "")
        {
            Name = name;
            Description = description;
        }

        public int TotalRequestsCount
        {
            get
            {
                var count = Requests.Count;
                foreach (var subCollection in SubCollections)
                {
                    count += subCollection.TotalRequestsCount;
                }
                return count;
            }
        }

        public void AddRequest(HttpRequest request)
        {
            Requests.Add(request);
            UpdatedAt = DateTime.Now;
        }

        public void RemoveRequest(HttpRequest request)
        {
            Requests.Remove(request);
            UpdatedAt = DateTime.Now;
        }

        public void AddSubCollection(RequestCollection subCollection)
        {
            subCollection.ParentCollection = this;
            SubCollections.Add(subCollection);
            UpdatedAt = DateTime.Now;
        }

        public void RemoveSubCollection(RequestCollection subCollection)
        {
            subCollection.ParentCollection = null;
            SubCollections.Remove(subCollection);
            UpdatedAt = DateTime.Now;
        }

        public RequestCollection Clone()
        {
            var clone = new RequestCollection
            {
                Name = Name + " (Copy)",
                Description = Description,
                Environment = Environment.Clone()
            };

            // Clone requests
            foreach (var request in Requests)
            {
                clone.Requests.Add(request.Clone());
            }

            // Clone sub-collections
            foreach (var subCollection in SubCollections)
            {
                clone.AddSubCollection(subCollection.Clone());
            }

            return clone;
        }

        public override string ToString()
        {
            return $"{Name} ({TotalRequestsCount} requests)";
        }
    }

    public partial class Environment : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private ObservableCollection<EnvironmentVariable> _variables = new();

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;

        [ObservableProperty]
        private DateTime _updatedAt = DateTime.Now;

        public Environment()
        {
            Name = "Default Environment";
        }

        public Environment(string name, string description = "")
        {
            Name = name;
            Description = description;
        }

        public string GetVariableValue(string key)
        {
            var variable = Variables.FirstOrDefault(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            return variable?.Value ?? "";
        }

        public void SetVariable(string key, string value, string description = "")
        {
            var existingVariable = Variables.FirstOrDefault(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            
            if (existingVariable != null)
            {
                existingVariable.Value = value;
                existingVariable.Description = description;
                existingVariable.UpdatedAt = DateTime.Now;
            }
            else
            {
                Variables.Add(new EnvironmentVariable
                {
                    Key = key,
                    Value = value,
                    Description = description
                });
            }
            
            UpdatedAt = DateTime.Now;
        }

        public Environment Clone()
        {
            var clone = new Environment
            {
                Name = Name + " (Copy)",
                Description = Description
            };

            foreach (var variable in Variables)
            {
                clone.Variables.Add(variable.Clone());
            }

            return clone;
        }
    }

    public partial class EnvironmentVariable : ObservableObject
    {
        [ObservableProperty]
        private string _key = "";

        [ObservableProperty]
        private string _value = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private bool _enabled = true;

        [ObservableProperty]
        private string _type = "string"; // string, number, boolean, secret

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;

        [ObservableProperty]
        private DateTime _updatedAt = DateTime.Now;

        public EnvironmentVariable Clone()
        {
            return new EnvironmentVariable
            {
                Key = Key,
                Value = Value,
                Description = Description,
                Enabled = Enabled,
                Type = Type
            };
        }

        public override string ToString()
        {
            return $"{Key}: {Value}";
        }
    }
}
