{
  pkgs ? import <nixpkgs> {},
  raiserverBinary ? "",
  doCheck ? true
}:
with pkgs;
let
  deps = import ./deps.nix {inherit fetchurl;};
in
stdenv.mkDerivation rec {
  name = "rai-server-csharp-client-sdk-${version}";
  version = "1.2.0";
  buildInputs = [
    raiserverBinary
    dotnet-sdk_3
    dotnetPackages.Nuget
  ];

  src = ./.;

  buildPhase = ''
    mkdir home
    export HOME=$PWD/home

    # disable default-source so nuget does not try to download from online-repo
    nuget sources Disable -Name "nuget.org"
    # add all dependencies to source called 'deps'
    for package in ${toString deps}; do
      nuget add $package -Source $HOME/deps
    done

    dotnet restore --source $HOME/deps RelationalAI.sln
    dotnet build --no-restore -c Release RelationalAI.sln
  '';

  installPhase = ''
    dotnet pack RelationalAI/RelationalAI.csproj

    mkdir -p $out/{bin,lib}
    cp -v RelationalAI/bin/Debug/RelationalAI.${version}.nupkg $out/lib
  '';

  checkPhase = ''
    rai-server server &
    PID=$!
    sleep 15s
    dotnet test --no-restore --filter LocalIntegrationTests || (kill -9 $PID && exit 1)
    echo "Shutting down rai-server server. Pid: $PID"
    kill -9 $PID
  '';

  inherit doCheck;
}
