# Custom activity plugins

Teams ship their own Elsa activities without touching the host. Three supported ways:

## 1. Runtime plugin — drop a DLL (no rebuild, no NuGet)

1. Build the team's activity library (a copy of `src/Elsa.Activities.Samples`):
   ```bash
   dotnet build src/Elsa.Activities.Samples/Elsa.Activities.Samples.csproj
   ```
2. Copy the produced `Elsa.Activities.Samples.dll` into **this** folder:
   ```bash
   cp src/Elsa.Activities.Samples/bin/Debug/net10.0/Elsa.Activities.Samples.dll plugins/
   ```
3. Restart the host. It scans `plugins/*.dll`, loads them, and registers every `[Activity]` class.
   The folder is configurable via `Plugins:Directory` in `appsettings.json` (relative to the content root).

DLLs in this folder are gitignored. In Docker, mount a volume at `/app/plugins`.

## 2. NuGet package

1. Package the library:
   ```bash
   dotnet pack src/Elsa.Activities.Samples/Elsa.Activities.Samples.csproj -c Release
   ```
2. Push to your internal feed (e.g. `nuget.tajan.ir` / `repository.tajan.ir`).
3. Reference it from `Tajan.Workflows.Host` and register the assembly:
   ```csharp
   services.AddActivitiesFrom<SamplesActivityMarker>();   // or AddActivitiesFrom<YourTeamMarker>()
   services.AddSamplesActivities();                        // optional: register the plugin's services
   ```

## 3. Project reference (compile-time)

Add `<ProjectReference Include="..\..\..\src\Elsa.Activities.Samples\Elsa.Activities.Samples.csproj" />`
to the host, then call `AddActivitiesFrom<Marker>()` as above.

## Template for a new team

Copy `src/Elsa.Activities.Samples`, rename it to `Tajan.{Team}.Elsa.Activities`, then:

- Give every activity a **unique namespace** to avoid type-name collisions across teams:
  ```csharp
  [Activity("Tajan.{Team}", "Category", "Display Name", Description = "...")]
  ```
- Declare inputs/outputs with `[Input]`/`[Output]`.
- Keep a `Marker` class for assembly scanning.
- Register DI services in the `Add{Team}Activities()` extension (compile-time path).

## Requirements

- Build against the **same Elsa version** as the host (3.10.1, pinned centrally in `Directory.Packages.props`).
- Set `<EnableDynamicLoading>true</EnableDynamicLoading>` so shared Elsa assemblies resolve from the host.
- Activities that need DI services work in the compile-time/NuGet paths. If a runtime (DLL) plugin needs services,
  define a shared `IElsaPlugin { void Register(IServiceCollection services); }` contract in a small abstractions
  package and have the loader invoke it — keep that contract out of the team's activity assembly.
