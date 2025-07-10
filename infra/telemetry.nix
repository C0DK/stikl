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
            job_name = "chrysalis";
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
        configuration = {
          server.http_listen_port = 3030;
          auth_enabled = false;

          ingester = {
            lifecycler = {
              address = "127.0.0.1";
              ring = {
                kvstore = {
                  store = "inmemory";
                };
                replication_factor = 1;
              };
            };
          };

          schema_config = {
            configs = [
              {
                from = "2022-06-06";
                store = "boltdb-shipper";
                object_store = "filesystem";
                schema = "v11";
                index = {
                  prefix = "index_";
                  period = "24h";
                };
              }
            ];
          };

        };
      };

      # promtail: port 3031 (8031)
      #
      promtail = {
        enable = true;
        configuration = {
          server = {
            http_listen_port = 3031;
            grpc_listen_port = 0;
          };
          positions = {
            filename = "/tmp/positions.yaml";
          };
          clients = [
            {
              url = "http://127.0.0.1:${toString config.services.loki.configuration.server.http_listen_port}/loki/api/v1/push";
            }
          ];
          scrape_configs = [
            {
              job_name = "journal";
              journal = {
                max_age = "12h";
                labels = {
                  job = "systemd-journal";
                  host = "pihole";
                };
              };
              relabel_configs = [
                {
                  source_labels = [ "__journal__systemd_unit" ];
                  target_label = "unit";
                }
              ];
            }
          ];
        };
        # extraFlags
      };

    };
  };
}
