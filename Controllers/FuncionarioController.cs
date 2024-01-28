using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    private readonly string _connectionString;
    private readonly string _tableName;

    public FuncionarioController(RHContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
        _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
    }

    private TableClient GetTableClient()
    {
        var serviceClient = new TableServiceClient(_connectionString);
        var tableClient = serviceClient.GetTableClient(_tableName);

        tableClient.CreateIfNotExists();
        return tableClient;
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        // Busca um funcionário no banco de dados pelo ID fornecido.
        var funcionario = _context.Funcionarios.Find(id);

        // Se o funcionário não for encontrado, retorna um status HTTP 404 (Not Found).
        if (funcionario == null)
            return NotFound();

        // Se o funcionário for encontrado, retorna um status HTTP 200 (OK) e os dados do funcionário.
        return Ok(funcionario);
    }

    [HttpPost]
    public IActionResult Criar(Funcionario funcionario)
    {
        // Adiciona o novo funcionário ao banco de dados e salva as alterações.
        _context.Funcionarios.Add(funcionario);
        _context.SaveChanges();

        // Cria um cliente para a tabela do Azure.
        var tableClient = GetTableClient();

        // Cria um log da ação de inclusão do funcionário.
        var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, funcionario.Departamento, Guid.NewGuid().ToString());

        // Insere ou atualiza a entidade de log na tabela do Azure.
        tableClient.UpsertEntity(funcionarioLog);

        // Retorna um status HTTP 201 (Created) e a localização do novo funcionário.
        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, Funcionario funcionario)
    {
        // Busca um funcionário no banco de dados pelo ID fornecido.
        var funcionarioBanco = _context.Funcionarios.Find(id);

        // Se o funcionário não for encontrado, retorna um status HTTP 404 (Not Found).
        if (funcionarioBanco == null)
            return NotFound();

        // Atualiza os dados do funcionário encontrado com os dados do funcionário fornecido.
        funcionarioBanco.Nome = funcionario.Nome;
        funcionarioBanco.Endereco = funcionario.Endereco;
        funcionarioBanco.Ramal = funcionario.Ramal;
        funcionarioBanco.EmailProfissional = funcionario.EmailProfissional;
        funcionarioBanco.Departamento = funcionario.Departamento;
        funcionarioBanco.Salario = funcionario.Salario;

        // Atualiza o funcionário no banco de dados e salva as alterações.
        _context.Funcionarios.Update(funcionarioBanco);
        _context.SaveChanges();

        // Cria um cliente para a tabela do Azure.
        var tableClient = GetTableClient();

        // Cria um log da ação de atualização do funcionário.
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        // Insere ou atualiza a entidade de log na tabela do Azure.
        tableClient.UpsertEntity(funcionarioLog);

        // Retorna um status HTTP 200 (OK) para indicar que a operação foi bem-sucedida.
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(int id)
    {
        // Busca um funcionário no banco de dados pelo ID fornecido.
        var funcionarioBanco = _context.Funcionarios.Find(id);

        // Se o funcionário não for encontrado, retorna um status HTTP 404 (Not Found).
        if (funcionarioBanco == null)
            return NotFound();

        // Remove o funcionário encontrado do banco de dados e salva as alterações.
        _context.Funcionarios.Remove(funcionarioBanco);
        _context.SaveChanges();

        // Cria um cliente para a tabela do Azure.
        var tableClient = GetTableClient();

        // Cria um log da ação de remoção do funcionário.
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        // Insere ou atualiza a entidade de log na tabela do Azure.
        tableClient.UpsertEntity(funcionarioLog);

        // Retorna um status HTTP 204 (No Content) para indicar que a operação foi bem-sucedida, mas não há conteúdo para retornar.
        return NoContent();
    }
}
