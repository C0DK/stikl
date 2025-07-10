{ config, pkgs, ... }:
{
  config = {
    services = {
      nginx = {
        virtualHosts."grafana.stikl.dk" = {
          addSSL = true;
          enableACME = true;
          locations."/" = {
            proxyPass = "http://${toString config.services.grafana.settings.server.http_addr}:${toString config.services.grafana.settings.server.http_port}";
            proxyWebsockets = true;
            recommendedProxySettings = true;
          };
        };
      };

      grafana = {
        enable = true;
        settings = {
          server = {
            # Listening Address
            http_addr = "127.0.0.1";
            # and Port
            http_port = 3000;
            # Grafana needs to know on which domain and URL it's running
            domain = "grafana.stikl.dk";
          };
        };
      };
    };
  };
}
