using System.ComponentModel.DataAnnotations;

using datasheetapi.Adapters;
using datasheetapi.Exceptions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;

namespace datasheetapi.Controllers;

[ApiController]
[Route("/tag/reviews/{reviewId}/conversations")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
[RequiresApplicationRoles(
    ApplicationRole.Admin,
    ApplicationRole.ReadOnlyUser,
    ApplicationRole.User
)]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(ILoggerFactory loggerFactory,
                            IConversationService conversationService)
    {
        _logger = loggerFactory.CreateLogger<ConversationsController>();
        _conversationService = conversationService;
    }

    [HttpPost(Name = "CreateConversation")]
    public async Task<ActionResult<GetConversationDto>> CreateConversation(
        [FromRoute][NotEmptyGuid] Guid reviewId, [FromBody][Required] ConversationDto conversation)
    {
        _logger.LogDebug("Creating new conversation in the review {reviewId}.", reviewId);
        if (conversation.Property != null)
        {
            if (!ValidateProperty<InstrumentPurchaserRequirement>(conversation.Property) &&
                !ValidateProperty<InstrumentSupplierOfferedProduct>(conversation.Property) &&
                !ValidateProperty<TagDataDto>(conversation.Property))
            {
                throw new BadRequestException($"Not supported property: {conversation.Property}");
            }
        }

        var savedConversation = await _conversationService.CreateConversation(
            conversation.ToModel(reviewId, Utils.GetAzureUniqueId(HttpContext.User)));
        _logger.LogInformation("Created new conversation in the review {reviewId}.", reviewId);

        var userIdNameMap = await _conversationService.GetUserIdUserName(
            savedConversation.Participants.Select(p => p.UserId).ToList());
        return savedConversation.ToDto(userIdNameMap);
    }

    [HttpGet("{conversationId}", Name = "GetConversation")]
    public async Task<ActionResult<GetConversationDto>> GetConversation(
        [NotEmptyGuid] Guid reviewId, [NotEmptyGuid] Guid conversationId)
    {
        _logger.LogDebug("Fetching conversation on the reviewId {reviewId}", reviewId);
        var conversation = await _conversationService.GetConversation(conversationId);

        var userIdNameMap = await _conversationService.GetUserIdUserName(
            conversation.Participants.Select(p => p.UserId).ToList());

        return conversation.ToDto(userIdNameMap);

    }

    /// <summary>
    /// Get the list of conversation available under the reviewId
    /// </summary>
    /// <param name="reviewId">Unique Id for the review</param>
    /// <param name="includeLatestMessage">Include Latest Message in the conversation. 
    /// The latest message will be non soft deleted message if at least one exists, else it will send last soft deleted message.</param>
    /// <returns></returns>
    [HttpGet(Name = "GetConversations")]
    public async Task<ActionResult<List<GetConversationDto>>> GetConversations([NotEmptyGuid] Guid reviewId,
        [FromQuery] bool includeLatestMessage = false)
    {

        var conversations = await _conversationService.GetConversations(reviewId, includeLatestMessage);

        var userIds = conversations.SelectMany(conversation =>
                        conversation.Participants.Select(p => p.UserId)).ToList();
        var userIdNameMap = await _conversationService.GetUserIdUserName(userIds);

        return conversations.Select(conversation => conversation.ToDto(userIdNameMap)).ToList();
    }

    [HttpPost("{conversationId}/messages", Name = "AddMessage")]
    public async Task<ActionResult<GetMessageDto>> AddMessage(
        [FromRoute][NotEmptyGuid] Guid reviewId, [FromRoute][NotEmptyGuid] Guid conversationId,
        [Required] MessageDto messageDto)
    {
        _logger.LogDebug("Adding new message in the {conversationId} of review {reviewId}.",
            conversationId, reviewId);
        var message = messageDto.ToMessageModel(Utils.GetAzureUniqueId(HttpContext.User));

        var savedMessage = await _conversationService.AddMessage(conversationId, message);
        _logger.LogInformation("Added new message in the conversation {conversationId}.", conversationId);

        return savedMessage.ToMessageDto(await _conversationService.GetUserName(savedMessage.UserId));
    }

    [HttpGet("{conversationId}/messages/{messageId}", Name = "GetMessage")]
    public async Task<ActionResult<GetMessageDto>> GetMessage(
        [NotEmptyGuid] Guid reviewId, [NotEmptyGuid] Guid conversationId, [NotEmptyGuid] Guid messageId)
    {
        _logger.LogDebug("Fetching message on the conversation {conversationId} of review {reviewId}", conversationId, reviewId);
        var message = await _conversationService.GetMessage(messageId);
        var username = await _conversationService.GetUserName(message.UserId);

        return message.ToMessageDto(username);
    }

    [HttpGet("{conversationId}/messages", Name = "GetMessages")]
    public async Task<ActionResult<List<GetMessageDto>>> GetMessages(
        [NotEmptyGuid] Guid reviewId, [NotEmptyGuid] Guid conversationId)
    {
        _logger.LogDebug("Fetching messages on the conversation {conversationId} of review {reviewId}",
            conversationId, reviewId);
        var messges = await _conversationService.GetMessages(conversationId);

        var userIdNameMap = await _conversationService.GetUserIdUserName(
                messges.Select(c => c.UserId).ToList());

        return messges.ToMessageDtos(userIdNameMap);
    }

    [HttpPut("{conversationId}/messages/{messageId}", Name = "UpdateMessage")]
    public async Task<ActionResult<GetMessageDto>> UpdateMessage(
        [FromRoute][NotEmptyGuid] Guid reviewId, [FromRoute][NotEmptyGuid] Guid conversationId,
        [FromRoute][NotEmptyGuid] Guid messageId, [Required] MessageDto newMessageDto)
    {
        _logger.LogDebug("Updating the message {messageId}.", messageId);
        var newMessage = newMessageDto.ToMessageModel(Utils.GetAzureUniqueId(HttpContext.User));

        var message = await _conversationService.UpdateMessage(messageId, newMessage);
        _logger.LogInformation("Updated the message {messageId} on the conversation {conversationId} of review {reviewId}.",
            messageId, conversationId, reviewId);

        var userName = await _conversationService.GetUserName(message.UserId);
        return message.ToMessageDto(userName);

    }

    [HttpDelete("{conversationId}/messages/{messageId}", Name = "DeleteMessage")]
    public async Task<ActionResult> DeleteMessage([FromRoute][NotEmptyGuid] Guid reviewId,
        [FromRoute][NotEmptyGuid] Guid conversationId, [FromRoute][NotEmptyGuid] Guid messageId)
    {
        _logger.LogDebug("Deleting the message {messageId} on conversation {conversationId}.", messageId, conversationId);
        await _conversationService.DeleteMessage(messageId, Utils.GetAzureUniqueId(HttpContext.User));
        _logger.LogInformation("Deleted the message {messageId} on conversation {conversationId} of review {reviewId}.",
            messageId, conversationId, reviewId);

        return NoContent();
    }

    private static bool ValidateProperty<T>(string propertyName)
    where T : class, new()
    {
        var obj = new T();
        var propertyInfo = obj.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance
            );

        return propertyInfo != null;
    }
}
