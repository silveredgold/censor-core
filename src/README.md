This might seem like a lot of separate projects, and you're right. CensorCore is specifically designed to be as flexible and configurable as possible, ideally being relatively easy to adapt into any project as needed. As such, components are split up as much as possible to avoid conflicts and reduce the need to bring in unnecessary components in another project.

Roughly, here's the components: 

- **CensorCore**: the "main" library, CensorCore contains all the components and interfaces required to use CensorCore in your own projects.
- **CensorCore.Console**: a lightweight cross-platform CLI interface for CensorCore for on-demand censoring of images
- **CensorCore.ModelLoader**: a *standalone* component for consistently discovering and loading a NudeNet model. Allows for consistent conventions and abstracts away (optionally) downloading the model from GitHub.
- **CensorCore.Server**: a self-contained ASP.NET Core server project for running a basic API to CensorCore. This is only the server itself. You can include these components in your own server with `CensorCore.Web`
- **CensorCore.Shared**: the shared types used across projects. Don't references/depend on this! There is no API stability and this project may even be removed in future: depend on CensorCore.
- **CensorCore.Web**: a lightweight HTTP API implementation that uses CensorCore to censor remote image. This library contains the controllers and types required for a HTTP API implementation, but does not include a server (see above).

---

Bonus unrequested FAQ answer: its in C# because it was the only platform with a good balance of performance, flexibility, and my familiarity. Browser/WASM performance was beyond abysmal, Node's image handling libraries leave a lot to be desired, and of the other "native" stacks, .NET is the only one I'm familiar enough in to do at this scale.