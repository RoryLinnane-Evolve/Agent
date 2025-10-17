# Ragent

A lightweight .NET framework for building tool-augmented AI agents that can reason, chat, and execute functions based on natural language instructions.

## Overview

Ragent enables developers to create intelligent agents that combine large language models (LLMs) with executable tools. The framework provides a clean abstraction for tool discovery, agent orchestration, and conversation management, allowing AI assistants to perform real-world tasks through function calls.

## Features

- **Tool-Augmented Agents**: Define tools as simple .NET methods and let the agent decide when and how to use them
- **Flexible Chat Backends**: Pluggable LLM integration supporting various models (Ollama only for now)
- **Reflection-Based Tool Discovery**: Automatic tool registration and schema generation using attributes
- **Message Management**: Structured conversation history and context management
