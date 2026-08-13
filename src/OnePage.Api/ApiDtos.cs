namespace OnePage.Api;

// Top-level DTOs/commands used by endpoint handlers
public record CreateAssetCommand(string Id, string Tag, string Name, string? Description, string? LocationId, string? CustodianEmployeeId, string? LegalEntityId, string? BranchId, string? DepartmentId);
public record AssignAssetCommand(string EmployeeId);
public record DisposeAssetCommand(string Reason);
public record DecideApprovalCommand(bool Approve, string? Comment);

public record CreatePurchaseOrderCommand(string Id, string Supplier, decimal TotalAmount);
public record CreateInventoryItemCommand(string Id, string Sku, string Name, decimal Quantity);
public record AdjustInventoryCommand(decimal Delta);
public record CreatePosSaleCommand(string Id, string? RegisterId, decimal Total);
public record CreateJournalEntryCommand(string Id, string Reference);
