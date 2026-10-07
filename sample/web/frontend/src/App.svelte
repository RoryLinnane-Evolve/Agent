<script lang="ts">
  import { onMount, tick } from 'svelte';
  import type { Conversation, Tool, Workspace } from './lib/types';

  let workspace: Workspace | null = null;
  let messages: Conversation['messages'] = [];
  let prompt = '';
  let busy = false;
  let loading = true;
  let error = '';
  let activeTool: Tool | null = null;
  let theme: 'light' | 'dark' = 'dark';
  const conversationId = crypto.randomUUID();
  let transcript: HTMLDivElement;

  const examples = ['What tools can you use?', 'Calculate 21 × 8', 'Summarize what you can help me with'];

  onMount(() => {
    const savedTheme = localStorage.getItem('ragent-studio-theme');
    theme = savedTheme === 'light' || savedTheme === 'dark'
      ? savedTheme
      : window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    loadWorkspace();
  });

  function toggleTheme() {
    theme = theme === 'dark' ? 'light' : 'dark';
    localStorage.setItem('ragent-studio-theme', theme);
  }

  async function loadWorkspace() {
    loading = true;
    error = '';
    try {
      const response = await fetch('/api/agent/workspace');
      if (!response.ok) throw new Error('Could not connect to the agent workspace.');
      workspace = await response.json();
      activeTool = workspace?.tools[0] ?? null;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Something went wrong while loading the workspace.';
    } finally {
      loading = false;
    }
  }

  async function send() {
    const content = prompt.trim();
    if (!content || busy) return;
    busy = true;
    error = '';
    messages = [...messages, { role: 'user', content, createdAt: new Date().toISOString() }];
    prompt = '';
    await scrollToLatest();
    try {
      const response = await fetch('/api/agent/messages', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ conversationId, content })
      });
      if (!response.ok) {
        const detail = await response.json().catch(() => null);
        throw new Error(detail?.error ?? 'The agent could not process this message.');
      }
      const conversation: Conversation = await response.json();
      messages = conversation.messages;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The message could not be sent.';
    } finally {
      busy = false;
      await scrollToLatest();
    }
  }

  async function scrollToLatest() {
    await tick();
    transcript?.scrollTo({ top: transcript.scrollHeight, behavior: 'smooth' });
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      send();
    }
  }
</script>

<svelte:head><meta name="description" content="A visual workspace for the Ragent .NET agent framework." /></svelte:head>

<main class:light-mode={theme === 'light'}>
  <header class="topbar">
    <a class="brand" href="/" aria-label="Ragent Studio home"><span class="brand-mark">R</span><span>Ragent <strong>Studio</strong></span></a>
    <div class="topbar-actions"><button class="theme-toggle" type="button" on:click={toggleTheme} aria-pressed={theme === 'light'} aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}><span aria-hidden="true">{theme === 'dark' ? '☀' : '◐'}</span>{theme === 'dark' ? 'Light' : 'Dark'}</button><div class="runtime"><span class:online={workspace?.status === 'IDLE'}></span>{workspace?.provider ?? 'Connecting…'}</div></div>
  </header>

  <section class="shell">
    <aside class="sidebar">
      <div class="side-heading"><p>Agent workspace</p><h2>Available tools</h2></div>
      {#if loading}<div class="tool-skeleton">Loading agent capabilities…</div>
      {:else if workspace}
        <div class="tool-list">
          {#each workspace.tools as tool}
            <button class:active={activeTool?.id === tool.id} class="tool-card" on:click={() => activeTool = tool}>
              <span class="tool-icon">⌁</span><span><strong>{tool.name}</strong><small>{tool.description}</small></span>
            </button>
          {/each}
        </div>
      {/if}
      <div class="sidebar-note"><span>●</span> Tools are discovered from your Ragent assemblies at startup.</div>
    </aside>

    <section class="chat-panel" aria-label="Agent conversation">
      <div class="chat-heading"><div><p>Session / {conversationId.slice(0, 8)}</p><h1>Build with an agent that can act.</h1></div><button class="quiet-button" on:click={() => { messages = []; error = ''; }}>New conversation</button></div>
      {#if error}<div class="alert" role="alert"><span>!</span><div>{error}<button on:click={loadWorkspace}>Retry connection</button></div></div>{/if}
      <div class="transcript" bind:this={transcript}>
        {#if messages.length === 0}
          <div class="empty-state"><div class="orb">✦</div><h2>Start exploring your agent</h2><p>Ask a question, invoke a tool, or use one of these prompts to see Ragent’s live message history.</p><div class="suggestions">{#each examples as example}<button on:click={() => { prompt = example; send(); }}>{example}<span>↗</span></button>{/each}</div></div>
        {:else}
          {#each messages as message}
            <article class:from-user={message.role === 'user'} class:from-error={message.role === 'error'} class="message"><div class="avatar">{message.role === 'user' ? 'You' : message.role === 'tool' ? 'Tool' : 'R'}</div><div><div class="message-meta">{message.role === 'user' ? 'You' : message.role === 'tool' ? 'Tool result' : message.role === 'error' ? 'Agent error' : 'Ragent'} <span>{message.status}</span></div><p>{message.content}</p></div></article>
          {/each}
        {/if}
        {#if busy}<article class="message"><div class="avatar">R</div><div class="typing"><i></i><i></i><i></i></div></article>{/if}
      </div>
      <form class="composer" on:submit|preventDefault={send}><textarea bind:value={prompt} on:keydown={handleKeydown} placeholder="Message your agent…" aria-label="Message your agent" disabled={busy}></textarea><div><span>Enter to send · Shift + Enter for a new line</span><button type="submit" disabled={busy || !prompt.trim()}>{busy ? 'Thinking…' : 'Send'} <b>↑</b></button></div></form>
    </section>

    <aside class="inspector">
      <p>Inspector</p>
      {#if activeTool}<div class="tool-detail"><div class="detail-icon">⌁</div><h2>{activeTool.name}</h2><code>{activeTool.id}</code><p>{activeTool.description}</p><h3>Parameters</h3>{#if activeTool.parameters.length}<ul>{#each activeTool.parameters as parameter}<li><strong>{parameter.name}</strong><span>{parameter.type}</span><small>{parameter.description ?? 'No description supplied'}</small></li>{/each}</ul>{:else}<div class="none">No parameters</div>{/if}</div>
      {:else}<div class="none">Select a tool to inspect its interface.</div>{/if}
      <div class="config-hint"><span>⌘</span><p><strong>Runtime configuration</strong>Change <code>AgentRuntime:Model</code> in appsettings.json to switch provider.</p></div>
    </aside>
  </section>
</main>
