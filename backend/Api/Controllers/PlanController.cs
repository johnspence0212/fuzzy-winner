using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Dtos;
using Api.Models;

namespace Api.Controllers;

[Route("api/plan")]
public class PlanController : BaseController<Plan>
{
    public PlanController(AppDbContext context) : base(context)
    {
    }

    [HttpGet("{planId:int}/schedule")]
    public async Task<ActionResult<ScheduleResponseDto>> GetSchedule(int planId)
    {
        var planExists = await _context.Plans.AnyAsync(p => p.Id == planId);
        if (!planExists)
            return NotFound();

        var cycle = await _context.ScheduleCycles
            .AsNoTracking()
            .Include(c => c.ScheduledSessions)
            .ThenInclude(s => s.WorkoutTemplate)
            .FirstOrDefaultAsync(c => c.PlanId == planId);

        if (cycle == null)
        {
            return Ok(new ScheduleResponseDto
            {
                PlanId = planId,
                SessionsPerWeek = 0,
                RepeatWeeks = 0,
                Slots = new List<ScheduleSlotResponseDto>(),
            });
        }

        var slots = cycle.ScheduledSessions
            .OrderBy(s => s.WeekIndex)
            .ThenBy(s => s.SessionIndex)
            .Select(s => new ScheduleSlotResponseDto
            {
                WeekIndex = s.WeekIndex,
                SessionIndex = s.SessionIndex,
                WorkoutTemplateId = s.WorkoutTemplateId,
                WorkoutTemplateName = s.WorkoutTemplate.Name,
            })
            .ToList();

        return Ok(new ScheduleResponseDto
        {
            PlanId = planId,
            SessionsPerWeek = cycle.SessionsPerWeek,
            RepeatWeeks = cycle.RepeatWeeks,
            Slots = slots,
        });
    }

    [HttpPut("{planId:int}/schedule")]
    public async Task<ActionResult<ScheduleResponseDto>> PutSchedule(int planId, [FromBody] SchedulePutDto dto)
    {
        var planExists = await _context.Plans.AnyAsync(p => p.Id == planId);
        if (!planExists)
            return NotFound();

        if (dto.SessionsPerWeek < 1 || dto.RepeatWeeks < 1)
            return BadRequest("SessionsPerWeek and RepeatWeeks must be at least 1.");

        var expected = dto.RepeatWeeks * dto.SessionsPerWeek;
        if (dto.Slots.Count != expected)
            return BadRequest($"Expected {expected} slots (RepeatWeeks × SessionsPerWeek).");

        var seen = new HashSet<(int Week, int Session)>();
        foreach (var slot in dto.Slots)
        {
            if (slot.WeekIndex < 0 || slot.WeekIndex >= dto.RepeatWeeks)
                return BadRequest("Invalid WeekIndex.");
            if (slot.SessionIndex < 0 || slot.SessionIndex >= dto.SessionsPerWeek)
                return BadRequest("Invalid SessionIndex.");
            if (!seen.Add((slot.WeekIndex, slot.SessionIndex)))
                return BadRequest("Duplicate week/session slot.");
        }

        if (seen.Count != expected)
            return BadRequest("Missing slots for some week/session combinations.");

        var validTemplateIds = await _context.WorkoutTemplates
            .Where(t => t.PlanId == planId)
            .Select(t => t.Id)
            .ToListAsync();

        var validSet = validTemplateIds.ToHashSet();
        foreach (var slot in dto.Slots)
        {
            if (!validSet.Contains(slot.WorkoutTemplateId))
                return BadRequest("WorkoutTemplateId does not belong to this plan.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.ScheduleCycles
                .Include(c => c.ScheduledSessions)
                .FirstOrDefaultAsync(c => c.PlanId == planId);

            if (existing != null)
            {
                _context.ScheduledSessions.RemoveRange(existing.ScheduledSessions);
                _context.ScheduleCycles.Remove(existing);
                await _context.SaveChangesAsync();
            }

            var cycle = new ScheduleCycle
            {
                PlanId = planId,
                SessionsPerWeek = dto.SessionsPerWeek,
                RepeatWeeks = dto.RepeatWeeks,
            };
            _context.ScheduleCycles.Add(cycle);
            await _context.SaveChangesAsync();

            foreach (var slot in dto.Slots.OrderBy(s => s.WeekIndex).ThenBy(s => s.SessionIndex))
            {
                _context.ScheduledSessions.Add(new ScheduledSession
                {
                    ScheduleCycleId = cycle.Id,
                    WeekIndex = slot.WeekIndex,
                    SessionIndex = slot.SessionIndex,
                    WorkoutTemplateId = slot.WorkoutTemplateId,
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return await GetSchedule(planId);
    }
}
