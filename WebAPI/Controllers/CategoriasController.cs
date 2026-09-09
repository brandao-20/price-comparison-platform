using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Entities;
using WebAPI.Repositories;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        [HttpGet]
        [AllowAnonymous] // Permitir acesso sem autenticação
        public async Task<ActionResult<ApiResponse<IEnumerable<Categoria>>>> GetAll()
        {
            try
            {
                var categorias = await _categoriaRepository.GetAllAsync();
                // Organizar as categorias em uma estrutura hierárquica
                var categoriasHierarquicas = BuildCategoryHierarchy(categorias);
                return Ok(new ApiResponse<IEnumerable<Categoria>>
                {
                    Success = true,
                    Message = "Categorias carregadas com sucesso.",
                    StatusCode = 200,
                    Data = categoriasHierarquicas
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<CategoriasController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<IEnumerable<Categoria>>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Categoria>>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<Categoria>
                    {
                        Success = false,
                        Message = "ID da categoria inválido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var categoria = await _categoriaRepository.GetByIdAsync(id);
                if (categoria == null)
                {
                    return NotFound(new ApiResponse<Categoria>
                    {
                        Success = false,
                        Message = "Categoria não encontrada.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<Categoria>
                {
                    Success = true,
                    Message = "Categoria encontrada com sucesso.",
                    StatusCode = 200,
                    Data = categoria
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<CategoriasController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<Categoria>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Categoria>>> Create(Categoria categoria)
        {

            try
            {
                if (categoria == null || string.IsNullOrEmpty(categoria.Nome))
                {
                    return BadRequest(new ApiResponse<Categoria>
                    {
                        Success = false,
                        Message = "Dados da categoria inválidos.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                if (categoria.ParentId.HasValue && categoria.ParentId.Value > 0)
                {
                    var parent = await _categoriaRepository.GetByIdAsync(categoria.ParentId.Value);
                    if (parent == null)
                    {
                        return BadRequest(new ApiResponse<Categoria>
                        {
                            Success = false,
                            Message = "Categoria pai não encontrada.",
                            StatusCode = 400,
                            Data = null
                        });
                    }
                }

                await _categoriaRepository.AddAsync(categoria);
                return CreatedAtAction(nameof(GetById), new { id = categoria.CategoriaId }, new ApiResponse<Categoria>
                {
                    Success = true,
                    Message = "Categoria criada com sucesso.",
                    StatusCode = 201,
                    Data = categoria
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<CategoriasController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<Categoria>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, Categoria categoria)
        {

            try
            {
                if (id <= 0 || id != categoria.CategoriaId)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID da categoria inválido ou não corresponde ao objeto fornecido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var existingCategoria = await _categoriaRepository.GetByIdAsync(id);
                if (existingCategoria == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Categoria não encontrada.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                if (categoria.ParentId.HasValue && categoria.ParentId.Value > 0)
                {
                    var parent = await _categoriaRepository.GetByIdAsync(categoria.ParentId.Value);
                    if (parent == null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Categoria pai não encontrada.",
                            StatusCode = 400,
                            Data = null
                        });
                    }
                    if (categoria.ParentId.Value == categoria.CategoriaId)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Uma categoria não pode ser pai de si mesma.",
                            StatusCode = 400,
                            Data = null
                        });
                    }
                }

                await _categoriaRepository.UpdateAsync(categoria);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Categoria atualizada com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Categoria não encontrada.",
                    StatusCode = 404,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<CategoriasController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {

            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ID da categoria inválido.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                var categoria = await _categoriaRepository.GetByIdAsync(id);
                if (categoria == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Categoria não encontrada.",
                        StatusCode = 404,
                        Data = null
                    });
                }

                var subcategorias = await _categoriaRepository.GetAllAsync();
                if (subcategorias.Any(c => c.ParentId == id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Não é possível excluir uma categoria que possui subcategorias.",
                        StatusCode = 400,
                        Data = null
                    });
                }

                await _categoriaRepository.DeleteAsync(categoria);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Categoria excluída com sucesso.",
                    StatusCode = 200,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<CategoriasController>>().LogError(ex, "The request failed.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "The request could not be completed. Please try again later.",
                    StatusCode = 500,
                    Data = null
                });
            }
        }

        private IEnumerable<Categoria> BuildCategoryHierarchy(IEnumerable<Categoria> categorias)
        {
            var lookup = categorias.ToLookup(c => c.ParentId);
            foreach (var categoria in categorias)
            {
                categoria.SubCategorias = lookup[categoria.CategoriaId].ToList();
            }
            return lookup[null];
        }
    }
}
