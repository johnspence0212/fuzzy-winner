using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Dtos;
using Api.Models;

namespace Api.Controllers;

[Route("api/workout-template")]
public class WorkoutTemplateController : BaseController<WorkoutTemplate>
{
    public WorkoutTemplateController(AppDbContext context) : base(context)
    {
    }

    [HttpGet("plan/{planId:int}")]
    public async Task<ActionResult<IEnumerable<WorkoutTemplate>>> GetByPlan(int planId)
    {
        var list = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises)
            .Where(t => t.PlanId == planId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        foreach (var template in list)
            template.Exercises = template.Exercises.OrderBy(e => e.SortOrder).ToList();

        return Ok(list);
    }

    [HttpPost("with-exercises")]
    public async Task<ActionResult<WorkoutTemplate>> CreateWithExercises([FromBody] CreateWorkoutTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");
        if (dto.Exercises is not { Count: > 0 })
            return BadRequest("At least one exercise is required.");

        var planExists = await _context.Plans.AnyAsync(p => p.Id == dto.PlanId);
        if (!planExists)
            return NotFound("Plan not found.");

        foreach (var ex in dto.Exercises.OrderBy(e => e.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(ex.Name))
                return BadRequest("Each exercise needs a name.");
        }

        var template = new WorkoutTemplate
        {
            PlanId = dto.PlanId,
            Name = dto.Name.Trim(),
            SortOrder = dto.SortOrder,
        };

        _context.WorkoutTemplates.Add(template);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("A workout with this name already exists for the plan.");
        }

        foreach (var ex in dto.Exercises.OrderBy(e => e.SortOrder))
        {
            _context.ExerciseDefinitions.Add(new ExerciseDefinition
            {
                WorkoutTemplateId = template.Id,
                Name = ex.Name.Trim(),
                SortOrder = ex.SortOrder,
                TargetSets = ex.TargetSets,
                TargetReps = ex.TargetReps,
                TargetWeightLbs = ex.TargetWeightLbs,
            });
        }

        await _context.SaveChangesAsync();

        var created = await _context.WorkoutTemplates
            .Include(t => t.Exercises)
            .FirstAsync(t => t.Id == template.Id);

        created.Exercises = created.Exercises.OrderBy(e => e.SortOrder).ToList();

        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:int}/with-exercises")]
    public async Task<ActionResult<WorkoutTemplate>> UpdateWithExercises(int id, [FromBody] UpdateWorkoutTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var template = await _context.WorkoutTemplates
            .Include(t => t.Exercises)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return NotFound();

        template.Name = dto.Name.Trim();
        template.SortOrder = dto.SortOrder;

        _context.ExerciseDefinitions.RemoveRange(template.Exercises);
        await _context.SaveChangesAsync();

        foreach (var ex in dto.Exercises.OrderBy(e => e.SortOrder))
        {
            _context.ExerciseDefinitions.Add(new ExerciseDefinition
            {
                WorkoutTemplateId = template.Id,
                Name = ex.Name.Trim(),
                SortOrder = ex.SortOrder,
                TargetSets = ex.TargetSets,
                TargetReps = ex.TargetReps,
                TargetWeightLbs = ex.TargetWeightLbs,
            });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("Could not update workout (name conflict or constraint).");
        }

        var updated = await _context.WorkoutTemplates
            .Include(t => t.Exercises)
            .FirstAsync(t => t.Id == id);

        updated.Exercises = updated.Exercises.OrderBy(e => e.SortOrder).ToList();

        return Ok(updated);
    }
}
