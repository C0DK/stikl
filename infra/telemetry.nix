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
      prometheus = {
        enable = true;

        scrapeConfigs = [
          {
            job_name = "garden";
            static_configs = [
              {
                targets = [
                  "127.0.0.1:${toString config.services.prometheus.exporters.node.port}"
                  # TODO: make sure that you can only call this from prometheus.
                  #"10.88.0.1:8080"
                ];
              }
            ];
          }
        ];
        port = 9001;

        exporters = {
          node = {
            enable = true;
            enabledCollectors = [ "systemd" ];
            port = 9002;
          };
        };

      };
      loki = {
        enable = true;
        configFile = ./loki-config.yaml;
      };

    };
    systemd.services.promtail = {
      description = "Promtail service for Loki";
      wantedBy = [ "multi-user.target" ];

      serviceConfig = {
        ExecStart = ''
          ${pkgs.grafana-loki}/bin/promtail --config.file ${./promtail-config.yaml}
        '';
      };
    };
  };
}
