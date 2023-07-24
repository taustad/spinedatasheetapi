using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;

namespace datasheetapi.Controllers;

[ApiController]
[Route("revisionreviews")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
[RequiresApplicationRoles(
    ApplicationRole.Admin,
    ApplicationRole.ReadOnlyUser,
    ApplicationRole.User
)]
public class RevisionContainerReviewsController : ControllerBase
{
    private readonly ILogger<RevisionContainerReviewsController> _logger;
    private readonly RevisionContainerReviewService _reviewService;

    public RevisionContainerReviewsController(ILoggerFactory loggerFactory, RevisionContainerReviewService reviewService)
    {
        _logger = loggerFactory.CreateLogger<RevisionContainerReviewsController>();
        _reviewService = reviewService;
    }

    [HttpGet("{id}", Name = "GetRevisionReview")]
    public async Task<ActionResult<RevisionContainerReviewDto>> GetRevisionReview([FromQuery] Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var review = await _reviewService.GetTagDataReview(id);
            if (review == null)
            {
                return NotFound();
            }
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revision review for id {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet(Name = "GetRevisionReviews")]
    public async Task<ActionResult<List<RevisionContainerReviewDto>>> GetRevisionReviews()
    {
        try
        {
            return await _reviewService.GetTagDataReviewDtos();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all revision reviews");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("tag/{id}", Name = "GetRevisionReviewsForTag")]
    public async Task<ActionResult<List<RevisionContainerReviewDto>>> GetRevisionReviewsForTag(Guid id)
    {
        try
        {
            return await _reviewService.GetReviewDtosForTag(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revision reviews for tag {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("project/{id}", Name = "GetRevisionReviewsForProject")]
    public async Task<ActionResult<List<RevisionContainerReviewDto>>> GetRevisionReviewsForProject([FromQuery] Guid id)
    {
        try
        {
            return await _reviewService.GetTagDataReviewDtosForProject(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revision reviews for project {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost(Name = "CreateRevisionReview")]
    public async Task<ActionResult<RevisionContainerReviewDto>> CreateRevisionReview([FromBody] RevisionContainerReviewDto review)
    {
        var httpContext = HttpContext;
        var user = httpContext.User;
        var fusionIdentity = user.Identities.FirstOrDefault(i => i is Fusion.Integration.Authentication.FusionIdentity) as Fusion.Integration.Authentication.FusionIdentity;
        var azureUniqueId = fusionIdentity?.Profile?.AzureUniqueId ?? throw new Exception("Could not get Azure Unique Id");

        if (review == null) { return BadRequest(); }

        try
        {
            return await _reviewService.CreateTagDataReview(review, azureUniqueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revision review");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
