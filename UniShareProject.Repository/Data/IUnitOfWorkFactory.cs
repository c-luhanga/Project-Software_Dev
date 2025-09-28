namespace UniShareProject.Repository.Data;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}