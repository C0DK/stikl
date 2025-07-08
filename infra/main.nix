{ config, pkgs, ... }:
{
  config.environment.systemPackages = with pkgs; [
    git
    vim
    # https://taskfile.dev/
    go-task

  ];

  config.services.postgresql = {
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
  config.virtualisation = {
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
          ports = [ "80:8080" ];
          pull = "always";

        };
      };
    };

    services.certbot = {
      enable = true;
      agreeTerms = true;
    };
  };
}
