---
title: Project Structure
---

It might seem like the CensorCore repo is using a lot of separate projects, and you're right. CensorCore is specifically designed to be as flexible and configurable as possible, ideally being relatively easy to adapt into any project as needed. As such, components are split up as much as possible to avoid conflicts and reduce the need to bring in unnecessary components in another project.

Roughly, here's the components: 

- **CensorCore**: the "main" library, CensorCore contains all the components and interfaces required to use CensorCore in your own projects.
- **CensorCore.Console**: a lightweight cross-platform CLI interface for CensorCore for on-demand censoring of images
- **CensorCore.ModelLoader**: a *standalone* component for consistently discovering and loading a NudeNet model. Allows for consistent conventions and abstracts away (optionally) downloading the model from GitHub.
- **CensorCore.Server**: a self-contained ASP.NET Core server project for running a basic API to CensorCore. This is only the server itself. You can include these components in your own server with `CensorCore.Web`
- **CensorCore.Shared**: the shared types used across projects. Don't references/depend on this! There is no API stability and this project may even be removed in future: depend on CensorCore.
- **CensorCore.Web**: a lightweight HTTP API implementation that uses CensorCore to censor remote image. This library contains the controllers and types required for a HTTP API implementation, but does not include a server (see above).

---

One of the biggest questions hanging over this project is it's hard dependency on ImageSharp. Currently multiple APIs directly expose ImageSharp types meaning there are parts of the project that simply can't be separated from it without significant rework. 

**At this time**, there is no plan to refactor this away but most components are still designed to be standalone _where possible_. This may lead to a future version where ImageSharp is just one option for handling images in CensorCore, but we're not there yet.