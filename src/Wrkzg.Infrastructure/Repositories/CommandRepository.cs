using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class CommandRepository : ICommandRepository
{
    private readonly BotDbContext _db;

    public CommandRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<Command?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Commands.FindAsync(new object[] { id }, ct);
    }

    public async Task<Command?> GetByTriggerOrAliasAsync(string trigger, CancellationToken ct = default)
    {
        string lowerTrigger = trigger.ToLowerInvariant();

        // First try exact trigger match
        Command? command = await _db.Commands
            .FirstOrDefaultAsync(c => c.Trigger == lowerTrigger, ct);

        if (command is not null)
        {
            return command;
        }

        // Fallback: search aliases (JSON array stored as text)
        // SQLite doesn't have native JSON array search, so we load all enabled commands
        // and check aliases in memory. With typical command counts (<200), this is fine.
        List<Command> allCommands = await _db.Commands
            .Where(c => c.IsEnabled)
            .ToListAsync(ct);

        return allCommands.FirstOrDefault(c =>
            c.Aliases.Any(a => a.Equals(lowerTrigger, System.StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Commands
            .OrderBy(c => c.Trigger)
            .ToListAsync(ct);
    }

    public async Task<Command> CreateAsync(Command command, CancellationToken ct = default)
    {
        _db.Commands.Add(command);
        await _db.SaveChangesAsync(ct);
        return command;
    }

    public async Task UpdateAsync(Command command, CancellationToken ct = default)
    {
        _db.Commands.Update(command);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Command? command = await _db.Commands.FindAsync(new object[] { id }, ct);

        if (command is not null)
        {
            _db.Commands.Remove(command);
            await _db.SaveChangesAsync(ct);
        }
    }
}
