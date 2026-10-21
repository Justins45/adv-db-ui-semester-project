using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using TheaterApi.Data;
using TheaterApi.Models.Logs;
using TheaterApi.Models;
using TheaterApi.Service;

namespace TheaterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(
    ILogRepository<AuditLog> auditLogs,
    ILogRepository<PermissionChangeLog> permissionLogs,
    ILogRepository<PerformanceStatusLog> performanceLogs,
    ILogRepository<ErrorLog> errorLogs,
    AuditLogService auditLogService) : ControllerBase
{
    // ───────────── Audit log ─────────────

    [HttpGet("audit")]
     [Tags("Logs - Audit")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> ListAudit(
        [Range(1, 200)] int limit = 50, CancellationToken ct = default) =>
        Ok(await auditLogs.GetRecentAsync(limit, ct));

    [HttpGet("audit/{id:int}")]
     [Tags("Logs - Audit")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<AuditLog>> GetAudit(int id, CancellationToken ct) =>
        GetOne(auditLogs, id, ct);

    [HttpPost("audit")]
     [Tags("Logs - Audit")]
    [ProducesResponseType<AuditLog>(StatusCodes.Status201Created)]
    public Task<ActionResult<AuditLog>> CreateAudit(AuditLogRequest request, CancellationToken ct) =>
        CreateOne(auditLogs, request.ToEntity(), nameof(GetAudit), ct);

    [HttpPut("audit/{id:int}")]
     [Tags("Logs - Audit")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<AuditLog>> UpdateAudit(int id, AuditLogRequest request, CancellationToken ct) =>
        UpdateOne(auditLogs, id, request.ApplyTo, ct);

    [HttpDelete("audit/{id:int}")]
     [Tags("Logs - Audit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteAudit(int id, CancellationToken ct) =>
        DeleteOne(auditLogs, id, ct);

    // NOTE: (1) FIREBASE SDK QUERY -- aggregate count
    // GET api/logs/audit-counts
    [HttpGet("audit-counts")]
     [Tags("Logs - Audit")]
    public async Task<ActionResult<AuditLogCounts>> GetAuditCounts()
        => Ok(await auditLogService.GetCountsAsync());

    
    // NOTE: (2) FIREBASE SDK QUERY -- filter by
    // GET api/logs/audit-recent?daysAgo=7
    [HttpGet("audit-recent")]
     [Tags("Logs - Audit")]
    public async Task<ActionResult<List<AuditLog>>> GetRecentAuditLogs(
        [FromQuery, Range(1, 365)] int daysAgo = 7)
        => Ok(await auditLogService.GetRecentAsync(daysAgo));

    // ───────────── Permission changes ─────────────

    [HttpGet("permission-changes")]
    [Tags("Logs - Permission Changes")]
    public async Task<ActionResult<IReadOnlyList<PermissionChangeLog>>> ListPermissionChanges(
        [Range(1, 200)] int limit = 50, CancellationToken ct = default) =>
        Ok(await permissionLogs.GetRecentAsync(limit, ct));

    [HttpGet("permission-changes/{id:int}")]
    [Tags("Logs - Permission Changes")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PermissionChangeLog>> GetPermissionChange(int id, CancellationToken ct) =>
        GetOne(permissionLogs, id, ct);

    [HttpPost("permission-changes")]
    [Tags("Logs - Permission Changes")]
    [ProducesResponseType<PermissionChangeLog>(StatusCodes.Status201Created)]
    public Task<ActionResult<PermissionChangeLog>> CreatePermissionChange(
        PermissionChangeLogRequest request, CancellationToken ct) =>
        CreateOne(permissionLogs, request.ToEntity(), nameof(GetPermissionChange), ct);

    [HttpPut("permission-changes/{id:int}")]
    [Tags("Logs - Permission Changes")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PermissionChangeLog>> UpdatePermissionChange(
        int id, PermissionChangeLogRequest request, CancellationToken ct) =>
        UpdateOne(permissionLogs, id, request.ApplyTo, ct);

    [HttpDelete("permission-changes/{id:int}")]
    [Tags("Logs - Permission Changes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeletePermissionChange(int id, CancellationToken ct) =>
        DeleteOne(permissionLogs, id, ct);

    // ───────────── Performance status changes ─────────────

    [HttpGet("performance-status")]
    [Tags("Logs - Performance Status")]
    public async Task<ActionResult<IReadOnlyList<PerformanceStatusLog>>> ListPerformanceStatus(
        [Range(1, 200)] int limit = 50, CancellationToken ct = default) =>
        Ok(await performanceLogs.GetRecentAsync(limit, ct));

    [HttpGet("performance-status/{id:int}")]
    [Tags("Logs - Performance Status")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PerformanceStatusLog>> GetPerformanceStatus(int id, CancellationToken ct) =>
        GetOne(performanceLogs, id, ct);

    [HttpPost("performance-status")]
    [Tags("Logs - Performance Status")]
    [ProducesResponseType<PerformanceStatusLog>(StatusCodes.Status201Created)]
    public Task<ActionResult<PerformanceStatusLog>> CreatePerformanceStatus(
        PerformanceStatusLogRequest request, CancellationToken ct) =>
        CreateOne(performanceLogs, request.ToEntity(), nameof(GetPerformanceStatus), ct);

    [HttpPut("performance-status/{id:int}")]
    [Tags("Logs - Performance Status")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PerformanceStatusLog>> UpdatePerformanceStatus(
        int id, PerformanceStatusLogRequest request, CancellationToken ct) =>
        UpdateOne(performanceLogs, id, request.ApplyTo, ct);

    [HttpDelete("performance-status/{id:int}")]
    [Tags("Logs - Performance Status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeletePerformanceStatus(int id, CancellationToken ct) =>
        DeleteOne(performanceLogs, id, ct);

    // ───────────── Error log ─────────────

    [HttpGet("errors")]
    [Tags("Logs - Errors")]
    public async Task<ActionResult<IReadOnlyList<ErrorLog>>> ListErrors(
        [Range(1, 200)] int limit = 50, CancellationToken ct = default) =>
        Ok(await errorLogs.GetRecentAsync(limit, ct));

    [HttpGet("errors/{id:int}")]
    [Tags("Logs - Errors")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<ErrorLog>> GetError(int id, CancellationToken ct) =>
        GetOne(errorLogs, id, ct);

    [HttpPost("errors")]
    [Tags("Logs - Errors")]
    [ProducesResponseType<ErrorLog>(StatusCodes.Status201Created)]
    public Task<ActionResult<ErrorLog>> CreateError(ErrorLogRequest request, CancellationToken ct) =>
        CreateOne(errorLogs, request.ToEntity(), nameof(GetError), ct);

    [HttpPut("errors/{id:int}")]
    [Tags("Logs - Errors")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<ErrorLog>> UpdateError(int id, ErrorLogRequest request, CancellationToken ct) =>
        UpdateOne(errorLogs, id, request.ApplyTo, ct);

    [HttpDelete("errors/{id:int}")]
    [Tags("Logs - Errors")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteError(int id, CancellationToken ct) =>
        DeleteOne(errorLogs, id, ct);

    // ───────────── Shared helpers (private, so they are not endpoints) ─────────────

    private async Task<ActionResult<T>> GetOne<T>(ILogRepository<T> repo, int id, CancellationToken ct)
        where T : class, ILogDocument
    {
        var doc = await repo.GetByIdAsync(id, ct);
        if (doc is null) return NotFound();
        return doc;
    }

    private async Task<ActionResult<T>> CreateOne<T>(
        ILogRepository<T> repo, T entity, string getActionName, CancellationToken ct)
        where T : class, ILogDocument
    {
        var created = await repo.CreateAsync(entity, ct);
        return CreatedAtAction(getActionName, new { id = created.Id }, created);
    }

    private async Task<ActionResult<T>> UpdateOne<T>(
        ILogRepository<T> repo, int id, Action<T> apply, CancellationToken ct)
        where T : class, ILogDocument
    {
        var updated = await repo.UpdateAsync(id, apply, ct);
        if (updated is null) return NotFound();
        return updated;
    }

    private async Task<IActionResult> DeleteOne<T>(ILogRepository<T> repo, int id, CancellationToken ct)
        where T : class, ILogDocument
    {
        if (!await repo.DeleteAsync(id, ct)) return NotFound();
        return NoContent();
    }
}