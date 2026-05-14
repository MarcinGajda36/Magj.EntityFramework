namespace Magj.EntityFramework;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public readonly record struct SavedResult<TResult>(TResult Result, int AffectedRows);

public static class DbContextFactoryExtensions
{
    public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
    public const QueryTrackingBehavior DefaultSaveQueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    public const QueryTrackingBehavior DefaultReadQueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    public static Task<SavedResult<TResult>> SaveInTransactionAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (argument, resultFactory, queryTrackingBehavior, isolationLevel),
            static async (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                var affectedRows = await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new SavedResult<TResult>(result, affectedRows);
            },
            cancellationToken);
    }

    public static Task<SavedResult<TResult>> SaveInTransactionAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => SaveInTransactionAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            isolationLevel,
            queryTrackingBehavior,
            cancellationToken);

    public static Task<SavedResult<TResult>> SaveAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (argument, resultFactory, queryTrackingBehavior),
            static async (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                var result = await arguments.resultFactory(context, arguments.argument, cancellationToken);
                var affectedRows = await context.SaveChangesAsync(cancellationToken);
                return new SavedResult<TResult>(result, affectedRows);
            },
            cancellationToken);
    }

    public static Task<SavedResult<TResult>> SaveAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultSaveQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => SaveAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            queryTrackingBehavior,
            cancellationToken);

    public static Task<TResult> ReadAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (argument, resultFactory, queryTrackingBehavior),
            static (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                return arguments.resultFactory(context, arguments.argument, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ReadAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => ReadAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            queryTrackingBehavior,
            cancellationToken);

    public static Task<TResult> ReadInTransactionAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (argument, resultFactory, isolationLevel, queryTrackingBehavior),
            static async (context, arguments, cancellationToken) =>
            {
                context.ChangeTracker.QueryTrackingBehavior = arguments.queryTrackingBehavior;
                await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                return await arguments.resultFactory(context, arguments.argument, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ReadInTransactionAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = DefaultIsolationLevel,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => ReadInTransactionAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            isolationLevel,
            queryTrackingBehavior,
            cancellationToken);

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return CoreAsync(dbContextFactory, argument, resultFactory, cancellationToken);

        static async Task<TResult> CoreAsync(
            IDbContextFactory<TContext> dbContextFactory,
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(
                (argument, resultFactory),
                static (contextBase, arguments, cancellationToken) =>
                {
                    contextBase.ChangeTracker.Clear(); // When re-try triggers then ChangeTracker still has changes from previous try iirc.
                    return arguments.resultFactory((TContext)contextBase, arguments.argument, cancellationToken);
                },
                null,
                cancellationToken);
        }
    }

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => ExecuteInStrategyAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            cancellationToken);

    public static IAsyncEnumerable<TResult> StreamReadAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return CoreAsync(dbContextFactory, argument, queryFactory, queryTrackingBehavior, cancellationToken);

        static async IAsyncEnumerable<TResult> CoreAsync(
            IDbContextFactory<TContext> dbContextFactory,
            TArgument argument,
            Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
            QueryTrackingBehavior queryTrackingBehavior,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            context.ChangeTracker.QueryTrackingBehavior = queryTrackingBehavior;
            await foreach (var result in queryFactory(context, argument)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }

    public static IAsyncEnumerable<TResult> StreamReadAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TResult>> queryFactory,
        QueryTrackingBehavior queryTrackingBehavior = DefaultReadQueryTrackingBehavior,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => StreamReadAsync(
            dbContextFactory,
            queryFactory,
            static (context, queryFactory) => queryFactory(context),
            queryTrackingBehavior,
            cancellationToken);
}
