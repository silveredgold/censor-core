---
title: AI/ML Components
---

On account of its design philosophy (extensible and configurable to a fault), CensorCore uses an **absurd** amount of interfaces/components/etc to try and allow for easier customisation/adaptation. As such, many of these components will be their own components purely for the sake of being able to be replaced.

## `AIService`

> This is the big one

`AIService` is the main entry point for classifying images using the NudeNet model. You will always need to create an `AIService` (check the static methods for some convenience factory methods) then run your images through it. Given the process of _creating_ an `AIService` is expensive, it's recommended to initialize one service and reuse it for multiple images as much as possible. As far as I'm aware it should all be pretty thread-safe.

## `IImageHandler`

The image handler is a component used by the `AIService` (or anything else) to abstract away the loading/parsing/transformation of image files. The two "main" uses for the image handler is firstly to load images from any source into memory, and secondly to perform any operations on that image data required by the AI (like resizing).

Crucially, the ImageHandler is also *partially* responsible for loading the image data into a tensor the AI can use. The image loading is handled by an `IImageHandler`, but it uses a `TensorLoadOptions<TTensorType>` to perform any *model-specific* transformations on the image data, like normalization or channel transforms.

> `TensorLoadOptions` is a very simple type, and CensorCore includes pre-built load options for both NudeNet and 68PFLD models.

The CensorCore default image handler is `ImageSharpHandler`, a fully-featured image handler based on ImageSharp.

## `MatchOptions`

A lightweight type used to fine-tune the "strictness" of the AI. Matches must meet the minimum confidence of a given `MatchOptions` to be considered valid and returned to the consumer. If a `MatchOptions` returns `0` for default or class scores, _all_ matches will be returned, which is probably a bad idea.