using System.Linq.Expressions;

namespace PatientTracker.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetByConditionsAsync(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}
