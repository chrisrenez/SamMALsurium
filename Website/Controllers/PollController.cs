using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels.Polls;
using SamMALsurium.Services.Polls;
using System.Security.Claims;

namespace SamMALsurium.Controllers;

[Authorize]
public class PollController : Controller
{
    private readonly IPollService _pollService;
    private readonly IVoteService _voteService;
    private readonly ILogger<PollController> _logger;

    public PollController(
        IPollService pollService,
        IVoteService voteService,
        ILogger<PollController> logger)
    {
        _pollService = pollService;
        _voteService = voteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden.";
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Check if user has voted
            var hasVoted = await _voteService.HasUserVotedAsync(id, userId);
            var userVote = hasVoted ? await _voteService.GetUserVoteAsync(id, userId) : null;

            // Determine if user can vote
            var canVote = poll.Status == PollStatus.Active &&
                          (poll.StartDate == null || poll.StartDate <= DateTime.UtcNow) &&
                          (poll.EndDate == null || poll.EndDate > DateTime.UtcNow);

            // Determine if user can change vote
            var canChangeVote = poll.AllowVoteChange && hasVoted && canVote;

            // Determine if user can view results
            var canViewResults = false;
            if (poll.ResultsVisibility == ResultsVisibility.RealTime)
            {
                canViewResults = true;
            }
            else if (poll.ResultsVisibility == ResultsVisibility.AfterVoting)
            {
                canViewResults = poll.Status == PollStatus.Closed || poll.Status == PollStatus.Archived || hasVoted;
            }
            else if (poll.ResultsVisibility == ResultsVisibility.CreatorOnly)
            {
                canViewResults = poll.CreatedById == userId || User.IsInRole("Admin");
            }

            var voteCount = await _pollService.GetVoteCountAsync(id);

            var model = new PollDetailsViewModel
            {
                Id = poll.Id,
                Title = poll.Title,
                Description = poll.Description,
                Type = poll.Type,
                Target = poll.Target,
                Status = poll.Status,
                StartDate = poll.StartDate,
                EndDate = poll.EndDate,
                IsAnonymous = poll.IsAnonymous,
                ResultsVisibility = poll.ResultsVisibility,
                AllowVoteChange = poll.AllowVoteChange,
                IsMultiSelect = poll.IsMultiSelect,
                ScoreMin = poll.ScoreMin,
                ScoreMax = poll.ScoreMax,
                Options = poll.Options.OrderBy(o => o.DisplayOrder).Select(o => new PollDetailsViewModel.PollOptionItem
                {
                    Id = o.Id,
                    Label = o.Label,
                    AdditionalInfo = o.AdditionalInfo,
                    DisplayOrder = o.DisplayOrder
                }).ToList(),
                EventId = poll.EventId,
                EventTitle = poll.Event?.Title,
                HasUserVoted = hasVoted,
                CanVote = canVote,
                CanChangeVote = canChangeVote,
                CanViewResults = canViewResults,
                UserVote = userVote,
                TotalVotes = voteCount
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading poll details for poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Laden der Umfrage.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(int id, [FromForm] Dictionary<string, string> formData)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden.";
                return RedirectToAction("Details", new { id });
            }

            // Check if poll is active and within time window
            if (poll.Status != PollStatus.Active)
            {
                TempData["ErrorMessage"] = "Diese Umfrage ist nicht aktiv.";
                return RedirectToAction("Details", new { id });
            }

            if (poll.StartDate.HasValue && poll.StartDate > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Diese Umfrage hat noch nicht begonnen.";
                return RedirectToAction("Details", new { id });
            }

            if (poll.EndDate.HasValue && poll.EndDate <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Diese Umfrage ist bereits beendet.";
                return RedirectToAction("Details", new { id });
            }

            // Check if user has already voted
            var hasVoted = await _voteService.HasUserVotedAsync(id, userId);
            if (hasVoted)
            {
                TempData["ErrorMessage"] = "Sie haben bereits abgestimmt. Verwenden Sie 'Stimme ändern' um Ihre Stimme zu aktualisieren.";
                return RedirectToAction("Details", new { id });
            }

            // Parse vote data based on poll type
            object voteData = ParseVoteData(poll, formData);

            // Cast vote
            await _voteService.CastVoteAsync(id, userId, voteData);

            TempData["SuccessMessage"] = "Ihre Stimme wurde erfolgreich gespeichert.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error casting vote for poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Speichern Ihrer Stimme. Bitte versuchen Sie es erneut.";
            return RedirectToAction("Details", new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeVote(int id, [FromForm] Dictionary<string, string> formData)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden.";
                return RedirectToAction("Details", new { id });
            }

            // Check if vote changing is allowed
            if (!poll.AllowVoteChange)
            {
                TempData["ErrorMessage"] = "Das Ändern der Stimme ist für diese Umfrage nicht erlaubt.";
                return RedirectToAction("Details", new { id });
            }

            // Check if poll is still active
            if (poll.Status != PollStatus.Active)
            {
                TempData["ErrorMessage"] = "Diese Umfrage ist nicht mehr aktiv.";
                return RedirectToAction("Details", new { id });
            }

            // Parse vote data
            object voteData = ParseVoteData(poll, formData);

            // Change vote
            await _voteService.ChangeVoteAsync(id, userId, voteData);

            TempData["SuccessMessage"] = "Ihre Stimme wurde erfolgreich aktualisiert.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing vote for poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Aktualisieren Ihrer Stimme.";
            return RedirectToAction("Details", new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawVote(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden.";
                return RedirectToAction("Details", new { id });
            }

            // Check if vote changing is allowed
            if (!poll.AllowVoteChange)
            {
                TempData["ErrorMessage"] = "Das Zurückziehen der Stimme ist für diese Umfrage nicht erlaubt.";
                return RedirectToAction("Details", new { id });
            }

            // Withdraw vote
            await _voteService.WithdrawVoteAsync(id, userId);

            TempData["SuccessMessage"] = "Ihre Stimme wurde erfolgreich zurückgezogen.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing vote for poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Zurückziehen Ihrer Stimme.";
            return RedirectToAction("Details", new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Results(int id)
    {
        try
        {
            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden.";
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get results with visibility check
            var results = await _voteService.GetPollResultsAsync(id, userId);

            var voteCount = await _pollService.GetVoteCountAsync(id);

            var model = new PollResultsViewModel
            {
                PollId = poll.Id,
                Title = poll.Title,
                Description = poll.Description,
                Type = poll.Type,
                Status = poll.Status,
                IsAnonymous = poll.IsAnonymous,
                TotalParticipants = voteCount,
                StartDate = poll.StartDate,
                EndDate = poll.EndDate,
                EventId = poll.EventId,
                EventTitle = poll.Event?.Title,
                Results = results,
                Options = poll.Options.OrderBy(o => o.DisplayOrder).Select(o => new PollResultsViewModel.PollOptionInfo
                {
                    Id = o.Id,
                    Label = o.Label,
                    AdditionalInfo = o.AdditionalInfo,
                    DisplayOrder = o.DisplayOrder
                }).ToList(),
                CanViewResults = true, // Already checked in GetPollResultsAsync
                CanExportResults = poll.CreatedById == userId || User.IsInRole("Admin")
            };

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Sie haben keine Berechtigung, die Ergebnisse dieser Umfrage anzusehen.";
            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading poll results for poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Laden der Ergebnisse.";
            return RedirectToAction("Details", new { id });
        }
    }

    private object ParseVoteData(Poll poll, Dictionary<string, string> formData)
    {
        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                // Form data contains "options[]" with selected option IDs
                var selectedOptions = formData
                    .Where(kvp => kvp.Key.StartsWith("options"))
                    .Select(kvp => int.Parse(kvp.Value))
                    .ToList();
                return new { SelectedOptions = selectedOptions };

            case PollType.RankedChoice:
                // Form data contains "rank_{optionId}" with rank values
                var rankings = formData
                    .Where(kvp => kvp.Key.StartsWith("rank_"))
                    .Select(kvp => new
                    {
                        OptionId = int.Parse(kvp.Key.Replace("rank_", "")),
                        Rank = int.Parse(kvp.Value)
                    })
                    .Where(r => r.Rank > 0) // Only include options that have been ranked
                    .OrderBy(r => r.Rank)
                    .ToList();
                return new { Rankings = rankings };

            case PollType.ScoreVoting:
                // Form data contains "score_{optionId}" with score values
                var scores = formData
                    .Where(kvp => kvp.Key.StartsWith("score_"))
                    .Select(kvp => new
                    {
                        OptionId = int.Parse(kvp.Key.Replace("score_", "")),
                        Score = int.Parse(kvp.Value)
                    })
                    .ToList();
                return new { Scores = scores };

            case PollType.AvailabilityGrid:
                // Form data contains "availability_{optionId}" with yes/maybe/no values
                var availability = formData
                    .Where(kvp => kvp.Key.StartsWith("availability_"))
                    .Select(kvp => new
                    {
                        OptionId = int.Parse(kvp.Key.Replace("availability_", "")),
                        Availability = Enum.Parse<Availability>(kvp.Value, ignoreCase: true)
                    })
                    .ToList();
                return new { Availability = availability };

            default:
                throw new NotSupportedException($"Poll type {poll.Type} is not supported");
        }
    }
}
