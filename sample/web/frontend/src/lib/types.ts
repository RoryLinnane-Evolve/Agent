export interface ToolParameter { name: string; type: string; description?: string | null }
export interface Tool { id: string; name: string; description: string; parameters: ToolParameter[] }
export interface Workspace { provider: string; tools: Tool[]; status: string }
export interface Message { role: string; content: string; createdAt: string; status?: string | null }
export interface Conversation { id: string; messages: Message[]; agentStatus: string }
