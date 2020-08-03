using System.Text.Json.Serialization;

namespace A6k.Nats.Operations
{
    public class ConnectOperation
    {
        [JsonPropertyName("verbose")]
        public bool Verbose { get; set; }

        [JsonPropertyName("pedantic")]
        public bool Pedantic { get; set; }

        [JsonPropertyName("tls_required")]
        /// Indicates whether the client requires an SSL connection.
        public bool? TlsRequeired { get; set; }

        [JsonPropertyName("auth_token")]
        /// : Client authorization token (if auth_required is set)
        public string AuthToken { get; set; }
        
        [JsonPropertyName("user")]
        /// : Connection username(if auth_required is set)
        public string User { get; set; }
        
        [JsonPropertyName("pass")]
        /// : Connection password(if auth_required is set)
        public string Pass { get; set; }
        
        [JsonPropertyName("name")]
        /// : Optional client name
        public string Name { get; set; }
        
        [JsonPropertyName("lang")]
        /// : The implementation language of the client.
        public string Lang { get; set; }
        
        [JsonPropertyName("version")]
        /// : The version of the client.
        public string version { get; set; }
        
        [JsonPropertyName("protocol")]
        /// : optional int. Sending 0 (or absent) indicates client supports original protocol. Sending 1 indicates that the client supports dynamic reconfiguration of cluster topology changes by asynchronously receiving INFO messages with known servers it can reconnect to.
        public int? Protocol { get; set; }
        
        [JsonPropertyName("echo")]
        /// : Optional boolean. If set to true, the server (version 1.2.0+) will not send originating messages from this connection to its own subscriptions.Clients should set this to true only for server supporting this feature, which is when proto in the INFO protocol is set to at least 1.
        public bool? Echo { get; set; }
        
        [JsonPropertyName("sig")]
        /// : In case the server has responded with a nonce on INFO, then a NATS client must use this field to reply with the signed nonce.
        public string Sig { get; set; }
        
        [JsonPropertyName("jwt")]
        /// : The JWT that identifies a user permissions and acccount.
        public string Jwt { get; set; }
    }
}
