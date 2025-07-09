{ config, pkgs, ... }:
{
  config = {
    imports = [
      ./app.nix
    ];
    environment.systemPackages = with pkgs; [
      git
      vim
      # https://taskfile.dev/
      go-task

    ];

    networking.firewall.allowedTCPPorts = [
      22
      80
      443
    ];

    services.nginx = {
      enable = true;
      recommendedProxySettings = true;
      recommendedTlsSettings = true;

      virtualHosts."stikl.dk" = {
        enableACME = true;
        forceSSL = true;
        locations."/" = {
          proxyPass = "http://127.0.0.1:8080";
          proxyWebsockets = true; # needed if you need to use WebSocket
          extraConfig =
            "proxy_http_version 1.1;"
            + "proxy_set_header   Upgrade $http_upgrade;"
            + "proxy_set_header   Connection $connection_upgrade;"
            + "proxy_buffering off;"
            + "proxy_read_timeout 100s;"
            + "proxy_set_header   Host $host;"
            + "proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;"
            + "proxy_set_header   X-Forwarded-Proto $scheme;"
            + "proxy_pass_header Authorization;";
        };
      };
    };
    security.acme = {
      acceptTerms = true;
      defaults.email = "c@cwb.dk";
    };
  };
}
