using Core.Entities;
using Core.Interfaces;
using KanchanaburiPlatform.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class GenericRepository<T>(StoreContext context) : IGenericRepository<T> where T : class
{
    public async Task<T?> GetByIdAsync(params object[] keyValues)
    {
        return await context.Set<T>().FindAsync(keyValues);
    }

    public async Task<IReadOnlyList<T>> ListAllAsync()
    {
        return await context.Set<T>().ToListAsync();
    }

    public void Add(T entity)
    {
        context.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        context.Set<T>().Attach(entity); //บอก EF Core ว่า "มี object นี้อยู่แล้ว ไม่ต้องดึงจากฐานข้อมูลใหม่
        context.Entry(entity).State = EntityState.Modified; //Update object เดี่ยวอย่างแม่นยำ
    }

    public void Remove(T entity)
    {
        context.Set<T>().Remove(entity);
    }

    //public bool Exists(int id)
    //{
    //    return context.Set<T>().Any(x => x.Id == id);
    //}

    public async Task<T?> GetEntityWithSpec(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    //4.GenericRepository<T> – ใช้ ApplySpecification(spec) เพื่อดึงข้อมูลจาก DB
    private IQueryable<T> ApplySpecification(ISpecification<T> spec) //สร้างเมธอดเพื่อใช้กับ SpecificationEvaluator
    {
        return SpecificationEvaluator<T>.GetQuery(context.Set<T>().AsQueryable(), spec);
    }

    private IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> spec)
    {
        return SpecificationEvaluator<T>.GetQuery<T, TResult>(context.Set<T>().AsQueryable(), spec);
    }

    public async Task<TResult?> GetEntityWithSpec<TResult>(ISpecification<T, TResult> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<T, TResult> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        var query = context.Set<T>().AsQueryable();

        query = spec.ApplyCriteria(query);

        return await query.CountAsync();
    }

}
