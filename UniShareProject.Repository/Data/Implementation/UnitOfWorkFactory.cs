using UniShareProject.Repository.Data.Interfaces;

namespace UniShareProject.Repository.Data.Implementation;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    public UnitOfWorkFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public IUnitOfWork Create() => new UnitOfWork(_connectionFactory);
}