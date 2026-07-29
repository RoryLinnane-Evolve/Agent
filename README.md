# Ragent

A lightweight .NET framework for building tool-augmented AI agents that can reason, chat, and execute functions based on natural language instructions.

## Overview

Ragent enables developers to create intelligent agents that combine large language models (LLMs) with executable tools. The framework provides a clean abstraction for tool discovery, agent orchestration, and conversation management, allowing AI assistants to perform real-world tasks through function calls.

## Features

- **Tool-Augmented Agents**: Define tools as simple .NET methods and let the agent decide when and how to use them
- **Deterministic Workflow Plans**: The agent plans multi-step tool workflows up front as a JSON DAG, mapping one tool's output onto another tool's input with `{{stepId}}` placeholders
- **Parallel Tool Execution**: Independent plan steps run concurrently (bounded by `MaxParallelTools`); dependent steps wait only for the outputs they reference
- **Plan Validation**: Plans are validated before execution (unknown tools, missing steps, duplicate IDs, dependency cycles) and invalid plans are sent back to the LLM for correction
- **Iterative Replanning**: After a plan executes, the LLM sees every step's result and can plan follow-up work, up to `MaxIterations`
- **Flexible Chat Backends**: Pluggable LLM integration supporting various models (Ollama, Gemini), plus an `LLMClientFactory` hook for custom backends and testing
- **Reflection-Based Tool Discovery**: Automatic tool registration and schema generation using attributes; sync and `Task`-returning async tools are both supported
- **Message Management**: Structured conversation history and context management

## How Workflow Plans Work

When a request needs tools, the LLM replies with a single JSON plan instead of one tool call at a time:

```json
{ "plan": [
  { "stepId": "s1", "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://example.com/a" } ] },
  { "stepId": "s2", "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://example.com/b" } ] },
  { "stepId": "s3", "toolId": "summarise", "params": [ { "name": "text", "value": "{{s1}}\n{{s2}}" } ] }
] }
```

The executor derives a dependency graph from the `{{stepId}}` placeholders and runs the plan in waves:
`s1` and `s2` are independent, so they run in parallel; `s3` waits for both, then receives their outputs
substituted into its `text` parameter. If a step fails, dependent steps are skipped with a clear error
while independent steps still run.
