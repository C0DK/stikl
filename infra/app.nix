{ config, pkgs, ... }:
{
  config = {
    environment.systemPackages = with pkgs; [
      git
      vim
      # https://taskfile.dev/
      go-task

    ];

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
        containers = {
          stikl-web = {
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
            networkMode = "host";
            ports = [ "8080:8080" ];
            pull = "always";

          };
        };
      };

    };
  };
}
