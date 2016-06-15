namespace HeyHttp.Core
{
    public class HeySslServerSettings : IHeyPortSettings
    {
        public ApplicationLayerProtocol Protocol { get; set; }

        public int Port { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public string Thumbprint { get; set; }
    }
}