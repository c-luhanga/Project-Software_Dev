namespace UniShareProject.Repository.Data.Interfaces;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}