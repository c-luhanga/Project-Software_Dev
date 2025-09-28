using UniShareProject.Repository.Data;

namespace UniShareProject.Repository.Data;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    public UnitOfWorkFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public IUnitOfWork Create() => new UnitOfWork(_connectionFactory);
}