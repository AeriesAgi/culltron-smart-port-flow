namespace SmartPort.Infrastructure.Services;

/// <summary>
/// A single, immutable audit record. Records are append-only: once written they are
/// never edited or deleted (the list is only trimmed from the front when it grows past
/// a cap, and a trim is itself an out-of-band operation, not a mutation of any record).
/// This gives every AI recommendation and every state-changing action a who / what /
/// when / why trail that can be rendered in the UI.
/// </summary>
public sealed record DecisionAuditRecord(
    long Sequence,
    DateTime TimestampUtc,
    string Actor,          // who (operator name / role / "Smart Port engine")
    string Category,       // domain area, e.g. "Cross-Domain Reasoning", "AI Brief", "Fleet Action"
    string Action,         // what happened, e.g. "Recommendation generated", "Approved", "Rejected"
    string Subject,        // the thing acted on, e.g. recommendation id or truck reference
    string Reasoning,      // why — the reasoning behind an AI suggestion or the decision note
    string Source,         // provenance, e.g. "Deterministic fallback engine" / "Gemini + fallback"
    string ApprovalState,  // Pending / Approved / Rejected / Modified / Logged
    string Impact);        // quantified impact summary (trucks, idling, CO2, cost)

public interface IDecisionAuditService
{
    DecisionAuditRecord Record(string actor, string category, string action, string subject,
        string reasoning, string source, string approvalState, string impact);

    IReadOnlyList<DecisionAuditRecord> Recent(int count = 100);
    int Count { get; }
}

/// <summary>
/// In-memory, thread-safe, append-only audit trail. Matches the existing demo pattern
/// (Gemini agent history, agent-governance safety history) of keeping a session-scoped
/// log without requiring a database migration, while exposing a single place every
/// recommendation and state change can be traced from.
/// </summary>
public sealed class InMemoryDecisionAuditService : IDecisionAuditService
{
    private const int MaxEntries = 1000;
    private readonly List<DecisionAuditRecord> _entries = new();
    private readonly object _lock = new();
    private long _sequence;

    public InMemoryDecisionAuditService()
    {
        // Seed a small, clearly-synthetic history so the audit UI is never empty on first load.
        var seedTime = DateTime.UtcNow.AddMinutes(-42);
        Record("Smart Port engine", "Cross-Domain Reasoning", "Recommendation generated",
            "REC-SEED01",
            "Berth pressure and gate backlog scored together exceeded the combined threshold.",
            "Deterministic fallback engine", "Logged",
            "12 trucks · 34 min avg idling · indicative demurrage risk", seedTime);
        Record("Nomvula Dlamini (Port Ops)", "Cross-Domain Reasoning", "Approved",
            "REC-SEED01",
            "Operator approved staging re-sequence; gate expedite confirmed with terminal.",
            "Human operator", "Approved",
            "Approved for dispatch — drivers/fleet notified after approval", seedTime.AddMinutes(3));
    }

    public DecisionAuditRecord Record(string actor, string category, string action, string subject,
        string reasoning, string source, string approvalState, string impact)
        => Record(actor, category, action, subject, reasoning, source, approvalState, impact, DateTime.UtcNow);

    private DecisionAuditRecord Record(string actor, string category, string action, string subject,
        string reasoning, string source, string approvalState, string impact, DateTime timestampUtc)
    {
        lock (_lock)
        {
            var record = new DecisionAuditRecord(
                ++_sequence,
                timestampUtc,
                Clean(actor, "Unknown actor"),
                Clean(category, "General"),
                Clean(action, "Recorded"),
                Clean(subject, "—"),
                Clean(reasoning, "No reasoning supplied"),
                Clean(source, "Deterministic fallback engine"),
                Clean(approvalState, "Logged"),
                Clean(impact, "—"));
            _entries.Add(record);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
            return record;
        }
    }

    public IReadOnlyList<DecisionAuditRecord> Recent(int count = 100)
    {
        lock (_lock)
        {
            return _entries
                .OrderByDescending(e => e.Sequence)
                .Take(Math.Clamp(count, 1, MaxEntries))
                .ToList();
        }
    }

    public int Count { get { lock (_lock) { return _entries.Count; } } }

    private static string Clean(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
