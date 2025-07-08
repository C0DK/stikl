{ config, pkgs, ... }:
{
  config = {
    environment.systemPackages = with pkgs; [
      git
      vim
      # https://taskfile.dev/
      go-task

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
            # required when the target is also TLS server with multiple hosts
            "proxy_ssl_server_name on;"
            +
              # required when the server wants to use HTTP Authentication
              "proxy_pass_header Authorization;";
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
