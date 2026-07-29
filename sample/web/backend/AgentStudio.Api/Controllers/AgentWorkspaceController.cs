using AgentStudio.Application.Abstractions;
using AgentStudio.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AgentStudio.Api.Controllers;

[ApiController]
[Route("api/agent")]
public sealed class AgentWorkspaceController(IAgentWorkspace workspace) : ControllerBase
{
    [HttpGet("workspace")]
    [ProducesResponseType<AgentWorkspaceDto>(StatusCodes.Status200OK)]
    public ActionResult<AgentWorkspaceDto> GetWorkspace() => Ok(workspace.GetWorkspace());

    [HttpGet("conversations/{conversationId}")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    public ActionResult<ConversationDto> GetConversation(string conversationId) => Ok(workspace.GetConversation(conversationId));

    [HttpPost("messages")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationDto>> SendMessage(SendMessageCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ConversationId) || string.IsNullOrWhiteSpace(command.Content))
            return BadRequest(new { error = "A conversation ID and message are required." });

        if (command.Content.Length > 4_000)
            return BadRequest(new { error = "Messages must be 4,000 characters or fewer." });

        return Ok(await workspace.SendAsync(command, cancellationToken));
    }
}
