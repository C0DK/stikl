{ config, pkgs, ... }:
{
  imports = [
    ./app.nix
    ./telemetry.nix
  ];
  config = {
    environment.systemPackages = with pkgs; [
      git
      vim
      # https://taskfile.dev/
      go-task

      # testing
      openssl

    ];

    networking.firewall = {
      enable = true;
      allowedTCPPorts = [
        22
        80
        443
        5432
      ];
    };
    security.acme = {
      acceptTerms = true;
      defaults.email = "c@cwb.dk";
    };

    services = {
      nginx = {
        enable = true;
        recommendedProxySettings = true;
        recommendedTlsSettings = true;
        logError = "stderr debug";
        virtualHosts."stikl.dk" = {
          enableACME = true;
          forceSSL = true;
          locations."/" = {
            proxyPass = "http://10.88.0.1:8080";
            extraConfig = ''
              proxy_pass_header  Authorization;
              # Increase the maximum size of the hash table
              proxy_headers_hash_max_size 1024;
              # Increase the bucket size of the hash table
              proxy_headers_hash_bucket_size 128;

              proxy_busy_buffers_size   512k;
              proxy_buffers   4 512k;
              proxy_buffer_size   256k;
            '';
          };
        };
      };
    };
  };
}
