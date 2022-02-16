# RelationalAI C# Client SDK

This is a Client SDK for RelationalAI

- API version: 1.2.7

## Frameworks supported


- .NET Core 5.0+

## Dependencies

- ini-parser-netstandard (>= 2.5.2)
- Newtonsoft.Json (>= 12.0.3)
- NSec.Cryptography (>= 20.2.0)

## Installation

Run the following command to generate the DLL

```shell
dotnet build
```

Then include the DLL (under the `bin` folder) in the C# project, and use the namespaces:

```csharp
using Com.RelationalAI.Api;
using Com.RelationalAI.Client;
using Com.RelationalAI.Model;

```

## Packaging

You can build the `.csproj` directly:

```
dotnet pack
```

Then, publish to a [local feed](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds) or [other host](https://docs.microsoft.com/en-us/nuget/hosting-packages/overview) and consume the new package via Nuget as usual.


## Nuget users

There are two options available:

- [`Nuget.org`](https://www.nuget.org/packages/RelationalAI/)
- [`Github Packages`](https://github.com/RelationalAI-oss/RelationalAICSharpClient/packages/420662)

## Licensing

RelationalAI Java Client SDK is licensed under the Apache License, Version 2.0.

## Author

[Mohammad Dashti](mailto:mohammad.dashti[at]relational[dot]ai)
