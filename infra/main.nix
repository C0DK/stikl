{ config, pkgs, ... }:
{

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
          # TODO: this isn't currently sourced correctly if built manually.
          # we should use the whole dockertools thing.
          # currently built via sudo podman build
          image = "stikl-web:latest";
          environment = {
            DEV_MODE = "false";
          };
          environmentFiles = [
            ../api/.env
          ];
          ports = [ "8080:8080" ];
          pull = "always";

        };
      };
    };
  };
}
