# QuickJSharp Documentation

Welcome to the official documentation for **QuickJSharp**, a high-performance, modern C# wrapper for the QuickJS-NG engine.

## Features

- **Speed**: Low-overhead marshaling with millions of boundery-crossing calls per second.
- **Strongly Typed**: Clean, high-level C# wrappers for JS types and all kinds of interactions with the JS engine.

## Quick Links

- [**Get Started**](articles/getting-started.md)
- [**API Reference**](api/QuickJSharp.yml)
- [**GitHub Repository**](https://github.com/the-byte-bender/quickjsharp)

## Very Basic Example

```csharp
using QuickJSharp;

using var rt = new JSRuntime();
using var ctx = rt.CreateContext();

// Evaluate script
var result = ctx.Eval("1 + 1");
Console.WriteLine(result.ToInt32(ctx)); // 2
```
