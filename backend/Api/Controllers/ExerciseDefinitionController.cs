using Microsoft.AspNetCore.Mvc;
using Api.Data;
using Api.Models;

namespace Api.Controllers;

[Route("api/exercise-definition")]
public class ExerciseDefinitionController : BaseController<ExerciseDefinition>
{
    public ExerciseDefinitionController(AppDbContext context) : base(context)
    {
    }
}
