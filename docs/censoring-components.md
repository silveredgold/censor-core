---
title: "Censoring Components"
---

> This is going to be **a lot of moving parts**. This is partially by design and partially a result of rapidly changing ideas and requirements. I really can't guarantee API stability for much of this at this early stage.

## `ICensoringProvider`

> This is the big one

`ICensoringProvider` is the basic interface for censoring an image after it's been classified by the [AI service](./ai-components.md). In general, you want to take the `ImageResult` from the AI and pass it to the `ICensoringProvider`, optionally with a results parser. From there, the censoring provider will censor the image and return the censored result.

## `ImageSharpCensoringProvider`

This is the default censoring provider for CensorCore.

> It is **strongly** recommended to use the `ImageSharpCensoringProvider` as your `ICensoringProvider` implementation. It has its own extensibility points for customisations.

This provider uses ImageSharp to load and modify the image being censored, relying on "censor type providers" (see below) for the real work of censoring the image. The `ImageSharpCensoringProvider` will (to greatly oversimplify things), loop through the results from the AI, run any transformers (see below), run any middlewares (see below), match results against a censor type to get the required image modification (i.e the censoring bit), then apply the results of all of the above onto the image to produce a censored image.

That design of deferring the image editing does mean that censor types cannot depend on one another, but it also makes it much harder to cause accidental conflicts/cancellations.

### `ICensorTypeProvider`

The censor type provider is an implementation of `ICensorTypeProvider` and is responsible for the different censoring methods. For example, blurring and pixelation are two of the available providers. Each provider is passed the unmodified source image, the details of the current match, and a results parser if one is available. The provider can then (optionally) return the modification it wants to make to the image.

> There's a whole bunch of censor types included with CensorCore: blurring, pixelation, and black bar, sticker and caption overlays.

Note that the modification is not *immediately* applied to the source image, the provider just stores it for later application.

#### Layers

Every provider specifies a *layer* that its modifications are made on. Providers can specify whatever layer they want, but the default is `0`. In general, only change this if your censor type is **additive**. Layer 0 is where all destructive or source-based edits are made (like blurs, pixelations etc), while captions and stickers (for example) are made a few layers higher since they are drawn over other censors/image features. You could also set your layer below `0`, but be aware that it may then be cancelled out by the source-based censors on layer 0.

#### Virtual Classifications

The `Classification` type includes a `VirtualBox` property that designates the current classification as "virtual". Virtual classifications are matches that aren't directly based on the content at the given bounding box. This *usually* indicates that the content to be censored is at a related location (such as the location after any relevant offsets or transforms are applied). If your provider is limited in capabilities (like not handling angled results) then skip virtual classifications to avoid censoring irrelevant areas of the images.

### `IResultsTransformer`

> Use this with caution! If an `IResultsTransformer` doesn't return a match, it is **dropped** from the censoring session.

The `IResultsTransformer` API is useful for transforming the results from the AI before the censoring begins, for changes that don't need the image data. For example, an `IResultsTransformer` can add new matches based on existing AI matches, or add/remove matches to be passed to the censoring providers. For a more grounded example, one of the bundled transformers applies a flat scaling to increase/decrease the size of the matches returned by the AI.

A no-op transformer can simply return the match collection it was passed. Additionally, the ResultsTransformer is currently **synchronous**. This may be changed in a future release.

### `ICensoringMiddleware`

> Use this with caution! This API is very easy to get wrong, and is _usually_ only a last resort for niche use cases.

As the name implies, censoring middleware is an abstraction for more complex and niche censoring needs that need access to more of the censoring session. They can also add new mutations to the image but *cannot* modify/remove other censors. Since they are executed sequentially and block censoring until completed, you should try and keep middleware as fast as possible. Currently, it is only used for censoring matches not supported by NudeNet.

## `IResultParser`

The result parser is a special type that should be provided by *consumers* of CensorCore. The result parser can be injected into the `ImageSharpCensoringProvider` or passed per-request to the current censoring provider. The result parser is what the censor type providers query to tweak whether the censoring should be applied and/or how aggressively.