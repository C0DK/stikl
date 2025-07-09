{ config, pkgs, ... }:
{
  imports = [
    ./app.nix
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
        8080
      ];
    };

    services.nginx = {
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
            proxy_set_header   Host $host;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;
            proxy_pass_header  Authorization;

            # Increase the maximum size of the hash table
            proxy_headers_hash_max_size 1024;
            # Increase the bucket size of the hash table
            proxy_headers_hash_bucket_size 128;
          '';
        };
      };
    };
    security.acme = {
      acceptTerms = true;
      defaults.email = "c@cwb.dk";
    };
  };
}
