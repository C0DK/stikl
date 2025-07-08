{ config, pkgs, ... }:
{
  config = {
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

    services.postgresql = {
      enable = true;
      ensureDatabases = [ "stikl" ];
      enableTCPIP = true;
      authentication = pkgs.lib.mkOverride 10 ''
        #type database DBuser origin-address auth-method
        local all       all     trust
        # ipv4
        host  all      all     127.0.0.1/32   trust
        host  all      all     ::1/128   trust
      '';
    };

    # https://bkiran.com/blog/deploying-containers-nixos
    virtualisation = {
      podman = {
        enable = true;
      };

      oci-containers = {
        #backend= "docker";
        containers = {
          stikl-web = {
            # todo login if private?
            login = {
              registry = "https://ghcr.io";
              username = "C0DK";
              passwordFile = "/etc/stikl/registry-password.txt";
            };
            image = "ghcr.io/c0dk/stikl:main";
            environment = {
              DEV_MODE = "false";
            };
            environmentFiles = [
              ../app/.env
            ];
            ports = [ "8080:8080" ];
            pull = "always";

          };
        };
      };

    };
  };
}
