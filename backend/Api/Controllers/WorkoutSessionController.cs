using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Dtos;
using Api.Models;

namespace Api.Controllers;

[Route("api/workout-session")]
[ApiController]
public class WorkoutSessionController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkoutSessionController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<WorkoutSessionResponseDto>> Post([FromBody] CreateWorkoutSessionDto dto)
    {
        var plan = await _context.Plans.FindAsync(dto.PlanId);
        if (plan == null)
            return NotFound("Plan not found.");

        var template = await _context.WorkoutTemplates
            .FirstOrDefaultAsync(t => t.Id == dto.WorkoutTemplateId && t.PlanId == dto.PlanId);
        if (template == null)
            return BadRequest("Workout template not found for this plan.");

        var exerciseIds = dto.Sets
            .Where(s => s.ExerciseDefinitionId.HasValue)
            .Select(s => s.ExerciseDefinitionId!.Value)
            .Distinct()
            .ToList();

        if (exerciseIds.Count > 0)
        {
            var validIds = await _context.ExerciseDefinitions
                .Where(e => e.WorkoutTemplateId == dto.WorkoutTemplateId && exerciseIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            if (validIds.Count != exerciseIds.Count)
                return BadRequest("One or more ExerciseDefinitionId values are invalid for this template.");
        }

        var exercisesById = await _context.ExerciseDefinitions
            .Where(e => e.WorkoutTemplateId == dto.WorkoutTemplateId)
            .ToDictionaryAsync(e => e.Id, e => e.Name);

        var performedAt = dto.PerformedAt ?? DateTime.UtcNow;
        if (performedAt.Kind == DateTimeKind.Unspecified)
            performedAt = DateTime.SpecifyKind(performedAt, DateTimeKind.Utc);
        else if (performedAt.Kind == DateTimeKind.Local)
            performedAt = performedAt.ToUniversalTime();

        var session = new WorkoutSession
        {
            PlanId = dto.PlanId,
            WorkoutTemplateId = dto.WorkoutTemplateId,
            PerformedAt = performedAt,
            Notes = dto.Notes,
        };

        _context.WorkoutSessions.Add(session);
        await _context.SaveChangesAsync();

        foreach (var s in dto.Sets.OrderBy(s => s.SetIndex))
        {
            string? snapshot = s.ExerciseNameSnapshot;
            if (s.ExerciseDefinitionId.HasValue && exercisesById.TryGetValue(s.ExerciseDefinitionId.Value, out var n))
                snapshot ??= n;

            _context.PerformedSets.Add(new PerformedSet
            {
                WorkoutSessionId = session.Id,
                ExerciseDefinitionId = s.ExerciseDefinitionId,
                ExerciseNameSnapshot = snapshot,
                SetIndex = s.SetIndex,
                Reps = s.Reps,
                WeightKg = s.WeightKg,
            });
        }

        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await MapSession(session.Id));
    }

    [HttpGet("plan/{planId:int}")]
    public async Task<ActionResult<IEnumerable<WorkoutSessionResponseDto>>> GetForPlan(int planId, [FromQuery] int take = 50)
    {
        var exists = await _context.Plans.AnyAsync(p => p.Id == planId);
        if (!exists)
            return NotFound();

        take = Math.Clamp(take, 1, 200);

        var ids = await _context.WorkoutSessions
            .AsNoTracking()
            .Where(s => s.PlanId == planId)
            .OrderByDescending(s => s.PerformedAt)
            .Select(s => s.Id)
            .Take(take)
            .ToListAsync();

        var list = new List<WorkoutSessionResponseDto>();
        foreach (var id in ids)
            list.Add(await MapSession(id));

        return Ok(list);
    }

    private async Task<WorkoutSessionResponseDto> MapSession(int sessionId)
    {
        var s = await _context.WorkoutSessions
            .AsNoTracking()
            .Include(x => x.WorkoutTemplate)
            .Include(x => x.PerformedSets.OrderBy(p => p.SetIndex))
            .FirstAsync(x => x.Id == sessionId);

        return new WorkoutSessionResponseDto
        {
            Id = s.Id,
            CreatedAt = s.CreatedAt,
            PlanId = s.PlanId,
            WorkoutTemplateId = s.WorkoutTemplateId,
            WorkoutTemplateName = s.WorkoutTemplate.Name,
            PerformedAt = s.PerformedAt,
            Notes = s.Notes,
            Sets = s.PerformedSets.Select(p => new PerformedSetResponseDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                ExerciseDefinitionId = p.ExerciseDefinitionId,
                ExerciseNameSnapshot = p.ExerciseNameSnapshot,
                SetIndex = p.SetIndex,
                Reps = p.Reps,
                WeightKg = p.WeightKg,
            }).ToList(),
        };
    }
}
