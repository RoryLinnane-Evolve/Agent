# Ragent

A lightweight .NET framework for building tool-augmented AI agents that can reason, chat, and execute functions based on natural language instructions.

## Overview

Ragent enables developers to create intelligent agents that combine large language models (LLMs) with executable tools. The framework provides a clean abstraction for tool discovery, agent orchestration, and conversation management, allowing AI assistants to perform real-world tasks through function calls.

## Features

- **Tool-Augmented Agents**: Define tools as simple .NET methods and let the agent decide when and how to use them
- **Flexible Chat Backends**: Pluggable LLM integration supporting various models (Ollama only for now)
- **Reflection-Based Tool Discovery**: Automatic tool registration and schema generation using attributes
- **Message Management**: Structured conversation history and context management

## Requirements

The solution targets **.NET 11**. `global.json` pins the SDK feature band validated for this repository and permits newer .NET 11 feature bands. Existing library and test dependencies were restored and built against `net11.0`; the redundant `Microsoft.Extensions.Logging.Abstractions` test reference was removed because it is supplied by the shared framework.

## Samples

- [`sample/cli`](sample/cli) is the Terminal.Gui demonstration.
- [`sample/web`](sample/web) contains **Ragent Studio**, a Svelte + ASP.NET Core controller-based workspace that uses the real Ragent runtime. It demonstrates a clean architecture split between Domain, Application, Infrastructure, and API layers, per-conversation agent state, tool discovery, loading/error states, and an accessible chat workflow.

See the [Ragent Studio setup guide](sample/web/README.md) for prerequisites, provider configuration, commands, architecture, validation, and known limitations.
