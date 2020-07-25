using System.Text.Json.Serialization;

namespace A6k.Nats.Operations
{
    public class ServerInfo
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("go")]
        public string Go { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("max_payload")]
        public ulong MaxPayload { get; set; }

        [JsonPropertyName("proto")]
        public int Proto { get; set; }

        [JsonPropertyName("client_id")]
        public ulong ClientId { get; set; }

        [JsonPropertyName("auth_required")]
        public bool AuthRequired { get; set; }

        [JsonPropertyName("tls_required")]
        public bool TlsRequired { get; set; }

        [JsonPropertyName("tls_verify")]
        public bool TlsVerify { get; set; }

        [JsonPropertyName("connect_urls")]
        public string[] ConnectUrls { get; set; }

        public override string ToString() => $"server:{ServerId} client:{ClientId}";
    }
}
