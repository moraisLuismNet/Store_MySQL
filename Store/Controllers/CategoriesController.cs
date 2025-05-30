﻿using Store.DTOs;
using Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Services;
using Microsoft.AspNetCore.Authorization;

namespace Store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly StoreContext _context;
        private readonly ActionsService _actionsService;

        public CategoriesController(StoreContext context, ActionsService actionsService)
        {
            _context = context;
            _actionsService = actionsService;
        }

        [HttpGet]
        public async Task<ActionResult> GetCategories()
        {
            await _actionsService.AddAction("Get categories", "Categories");
            var categories = await (from x in _context.Categories
                                    select new CategoryDTO
                                    {
                                        IdCategory = x.IdCategory,
                                        CategoryName = x.NameCategory,
                                        TotalProducts = x.Products.Count()
                                    }).ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryItemDTO>> GetCategoryById(int id)
        {
            await _actionsService.AddAction("Get categories by id", "Categories");
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.IdCategory == id);

            if (category == null)
                return NotFound();

            var result = new CategoryItemDTO
            {
                CategoryName = category.NameCategory
            };

            return Ok(result);
        }

        [HttpGet("orderNameCategory/{desc}")]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategoriesOrderName(bool desc)
        {
            await _actionsService.AddAction("Get categories by order (name)", "Categories");
            List<Category> categories;

            if (desc)
            {
                categories = await _context.Categories
                                           .Include(c => c.Products)
                                           .OrderBy(x => x.NameCategory)
                                           .ToListAsync();
            }
            else
            {
                categories = await _context.Categories
                                           .Include(c => c.Products)
                                           .OrderByDescending(x => x.NameCategory)
                                           .ToListAsync();
            }

            return Ok(categories);
        }

        [HttpGet("nameCategory/contains/{text}")]
        public async Task<ActionResult<List<CategoryDTO>>> GetNameCategory(string text)
        {
            await _actionsService.AddAction("Get categories containing (name)", "Categories");
            var categories = await _context.Categories
                .Where(x => x.NameCategory.Contains(text))
                .Include(x => x.Products)
                .ToListAsync();

            var categoriesDTO = categories.Select(c => new CategoryDTO
            {
                IdCategory = c.IdCategory,
                CategoryName = c.NameCategory,
                Products = c.Products.Select(p => new ProductDTO
                {
                    IdProduct = p.IdProduct,
                    ProductName = p.NameProduct,
                    Price = p.Price,
                    DateUp = p.DateUp,
                    Discontinued = p.Discontinued,
                    PhotoUrl = p.PhotoUrl
                }).ToList()
            }).ToList();

            return categoriesDTO;
        }

        [HttpGet("paginacion/{page?}")]
        public async Task<ActionResult> GetCategoriesPagination(int page = 1)
        {
            await _actionsService.AddAction("Get paginated categories", "Categories");
            int recordsPerPage = 2;

            var totalCategories = await _context.Categories.CountAsync();

            var categories = await _context.Categories
                .Include(x => x.Products)
                .Skip((page - 1) * recordsPerPage)
                .Take(recordsPerPage)
                .ToListAsync();

            var categoriesDTO = categories.Select(c => new CategoryDTO
            {
                IdCategory = c.IdCategory,
                CategoryName = c.NameCategory,
                Products = c.Products.Select(p => new ProductDTO
                {
                    IdProduct = p.IdProduct,
                    ProductName = p.NameProduct,
                    Price = p.Price,
                    DateUp = p.DateUp,
                    Discontinued = p.Discontinued,
                    PhotoUrl = p.PhotoUrl
                }).ToList()
            }).ToList();

            var totalPages = (int)Math.Ceiling(totalCategories / (double)recordsPerPage);

            return Ok(new { categories = categoriesDTO, totalPages });
        }


        [HttpGet("pagination/{from}/{until}")]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategoriesFromUntil(int from, int until)
        {
            await _actionsService.AddAction("Get paginated categories from|until", "Categories");
            if (from < 1)
            {
                return BadRequest("The minimum must be greater than 0");
            }
            if (until < from)
            {
                return BadRequest("The maximum cannot be less than the minimum");
            }

            var categories = await _context.Categories
                .Include(c => c.Products)
                .Skip(from - 1)
                .Take(until - from + 1)
                .ToListAsync();

            var categoriesDTO = categories.Select(c => new CategoryDTO
            {
                IdCategory = c.IdCategory,
                CategoryName = c.NameCategory,
                Products = c.Products.Select(p => new ProductDTO
                {
                    IdProduct = p.IdProduct,
                    ProductName = p.NameProduct,
                    Price = p.Price,
                    DateUp = p.DateUp,
                    Discontinued = p.Discontinued,
                    PhotoUrl = p.PhotoUrl
                }).ToList()
            }).ToList();

            return Ok(categories);
        }

        [HttpGet("categoriesProductsSelect/{id:int}")]
        public async Task<ActionResult<Category>> GetCategoriesProductsSelect(int id)
        {
            await _actionsService.AddAction("Get categories and products", "Categories");
            var category = await (from x in _context.Categories
                                   select new CategoryProductDTO
                                   {
                                       IdCategory = x.IdCategory,
                                       CategoryName = x.NameCategory,
                                       TotalProducts = x.Products.Count(),
                                       Products = x.Products.Select(y => new ProductItemDTO
                                       {
                                           IdProduct = y.IdProduct,
                                           ProductName = y.NameProduct
                                       }).ToList(),
                                   }).FirstOrDefaultAsync(x => x.IdCategory == id);

            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpGet("stored_procedure/{id:int}")]
        public async Task<ActionResult<Category>> GetStoredProcedure(int id)
        {
            try
            {
                await _actionsService.AddAction("Getting Categories with a Stored Procedure", "Categories");

                var categories = _context.Categories
                    .FromSqlInterpolated($"EXEC Categories_GetById {id}")
                    .IgnoreQueryFilters()
                    .AsAsyncEnumerable();

                await foreach (var category in categories)
                {
                    return category;
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> PostCategory(CategoryInsertDTO category)
        {
            await _actionsService.AddAction("Add category", "Categories");
            var newCategory = new Category()
            {
                NameCategory = category.CategoryName
            };

            await _context.AddAsync(newCategory);
            await _context.SaveChangesAsync();

            return Created("Category", new { category = newCategory });
        }

        [Authorize]
        [HttpPost("stored_procedure")]
        public async Task<ActionResult> PostStoredProcedure(CategoryInsertDTO category)
        {
            try
            {
                await _actionsService.AddAction("Add category with sp", "Categories");
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "EXEC Categories_Insert @categoryName, @id OUTPUT";
                command.CommandType = System.Data.CommandType.Text;

                // Input parameter
                var paramName = command.CreateParameter();
                paramName.ParameterName = "@categoryName";
                paramName.Value = category.CategoryName;
                paramName.DbType = System.Data.DbType.String;
                command.Parameters.Add(paramName);

                // Output parameter
                var paramId = command.CreateParameter();
                paramId.ParameterName = "@id";
                paramId.DbType = System.Data.DbType.Int32;
                paramId.Direction = System.Data.ParameterDirection.Output;
                command.Parameters.Add(paramId);

                await command.ExecuteNonQueryAsync();

                // Get the generated ID
                var id = (paramId.Value != DBNull.Value) ? (int)paramId.Value : 0;

                return Ok(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [Authorize]
        [HttpPut("{idCategory:int}")]
        public async Task<IActionResult> PutCategory(int idCategory, [FromBody] CategoryUpdateDTO category)
        {
            await _actionsService.AddAction("Update category", "Categories");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (idCategory != category.IdCategory)
            {
                return BadRequest(new { message = "The route ID does not match the body ID" });
            }

            var categoryUpdate = await _context.Categories
                .AsTracking()
                .FirstOrDefaultAsync(x => x.IdCategory == idCategory);

            if (categoryUpdate == null)
            {
                return NotFound(new { message = "The category was not found" });
            }

            categoryUpdate.NameCategory = category.CategoryName;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating category", details = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _actionsService.AddAction("Delete category", "Categories");
            var thereAreProducts = await _context.Products.AnyAsync(x => x.CategoryId == id);
            if (thereAreProducts)
            {
                return BadRequest("There are related products");
            }
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.IdCategory == id);

            if (category is null)
            {
                return NotFound();
            }

            _context.Remove(category);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}