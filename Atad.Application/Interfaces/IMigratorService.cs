namespace Atad.Application.Interfaces;

public interface IMigratorService
{
    public Task AddOrderNumbersIfTheyDoesntExistAsync();
}
