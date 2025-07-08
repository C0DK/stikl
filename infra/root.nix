{ config, pkgs, ... }:
{
  config = {
    environment.systemPackages = with pkgs; [
      git
      vim
      # https://taskfile.dev/
      go-task
    ];
  };
}
