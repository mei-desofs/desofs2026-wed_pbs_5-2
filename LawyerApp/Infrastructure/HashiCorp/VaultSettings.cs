namespace LawyerApp.Infrastructure.HashiCorp
{
    public class VaultSettings
    {
        public string ServerUri { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        // Onde os segredos estão montados (normalmente "secret" no modo KV v2)
        public string MountPoint { get; set; } = "secret";

        // O caminho específico para os segredos da app
        public string SecretPath { get; set; } = string.Empty;
        public string Namespace { get; set; } = "admin";
    }
}
