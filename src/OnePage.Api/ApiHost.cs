using OnePage.Platform;
using OnePage.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using OnePage.Api.Endpoints;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace OnePage.Api;

public static class ApiHost
{
    public static WebApplication Create(string[]? args = null, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        configureBuilder?.Invoke(builder);
        builder.Services.AddSingleton<InMemoryEntitlementEvaluator>();
        builder.Services.AddSingleton<IEntitlementEvaluator>(sp => sp.GetRequiredService<InMemoryEntitlementEvaluator>());
        builder.Services.AddSingleton<ITrustedApiCredentialResolver, ConfigurationApiCredentialResolver>();
        builder.Services.AddScoped<IModuleAccessEvaluator, ModuleAccessEvaluator>();
        
        // Authentication services
        var tokenSecret = builder.Configuration["OnePage:Auth:Jwt:Secret"] ?? "onepage-default-secret-change-in-production";
        var tokenIssuer = builder.Configuration["OnePage:Auth:Jwt:Issuer"] ?? "OnePage";
        var tokenAudience = builder.Configuration["OnePage:Auth:Jwt:Audience"] ?? "OnePage.Client";
        var accessTokenExpiration = int.TryParse(builder.Configuration["OnePage:Auth:Jwt:AccessTokenExpirationMinutes"], out var accessExp) ? accessExp : 60;
        var refreshTokenExpiration = int.TryParse(builder.Configuration["OnePage:Auth:Jwt:RefreshTokenExpirationDays"], out var refreshExp) ? refreshExp : 7;
        
        var tokenConfig = new TokenConfiguration(tokenIssuer, tokenAudience, tokenSecret, accessTokenExpiration, refreshTokenExpiration);
        builder.Services.AddSingleton(tokenConfig);
        
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        builder.Services.AddSingleton<IJwtTokenService>(sp => new JwtTokenService(tokenIssuer, tokenAudience, tokenSecret, accessTokenExpiration, refreshTokenExpiration));
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        if (string.Equals(builder.Configuration["OnePage:DatabaseProvider"], "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddDbContext<OrganizationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("OnePage") ?? "Data Source=onepage.db"));
            builder.Services.AddDbContext<HrDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("OnePage") ?? "Data Source=onepage.db"));
        }
        else
        {
            builder.Services.AddDbContext<OrganizationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("OnePage") ?? "Host=localhost;Database=onepage;Username=postgres;Password=postgres"));
            builder.Services.AddDbContext<HrDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("OnePage") ?? "Host=localhost;Database=onepage;Username=postgres;Password=postgres"));
        }
        builder.Services.AddScoped<TenantContextAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        builder.Services.AddScoped<ITenantRepository, TenantRepository>();
        builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        builder.Services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
        builder.Services.AddScoped<IAuthorizationEvaluator, ScopedAuthorizationEvaluator>();
        builder.Services.AddScoped<IAuditRepository, AuditRepository>();
        builder.Services.AddScoped<IAssetsRepository, AssetsRepository>();
        builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
        builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        builder.Services.AddScoped<IProcurementRepository, ProcurementRepository>();
        builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
        builder.Services.AddScoped<IPosRepository, PosRepository>();
        builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();
        builder.Services.AddScoped<IHRRepository, HRRepository>();
        builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
        builder.Services.AddScoped<IHrRepository, HrRepository>();
        builder.Services.AddScoped<IEmployeeDocumentStorage, ExternalDocumentStorageBoundary>();
        builder.Services.AddScoped<IAttendanceImportValidator, AttendanceImportValidator>();

        // Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Enable CORS so the standalone UI can call the API in development
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();

        // Swagger/OpenAPI
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors();

        // Serve static files (e.g. uploaded profile images from /wwwroot/uploads)
        app.UseStaticFiles();

        // Exception handler for HR-specific validation and tenant context exceptions
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionHandler?.Error;
                if (ex is HrValidationException hrEx)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/problem+json";
                    await Results.Problem(statusCode: 400, title: "Invalid HR request", detail: hrEx.Message).ExecuteAsync(context);
                    return;
                }
                if (ex is TenantContextValidationException tcEx)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/problem+json";
                    await Results.Problem(statusCode: 400, title: "Tenant context validation error", detail: tcEx.Message).ExecuteAsync(context);
                    return;
                }
                // Re-throw any other exceptions so the default handler kicks in
                if (ex is not null)
                {
                    throw ex;
                }
            });
        });

        app.Use(async (httpContext, next) =>
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            {
                await next(httpContext);
                return;
            }

            if (IsAnonymousAuthPath(path))
            {
                await next(httpContext);
                return;
            }

            var credentials = httpContext.RequestServices.GetRequiredService<ITrustedApiCredentialResolver>();
            var authService = httpContext.RequestServices.GetRequiredService<IAuthenticationService>();
            var tokenService = httpContext.RequestServices.GetRequiredService<IJwtTokenService>();
            var accessor = httpContext.RequestServices.GetRequiredService<TenantContextAccessor>();
            var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();
            var tenantHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            var correlation = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");

            // Try JWT token authentication first
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var authResult = await authService.ValidateTokenAsync(token, httpContext.RequestAborted);
                
                if (authResult.Success && authResult.Principal is not null)
                {
                    var principal = authResult.Principal;
                    // Extract tenant from token or header
                    var tenantId = tenantHeader ?? principal.TenantId ?? "";
                    
                    // Set tenant context
                    accessor.Current = TenantContext.Create(principal.Id, tenantId, correlation);

                    // Set HttpContext.User so endpoints like /auth/me (which check
                    // context.User.Identity.IsAuthenticated) work after JWT validation
                    var userClaims = new List<Claim>
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, principal.Id),
                        new Claim("user_id", principal.Id),
                        new Claim("username", principal.Username ?? principal.Id),
                        new Claim("tenant_id", tenantId),
                    };
                    if (!string.IsNullOrEmpty(principal.Email))
                        userClaims.Add(new Claim(ClaimTypes.Email, principal.Email));
                    foreach (var role in principal.Roles)
                        userClaims.Add(new Claim(ClaimTypes.Role, role));
                    foreach (var permission in principal.Permissions)
                        userClaims.Add(new Claim("permission", permission));
                    httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "jwt"));

                    await next(httpContext);
                    return;
                }
            }

            // If an API key is provided, attempt to resolve it. If resolution fails in development, fall back to demo tenant.
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var credential = credentials.Resolve(apiKey);
                if (credential is null)
                {
                    if (app.Environment.IsDevelopment())
                    {
                        accessor.Current = TenantContext.Create("super-admin", "demo-tenant", correlation);
                        await next(httpContext);
                        return;
                    }
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await Results.Problem(statusCode: 401, title: "Authentication required", detail: "A valid API key is required.").ExecuteAsync(httpContext);
                    return;
                }

                // Super admin can access any tenant
                if (credential.IsSuperAdmin)
                {
                    // For super admin, use the requested tenant or default to system tenant
                    var effectiveTenant = string.IsNullOrWhiteSpace(tenantHeader) ? SuperAdmin.TenantId : tenantHeader.Trim();
                    accessor.Current = TenantContext.Create(credential.UserId, effectiveTenant, correlation);
                    await next(httpContext);
                    return;
                }

                if (string.IsNullOrWhiteSpace(tenantHeader))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await Results.Problem(statusCode: 400, title: "Tenant selection required", detail: "X-Tenant-Id is required.").ExecuteAsync(httpContext);
                    return;
                }

                // Check if super admin wildcard allows all tenants
                if (credential.AllowedTenantIds.Contains("*") || credential.AllowedTenantIds.Contains(tenantHeader.Trim()))
                {
                    accessor.Current = TenantContext.Create(credential.UserId, tenantHeader, correlation);
                    await next(httpContext);
                    return;
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await Results.Problem(statusCode: 403, title: "Tenant access denied", detail: "The API credential is not authorized for this tenant.").ExecuteAsync(httpContext);
                return;
            }

            // No API key supplied: in Development environment, auto-inject demo tenant context to help the SPA work without headers.
            if (app.Environment.IsDevelopment())
            {
                accessor.Current = TenantContext.Create("super-admin", "demo-tenant", correlation);
                await next(httpContext);
                return;
            }

            // Otherwise require an API key
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Results.Problem(statusCode: 401, title: "Authentication required", detail: "A valid API key is required.").ExecuteAsync(httpContext);
            return;
        });

        // Audit middleware: records tenant-scoped audit events for sensitive API paths
        app.Use(async (httpContext, next) =>
        {
            await next();
            try
            {
                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (!path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase)) return;
                var accessor = httpContext.RequestServices.GetService<ITenantContextAccessor>();
                var current = accessor?.Current;
                if (current is null) return;
                var auditRepo = httpContext.RequestServices.GetService<IAuditRepository>();
                if (auditRepo is null) return;

                var action = $"{httpContext.Request.Method} {path}";
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var resourceType = segments.Length >= 3 ? segments[2] : "api";
                string? resourceId = null;
                if (httpContext.Request.RouteValues.TryGetValue("id", out var id)) resourceId = id?.ToString();
                var source = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();

                var evt = new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, action, resourceType, resourceId, null, null, current.CorrelationId, source, userAgent);
                await auditRepo.AddAsync(evt);
            }
            catch
            {
                // Best-effort audit; never fail the request because of audit problems
            }
        });

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/api/v1/platform/entitlements/{namespace}/{*name}", CheckEntitlement);
        app.MapGet("/api/v1/platform/organization/branches/{id}", async (string id, IOrganizationRepository repository, CancellationToken ct) => Results.Ok(await repository.GetAsync<Branch>(id, ct)));
        app.MapGet("/api/v1/platform/context", (ITenantContextAccessor accessor) => Results.Ok(accessor.Current));
        app.MapGet("/api/v1/platform/authorize/{*permission}", Authorize);

        // Add simple listing endpoints for the SPA/demo
        app.MapGet("/api/v1/assets", async (ITenantContextAccessor ctx, IAssetsRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var list = await repo.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });

        app.MapGet("/api/v1/approvals/pending", async (ITenantContextAccessor ctx, IApprovalRepository approvals, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var list = await approvals.ListPendingAsync(current.TenantId, ct);
            return Results.Ok(list);
        });

        app.MapGet("/api/v1/approvals", async (ITenantContextAccessor ctx, IApprovalRepository approvals, IAuthorizationRepository authRepo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userRoles = await authRepo.GetUserRolesAsync(current.TenantId, current.UserId, ct);
            var list = await approvals.ListForUserAsync(current.TenantId, current.UserId, userRoles, ct);
            return Results.Ok(list);
        });

        app.MapGet("/api/v1/approvals/completed", async (ITenantContextAccessor ctx, IApprovalRepository approvals, IAuthorizationRepository authRepo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userRoles = await authRepo.GetUserRolesAsync(current.TenantId, current.UserId, ct);
            var list = await approvals.ListCompletedAsync(current.TenantId, current.UserId, userRoles, ct);
            return Results.Ok(list);
        });

        MapAuthenticationEndpoints(app);
        MapAssetEndpoints(app);
        MapApprovalEndpoints(app);
        MapProcurementEndpoints(app);
        MapInventoryEndpoints(app);
        MapPosEndpoints(app);
        MapFinanceEndpoints(app);
        MapReportingEndpoints(app);
        MapHREndpoints(app);
        MapAdvancedHREndpoints(app);
        MapDisciplinaryEndpoints(app);
        MapPerformanceEndpoints(app);
        MapRecruitmentEndpoints(app);
        MapPayrollEndpoints(app);
        MapDashboardEndpoints(app);
        MapSuperAdminEndpoints(app);
        MapUserManagementEndpoints(app);
        MapRoleManagementEndpoints(app);
        MapProfileSettingsEndpoints(app);
        MapWorkflowEndpoints(app);

        // Root "/" redirects to Swagger UI. This API is a pure backend; the
        // consolidated SPA is served separately by the OnePage.Web project (:5000).
        app.MapGet("/", () => Results.Redirect("/swagger"));
        return app;
    }

    private static bool IsAnonymousAuthPath(string path) =>
        path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/v1/auth/organizations", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/v1/auth/logout", StringComparison.OrdinalIgnoreCase);

    private static void MapAuthenticationEndpoints(WebApplication app)
    {
        app.MapAuthenticationEndpoints();
    }

    private static void MapAssetEndpoints(WebApplication app)
    {
        app.MapAssetsEndpoints();
    }

    private static void MapApprovalEndpoints(WebApplication app)
    {
        app.MapApprovalEndpoints();
    }

    private static void MapProcurementEndpoints(WebApplication app)
    {
        app.MapProcurementEndpoints();
    }

    private static void MapInventoryEndpoints(WebApplication app)
    {
        app.MapInventoryEndpoints();
    }

    private static void MapPosEndpoints(WebApplication app)
    {
        app.MapPosEndpoints();
    }

    private static void MapFinanceEndpoints(WebApplication app)
    {
        app.MapFinanceEndpoints();
    }

    private static void MapReportingEndpoints(WebApplication app)
    {
        app.MapReportingEndpoints();
    }

    private static void MapHREndpoints(WebApplication app)
    {
        app.MapHREndpoints();
    }

    private static void MapAdvancedHREndpoints(WebApplication app)
    {
        app.MapAdvancedHREndpoints();
    }

    private static void MapDisciplinaryEndpoints(WebApplication app)
    {
        app.MapDisciplinaryEndpoints();
    }

    private static void MapPerformanceEndpoints(WebApplication app)
    {
        app.MapPerformanceEndpoints();
    }

    private static void MapRecruitmentEndpoints(WebApplication app)
    {
        app.MapRecruitmentEndpoints();
    }

    private static void MapPayrollEndpoints(WebApplication app)
    {
        app.MapPayrollEndpoints();
    }

    private static void MapDashboardEndpoints(WebApplication app)
    {
        app.MapDashboardEndpoints();
    }

    private static void MapSuperAdminEndpoints(WebApplication app)
    {
        app.MapSuperAdminEndpoints();
    }

    private static void MapUserManagementEndpoints(WebApplication app)
    {
        app.MapUserManagementEndpoints();
    }

    private static void MapRoleManagementEndpoints(WebApplication app)
    {
        app.MapRoleManagementEndpoints();
    }

    private static void MapProfileSettingsEndpoints(WebApplication app)
    {
        app.MapProfileSettingsEndpoints();
    }

    private static void MapWorkflowEndpoints(WebApplication app)
    {
        app.MapGet("/api/v1/workflows/resource-types", async (ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            // Returns the available module/action pairs for the cascaded resource type dropdown.
            // Each module maps to a set of actions; the combined "module.action" is the ResourceType.
            var resourceTypes = new[]
            {
                new { Module = "purchase_order", Label = "Purchase Order", Actions = new[] { "approve" } },
                new { Module = "asset", Label = "Asset", Actions = new[] { "dispose", "transfer", "assign" } },
                new { Module = "inventory", Label = "Inventory", Actions = new[] { "adjust" } }
            };
            return Results.Ok(new { resourceTypes });
        });

        app.MapGet("/api/v1/workflows/roles-with-users", async (ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, OrganizationDbContext db, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();

            // Returns all roles in the tenant with their active users, for the role→users cascading dropdown.
            var roles = await db.Roles.AsNoTracking()
                .Where(r => r.TenantId == current.TenantId)
                .OrderBy(r => r.Name)
                .ToListAsync(ct);

            // Load all active user memberships for this tenant.
            var memberships = await db.UserMemberships.AsNoTracking()
                .Where(m => m.TenantId == current.TenantId && m.IsActive)
                .ToListAsync(ct);

            var memberIds = memberships.Select(m => m.Id).ToList();

            var roleAssignments = await db.MembershipRoleAssignments.AsNoTracking()
                .Where(mra => memberIds.Contains(mra.MembershipId))
                .ToListAsync(ct);

            // Build a map: roleId -> list of userIds
            var roleIdToUserIds = roleAssignments
                .GroupBy(mra => mra.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(mra => memberships.First(m => m.Id == mra.MembershipId).UserId).Distinct().ToList());

            var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
            var userProfiles = await db.UserProfiles.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, ct);

            var result = roles.Select(r => {
                var userIdsInRole = roleIdToUserIds.TryGetValue(r.Id, out var ids) ? ids : new List<string>();
                return new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    Users = userIdsInRole
                        .Select(uid => userProfiles.TryGetValue(uid, out var p)
                            ? new { UserId = p.UserId, p.FirstName, p.LastName, p.Email }
                            : new { UserId = uid, FirstName = "", LastName = "", Email = "" })
                        .ToArray()
                };
            }).ToArray();

            return Results.Ok(new { roles = result });
        });

        app.MapGet("/api/v1/workflows", async (ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, IWorkflowRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            var list = await repo.ListAsync(current.TenantId, ct);
            return Results.Ok(list.Select(w => new {
                w.Id, w.Name, w.Description, w.ResourceType, w.TriggerType, w.TriggerAmount, w.TriggerQuantity, w.IsActive, w.CreatedAt, w.UpdatedAt
            }));
        });

        app.MapGet("/api/v1/workflows/{id}", async (string id, ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, IWorkflowRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            var workflow = await repo.GetAsync(id, ct);
            if (workflow is null || workflow.TenantId != current.TenantId) return Results.NotFound();
            var steps = await repo.ListStepsAsync(workflow.Id, ct);
            return Results.Ok(new
            {
                workflow.Id, workflow.Name, workflow.Description, workflow.ResourceType, workflow.TriggerType, workflow.TriggerAmount, workflow.TriggerQuantity, workflow.IsActive, workflow.CreatedAt, workflow.UpdatedAt,
                Steps = steps.Select(s => new { s.Id, s.WorkflowDefinitionId, s.StepNumber, s.ApproverType, s.ApproverValue, s.CanSkip })
            });
        });

        app.MapPost("/api/v1/workflows", async (CreateWorkflowDefinitionCommand command, ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, IWorkflowRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            var workflow = new WorkflowDefinition(
                Guid.NewGuid().ToString("N"), current.TenantId, command.Name, command.ResourceType,
                command.TriggerType, command.TriggerAmount, command.IsActive, command.Description, command.TriggerQuantity);
            await repo.CreateAsync(workflow, ct);
            var steps = (command.Steps ?? []).Select((s, i) => new WorkflowStep(
                Guid.NewGuid().ToString("N"), current.TenantId, workflow.Id, i + 1, s.ApproverType, s.ApproverValue, s.CanSkip));
            await repo.ReplaceStepsAsync(workflow.Id, steps.ToList(), ct);
            return Results.Created($"/api/v1/workflows/{workflow.Id}", new
            {
                workflow.Id, workflow.Name, workflow.Description, workflow.ResourceType, workflow.TriggerType, workflow.TriggerAmount, workflow.TriggerQuantity, workflow.IsActive, workflow.CreatedAt, workflow.UpdatedAt,
                Steps = steps.Select(s => new { s.Id, s.WorkflowDefinitionId, s.StepNumber, s.ApproverType, s.ApproverValue, s.CanSkip })
            });
        });

        app.MapPut("/api/v1/workflows/{id}", async (string id, UpdateWorkflowDefinitionCommand command, ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, IWorkflowRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            var workflow = await repo.GetAsync(id, ct);
            if (workflow is null || workflow.TenantId != current.TenantId) return Results.NotFound();
            workflow.UpdateDetails(command.Name, command.Description, command.TriggerType, command.TriggerAmount, command.IsActive, command.TriggerQuantity);
            await repo.UpdateAsync(workflow, ct);
            var steps = (command.Steps ?? []).Select((s, i) => new WorkflowStep(
                Guid.NewGuid().ToString("N"), current.TenantId, workflow.Id, i + 1, s.ApproverType, s.ApproverValue, s.CanSkip));
            await repo.ReplaceStepsAsync(workflow.Id, steps.ToList(), ct);
            return Results.Ok(new
            {
                workflow.Id, workflow.Name, workflow.Description, workflow.ResourceType, workflow.TriggerType, workflow.TriggerAmount, workflow.TriggerQuantity, workflow.IsActive, workflow.CreatedAt, workflow.UpdatedAt,
                Steps = steps.Select(s => new { s.Id, s.WorkflowDefinitionId, s.StepNumber, s.ApproverType, s.ApproverValue, s.CanSkip })
            });
        });

        app.MapDelete("/api/v1/workflows/{id}", async (string id, ITenantContextAccessor ctx, IAuthorizationEvaluator evaluator, IWorkflowRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.WorkflowManage), ct);
            if (!decision.Allowed) return Results.Forbid();
            var workflow = await repo.GetAsync(id, ct);
            if (workflow is null || workflow.TenantId != current.TenantId) return Results.NotFound();
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        await OrganizationPersistence.InitializeAsync(db, cancellationToken);
        var hrDb = scope.ServiceProvider.GetRequiredService<HrDbContext>();
        await HrPersistence.InitializeAsync(hrDb, cancellationToken);
        // Seed demo data for presentation
        try
        {
            await DemoData.SeedAsync(services, cancellationToken);
        }
        catch
        {
            // best-effort
        }
    }

    private static IResult CheckEntitlement(string @namespace, string name, ITenantContextAccessor accessor, IEntitlementEvaluator evaluator, HttpRequest request)
    {
        try
        {
            var context = accessor.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var key = new EntitlementKey(@namespace, name);
            var requestedUsage = long.TryParse(request.Query["requestedUsage"], out var parsed) ? parsed : 0;
            var historicalRead = bool.TryParse(request.Query["historicalRead"], out var historical) && historical;
            var decision = evaluator.Evaluate(context, key, requestedUsage, historicalRead);
            return decision.Allowed ? Results.Ok(decision) : Results.Problem(statusCode: 403, title: "Entitlement denied", detail: $"Entitlement '{key}' cannot be used for this operation.", extensions: new Dictionary<string, object?>
            {
                ["code"] = "ENTITLEMENT_DENIED", ["denialReason"] = decision.DenialReason?.ToString(), ["entitlement"] = key.ToString()
            });
        }
        catch (TenantContextValidationException ex)
        {
            return Results.Problem(statusCode: 400, title: "Invalid tenant context", detail: ex.Message);
        }
    }

    private static async Task<IResult> Authorize(string permission, ITenantContextAccessor accessor, IAuthorizationEvaluator evaluator, HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var context = accessor.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var amount = decimal.TryParse(request.Query["amount"], out var parsedAmount) ? parsedAmount : (decimal?)null;
            var managerChain = request.Query["managerChain"].FirstOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
            var scope = new AuthorizationScope(request.Query["legalEntityId"], request.Query["branchId"], request.Query["departmentId"], request.Query["locationId"], managerChain);
            var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(context, PermissionCatalog.Create(permission), scope, amount, request.Query["currency"]), cancellationToken);
            return decision.Allowed ? Results.Ok(decision) : Results.Problem(statusCode: 403, title: "Authorization denied", detail: "The requested action is not authorized.", extensions: new Dictionary<string, object?> { ["code"] = "AUTHORIZATION_DENIED", ["denialReason"] = decision.DenialReason?.ToString() });
        }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Invalid authorization request", detail: ex.Message); }
    }
}
