using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace Snitcher.UI.Desktop.Models
{
    public partial class HttpRequest : ObservableObject
    {
        [ObservableProperty]
        private string _id = System.Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _method = "GET";

        [ObservableProperty]
        private string _url = "";

        [ObservableProperty]
        private ObservableCollection<HttpHeader> _headers = new();

        [ObservableProperty]
        private ObservableCollection<HttpParameter> _parameters = new();

        [ObservableProperty]
        private string _body = "";

        [ObservableProperty]
        private BodyType _bodyType = BodyType.None;

        [ObservableProperty]
        private AuthenticationType _authenticationType = AuthenticationType.None;

        [ObservableProperty]
        private AuthenticationConfig _authentication = new();

        [ObservableProperty]
        private ObservableCollection<HttpTest> _tests = new();

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private System.DateTime _createdAt = System.DateTime.Now;

        [ObservableProperty]
        private System.DateTime _updatedAt = System.DateTime.Now;

        public HttpRequest()
        {
            // Add default headers
            Headers.Add(new HttpHeader { Key = "Content-Type", Value = "application/json" });
            Headers.Add(new HttpHeader { Key = "Accept", Value = "application/json" });
        }

        public HttpRequest Clone()
        {
            return new HttpRequest
            {
                Name = Name + " (Copy)",
                Method = Method,
                Url = Url,
                Headers = new ObservableCollection<HttpHeader>(Headers.Select(h => h.Clone())),
                Parameters = new ObservableCollection<HttpParameter>(Parameters.Select(p => p.Clone())),
                Body = Body,
                BodyType = BodyType,
                AuthenticationType = AuthenticationType,
                Authentication = Authentication.Clone(),
                Tests = new ObservableCollection<HttpTest>(Tests.Select(t => t.Clone())),
                Description = Description
            };
        }

        public Dictionary<string, string> GetHeadersDictionary()
        {
            return Headers.Where(h => !string.IsNullOrWhiteSpace(h.Key) && !string.IsNullOrWhiteSpace(h.Value))
                          .ToDictionary(h => h.Key, h => h.Value);
        }

        public Dictionary<string, string> GetParametersDictionary()
        {
            return Parameters.Where(p => !string.IsNullOrWhiteSpace(p.Key) && !string.IsNullOrWhiteSpace(p.Value))
                            .ToDictionary(p => p.Key, p => p.Value);
        }
    }

    public partial class HttpHeader : ObservableObject
    {
        [ObservableProperty]
        private string _key = "";

        [ObservableProperty]
        private string _value = "";

        [ObservableProperty]
        private bool _enabled = true;

        public HttpHeader Clone()
        {
            return new HttpHeader { Key = Key, Value = Value, Enabled = Enabled };
        }
    }

    public partial class HttpParameter : ObservableObject
    {
        [ObservableProperty]
        private string _key = "";

        [ObservableProperty]
        private string _value = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private bool _enabled = true;

        public HttpParameter Clone()
        {
            return new HttpParameter { Key = Key, Value = Value, Description = Description, Enabled = Enabled };
        }
    }

    public partial class HttpTest : ObservableObject
    {
        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _script = "";

        [ObservableProperty]
        private bool _enabled = true;

        public HttpTest Clone()
        {
            return new HttpTest { Name = Name, Script = Script, Enabled = Enabled };
        }
    }

    public partial class AuthenticationConfig : ObservableObject
    {
        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _bearerToken = "";

        [ObservableProperty]
        private string _apiKey = "";

        [ObservableProperty]
        private string _apiValue = "";

        [ObservableProperty]
        private string _oauth2ClientId = "";

        [ObservableProperty]
        private string _oauth2ClientSecret = "";

        [ObservableProperty]
        private string _oauth2Scope = "";

        public AuthenticationConfig Clone()
        {
            return new AuthenticationConfig
            {
                Username = Username,
                Password = Password,
                BearerToken = BearerToken,
                ApiKey = ApiKey,
                ApiValue = ApiValue,
                Oauth2ClientId = Oauth2ClientId,
                Oauth2ClientSecret = Oauth2ClientSecret,
                Oauth2Scope = Oauth2Scope
            };
        }
    }

    public enum BodyType
    {
        None,
        Form,
        XForm,
        Raw,
        Binary,
        GraphQL
    }

    public enum AuthenticationType
    {
        None,
        Basic,
        Bearer,
        ApiKey,
        OAuth2
    }

    public static class HttpMethods
    {
        public static readonly string[] Methods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "CUSTOM" };
    }
}
