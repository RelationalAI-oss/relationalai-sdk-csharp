# C# Client SDK samples

`C# RelationalAI` allows users to run RelationalAI logic against local RelationalAI servers or hosted servers in the cloud using `C#`.

## Getting started

First we need to install `Dotnet core 3.1` [dotnet-core-3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) available for Windows, Linux and OSX.

## Usage
### Dependencies
In order to perform local tests using `C# Client SDK`, we need to run RelationalAI server locally
```
julia -O1
julia> using Pkg
julia> using Revise
julia> Pkg.activate(".")
julia> # to start RelationalAI server
julia> include("./incubator/ClientSDKs/start-rai-server.jl")
julia> # to stop RelationalAI server
julia> include("./incubator/ClientSDKs/stop-rai-server.jl")
```
### Building C# Client SDK and running samples
```
cd ./incubator/ClientSDKs/csharp-client-sdk
# to build the SDK
dotnet build
# to run tests
dotnet test
# to run samples
dotnet run --project RelationalAISamples/RelationalAISamples.csproj
```
