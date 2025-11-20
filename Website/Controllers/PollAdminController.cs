using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels.Polls;
using SamMALsurium.Services.Polls;
using System.Globalization;
using System.Text;

namespace SamMALsurium.Controllers;

public class PollAdminController : BaseAdminController
{
    private readonly IPollService _pollService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PollAdminController> _logger;

    public PollAdminController(
        IPollService pollService,
        ApplicationDbContext context,
        ILogger<PollAdminController> logger)
    {
        _pollService = pollService;
        _context = context;
        _logger = logger;
    }

    // GET: /PollAdmin/Index
    public async Task<IActionResult> Index(
        PollStatus? status = null,
        PollType? type = null,
        int? eventId = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 25)
    {
        var query = _context.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .Include(p => p.Event)
            .AsQueryable();

        // Apply filters
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (eventId.HasValue)
        {
            query = query.Where(p => p.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Title.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var polls = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pollSummaries = new List<PollSummary>();
        foreach (var poll in polls)
        {
            var voteCount = await _pollService.GetVoteCountAsync(poll.Id);
            var canEdit = await _pollService.CanEditPollAsync(poll.Id);

            pollSummaries.Add(new PollSummary
            {
                Id = poll.Id,
                Title = poll.Title,
                Type = poll.Type,
                Status = poll.Status,
                EventTitle = poll.Event?.Title,
                EventId = poll.EventId,
                VoteCount = voteCount,
                CreatedAt = poll.CreatedAt,
                StartDate = poll.StartDate,
                EndDate = poll.EndDate,
                CreatedByName = poll.CreatedBy?.FirstName ?? poll.CreatedBy?.Email ?? "Unknown",
                CanEdit = canEdit
            });
        }

        var viewModel = new PollListViewModel
        {
            Polls = pollSummaries,
            Filters = new FilterOptions
            {
                Status = status,
                Type = type,
                EventId = eventId,
                SearchTerm = searchTerm
            },
            Pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            },
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    // GET: /PollAdmin/Create
    public IActionResult Create(int? eventId = null)
    {
        var viewModel = new CreatePollViewModel
        {
            EventId = eventId,
            Type = PollType.MultipleChoice,
            Target = PollTarget.Other,
            IsMultiSelect = true,
            ScoreMin = 1,
            ScoreMax = 5,
            IsAnonymous = false,
            ResultsVisibility = ResultsVisibility.AfterVoting,
            AllowVoteChange = true,
            Options = new List<PollOptionInput>
            {
                new PollOptionInput { DisplayOrder = 0 },
                new PollOptionInput { DisplayOrder = 1 }
            }
        };

        ViewBag.Events = _context.Events
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Title })
            .ToList();

        return View(viewModel);
    }

    // POST: /PollAdmin/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePollViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }

        // Validate options
        if (model.Options.Count < 2)
        {
            ModelState.AddModelError("Options", "Mindestens 2 Optionen sind erforderlich");
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }

        if (model.Options.Count > 50)
        {
            ModelState.AddModelError("Options", "Maximal 50 Optionen sind erlaubt");
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }

        // Validate date range
        if (model.StartDate.HasValue && model.EndDate.HasValue && model.StartDate >= model.EndDate)
        {
            ModelState.AddModelError("EndDate", "Enddatum muss nach dem Startdatum liegen");
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }

        try
        {
            var poll = new Poll
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                Target = model.Target,
                EventId = model.EventId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsAnonymous = model.IsAnonymous,
                ResultsVisibility = model.ResultsVisibility,
                AllowVoteChange = model.AllowVoteChange,
                IsMultiSelect = model.IsMultiSelect,
                ScoreMin = model.ScoreMin,
                ScoreMax = model.ScoreMax,
                Status = PollStatus.Active,
                CreatedById = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                Options = model.Options.Select((opt, index) => new PollOption
                {
                    Label = opt.Label,
                    AdditionalInfo = opt.AdditionalInfo,
                    DisplayOrder = index
                }).ToList()
            };

            await _pollService.CreatePollAsync(poll);

            TempData["SuccessMessage"] = "Umfrage erfolgreich erstellt";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating poll");
            ModelState.AddModelError("", "Fehler beim Erstellen der Umfrage");
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }
    }

    // GET: /PollAdmin/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var poll = await _pollService.GetPollByIdAsync(id);
        if (poll == null)
        {
            TempData["ErrorMessage"] = "Umfrage nicht gefunden";
            return RedirectToAction(nameof(Index));
        }

        var voteCount = await _pollService.GetVoteCountAsync(id);
        var hasVotes = voteCount > 0;

        var viewModel = new EditPollViewModel
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            Type = poll.Type,
            Target = poll.Target,
            EventId = poll.EventId,
            StartDate = poll.StartDate,
            EndDate = poll.EndDate,
            IsAnonymous = poll.IsAnonymous,
            ResultsVisibility = poll.ResultsVisibility,
            AllowVoteChange = poll.AllowVoteChange,
            IsMultiSelect = poll.IsMultiSelect,
            ScoreMin = poll.ScoreMin,
            ScoreMax = poll.ScoreMax,
            Options = poll.Options.OrderBy(o => o.DisplayOrder).Select(o => new PollOptionInput
            {
                Label = o.Label,
                AdditionalInfo = o.AdditionalInfo,
                DisplayOrder = o.DisplayOrder
            }).ToList(),
            HasVotes = hasVotes,
            VoteCount = voteCount
        };

        ViewBag.Events = _context.Events
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Title })
            .ToList();

        return View(viewModel);
    }

    // POST: /PollAdmin/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPollViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Ungültige Anfrage";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }

        var poll = await _pollService.GetPollByIdAsync(id);
        if (poll == null)
        {
            TempData["ErrorMessage"] = "Umfrage nicht gefunden";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var voteCount = await _pollService.GetVoteCountAsync(id);
            var hasVotes = voteCount > 0;

            // Update allowed fields
            poll.Title = model.Title;
            poll.Description = model.Description;
            poll.EndDate = model.EndDate;
            poll.IsAnonymous = model.IsAnonymous;
            poll.ResultsVisibility = model.ResultsVisibility;
            poll.AllowVoteChange = model.AllowVoteChange;

            // If no votes, allow full editing
            if (!hasVotes)
            {
                poll.Type = model.Type;
                poll.Target = model.Target;
                poll.EventId = model.EventId;
                poll.StartDate = model.StartDate;
                poll.IsMultiSelect = model.IsMultiSelect;
                poll.ScoreMin = model.ScoreMin;
                poll.ScoreMax = model.ScoreMax;

                // Update options
                _context.PollOptions.RemoveRange(poll.Options);
                poll.Options = model.Options.Select((opt, index) => new PollOption
                {
                    PollId = poll.Id,
                    Label = opt.Label,
                    AdditionalInfo = opt.AdditionalInfo,
                    DisplayOrder = index
                }).ToList();
            }

            await _pollService.UpdatePollAsync(poll);

            TempData["SuccessMessage"] = "Umfrage erfolgreich aktualisiert";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating poll {PollId}", id);
            ModelState.AddModelError("", "Fehler beim Aktualisieren der Umfrage");
            ViewBag.Events = _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.Title })
                .ToList();
            return View(model);
        }
    }

    // POST: /PollAdmin/Close/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _pollService.ClosePollAsync(id, userId);
            TempData["SuccessMessage"] = "Umfrage erfolgreich geschlossen";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Schließen der Umfrage";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /PollAdmin/Archive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _pollService.ArchivePollAsync(id, userId);
            TempData["SuccessMessage"] = "Umfrage erfolgreich archiviert";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving poll {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Archivieren der Umfrage";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /PollAdmin/Statistics
    public async Task<IActionResult> Statistics()
    {
        try
        {
            var stats = await _pollService.GetPollStatisticsAsync();

            var viewModel = new PollStatisticsViewModel
            {
                TotalPolls = (int)stats["totalPolls"],
                ActivePolls = (int)stats["activePolls"],
                ClosedPolls = (int)stats["closedPolls"],
                ArchivedPolls = (int)stats["archivedPolls"],
                TotalVotes = (int)stats["totalVotes"],
                MostActivePolls = ((List<dynamic>)stats["mostActivePolls"]).Select(p => new MostActivePoll
                {
                    PollId = p.PollId,
                    Title = p.Title,
                    VoteCount = p.VoteCount
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading poll statistics");
            TempData["ErrorMessage"] = "Fehler beim Laden der Statistiken";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /PollAdmin/ExportResults/5
    public async Task<IActionResult> ExportResults(int id)
    {
        try
        {
            var poll = await _pollService.GetPollByIdAsync(id);
            if (poll == null)
            {
                TempData["ErrorMessage"] = "Umfrage nicht gefunden";
                return RedirectToAction(nameof(Index));
            }

            var csvData = await _pollService.ExportResultsToCsvAsync(id);
            var fileName = $"poll-{id}-results-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting poll results {PollId}", id);
            TempData["ErrorMessage"] = "Fehler beim Exportieren der Ergebnisse";
            return RedirectToAction(nameof(Index));
        }
    }
}
