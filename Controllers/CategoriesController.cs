using JajkoBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        return await _db.Categories
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/categories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        return category;
    }

    private async Task<List<Category>> GetAllChildren(int parentId)
    {
        var children = await _db.Categories
            .Where(c => c.ParentCategory == parentId)
            .ToListAsync();

        var result = new List<Category>();

        foreach (var child in children)
        {
            result.Add(child);

            var grandchildren = await GetAllChildren(child.Id);

            result.AddRange(grandchildren);
        }

        return result;
    }


    // GET: api/categories/5/children
    [HttpGet("{id}/children")]
    public async Task<ActionResult<IEnumerable<Category>>> GetChildren(int id)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound();
        }

        var children = await GetAllChildren(id);

        return children;
    }



    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        _db.Categories.Add(category);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            category
        );
    }

    // PUT: api/categories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        var existingCategory = await _db.Categories.FindAsync(id);

        if (existingCategory == null)
        {
            return NotFound();
        }

        existingCategory.ParentCategory = category.ParentCategory;
        existingCategory.Name = category.Name;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound();
        }

        _db.Categories.Remove(category);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
