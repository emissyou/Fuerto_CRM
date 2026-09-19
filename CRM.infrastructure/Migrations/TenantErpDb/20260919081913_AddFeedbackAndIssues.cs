using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.infrastructure.Migrations.TenantErpDb
{
    /// <inheritdoc />
    public partial class AddFeedbackAndIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectFeedbacks",
                columns: table => new
                {
                    ProjectFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OverallRating = table.Column<int>(type: "int", nullable: false),
                    TimelinessRating = table.Column<int>(type: "int", nullable: false),
                    CommunicationRating = table.Column<int>(type: "int", nullable: false),
                    ValueRating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DesignLikes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DesignImprovements = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WouldRecommend = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeedbacks", x => x.ProjectFeedbackId);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFeedbacks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectIssues",
                columns: table => new
                {
                    ProjectIssueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DisputedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReportedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIssues", x => x.ProjectIssueId);
                    table.ForeignKey(
                        name: "FK_ProjectIssues_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectIssues_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_CustomerId",
                table: "ProjectFeedbacks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_ProjectId",
                table: "ProjectFeedbacks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIssues_CustomerId",
                table: "ProjectIssues",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIssues_ProjectId",
                table: "ProjectIssues",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectFeedbacks");

            migrationBuilder.DropTable(
                name: "ProjectIssues");
        }
    }
}
